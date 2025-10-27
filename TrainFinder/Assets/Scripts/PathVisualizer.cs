using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Visualiza un NavMeshPath con un LineRenderer, alineando el espacio del NavMesh (mundo virtual)
/// con el espacio AR actual (XR Origin) tras la localización por imagen.
/// </summary>
public class PathVisualizer : MonoBehaviour
{
    [Header("Path Reference")]
    [Tooltip("Referencia al script de localización por imagen")]
    public ImageLocalization imageLocalization;

    [Header("Line Renderer")]
    [Tooltip("LineRenderer que dibuja la ruta")]
    public LineRenderer lineRenderer;

    [Header("Line Settings")]
    [Tooltip("Anchura de la línea en metros")]
    public float lineWidth = 0.03f;

    [Tooltip("Color de la línea")]
    public Color lineColor = new Color(1f, 0f, 1f, 1f); // magenta para visibilidad

    [Tooltip("Material para la línea (si es null, se crea uno Unlit/Color)")]
    public Material lineMaterial;

    [Tooltip("Altura extra sobre el suelo para evitar z-fighting")]
    public float heightOffset = 0.02f;

    [Tooltip("Cap y corner simplificados (mejor para AR)")]
    public bool useSimpleCap = true;

    [Header("Display Settings")]
    [Tooltip("Mostrar solo cuando hay localización válida")]
    public bool onlyShowWhenLocalized = true;

    [Tooltip("Logs de depuración")]
    public bool enableDebugLogs = false;

    [Tooltip("Mostrar una esfera donde termina una ruta parcial")]
    public bool showGapIndicator = true;

    [Tooltip("Color de la esfera de gap")]
    public Color gapIndicatorColor = Color.red;

    [Tooltip("Tamaño de la esfera de gap")]
    public float gapIndicatorSize = 0.08f;

    private NavMeshPath currentPath;
    private GameObject gapIndicatorObject;

    private void Awake()
    {
        if (lineRenderer != null)
        {
            ConfigureLineRenderer();
        }
    }

    private void Start()
    {
        // Autodetección de ImageLocalization si no está asignado
        if (imageLocalization == null)
        {
            imageLocalization = FindObjectOfType<ImageLocalization>();
            if (imageLocalization != null)
                DebugLog("✓ Auto-found ImageLocalization");
            else
                Debug.LogWarning("⚠️ ImageLocalization no encontrado. Asigna la referencia.");
        }

        if (lineRenderer == null)
        {
            Debug.LogError("❌ LineRenderer no asignado. Asigna uno en el Inspector.");
        }
        else
        {
            // Desactivar al inicio
            lineRenderer.enabled = false;
            DebugLog("✓ LineRenderer asignado y desactivado inicialmente");
        }
    }

    private void ConfigureLineRenderer()
    {
        if (lineRenderer == null) return;

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // Material simple no iluminado (mejor en AR)
        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
        }

        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 0;

        // Sin sombras
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // La línea siempre “de frente” a cámara ayuda en AR
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;

        if (useSimpleCap)
        {
            lineRenderer.numCapVertices = 0;
            lineRenderer.numCornerVertices = 0;
        }

        // Asegura que no quede oculto por planos/meshes
        lineRenderer.sortingOrder = 100;

        lineRenderer.enabled = false;
        DebugLog("✓ LineRenderer configurado");
    }

    private void Update()
    {
        if (imageLocalization == null || lineRenderer == null)
        {
            HidePath();
            return;
        }

        if (onlyShowWhenLocalized && !imageLocalization.IsLocalized())
        {
            HidePath();
            return;
        }

        // Usamos el path que mantiene ImageLocalization
        currentPath = imageLocalization.navMeshPath; // expuesto en ImageLocalization:contentReference[oaicite:2]{index=2}

        if (currentPath == null || currentPath.corners == null || currentPath.corners.Length < 2)
        {
            HidePath();
            if (enableDebugLogs && currentPath != null)
                Debug.LogWarning($"[PathVisualizer] Path con pocos puntos: {currentPath.corners.Length}");
            return;
        }

        DrawPath(currentPath);
    }

    private void DrawPath(NavMeshPath path)
    {
        if (lineRenderer == null || path == null) return;

        var corners = path.corners;
        if (corners == null || corners.Length < 2)
        {
            HidePath();
            return;
        }

        // Cámara principal (AR Camera)
// Cámara principal (AR Camera)
        Camera arCamera = Camera.main;
        if (arCamera == null) { Debug.LogWarning("[PathVisualizer] No hay Camera.main"); return; }

// XR Origin actual
        var xr = imageLocalization != null ? imageLocalization.xrOrigin : null;

// Heurística: si la primera esquina ya está cerca de la cámara, asumimos que las corners están en mundo AR
        float distCorner0ToCam = Vector3.Distance(arCamera.transform.position, corners[0]);
        bool cornersAreInARWorld = distCorner0ToCam < 2.0f; // umbral 2 m; ajusta si quieres

        lineRenderer.positionCount = corners.Length;

        if (enableDebugLogs)
        {
            Debug.Log($"[PathVisualizer] dist cam -> corner0 = {distCorner0ToCam:F2} m. cornersAreInARWorld={cornersAreInARWorld}");
        }

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 p;

            if (cornersAreInARWorld || xr == null)
            {
                // Usamos las esquinas tal cual (ya en AR world)
                p = corners[i];
            }
            else
            {
                // Solo si hiciera falta convertir del espacio virtual al AR actual
                p = xr.TransformPoint(corners[i]);
            }

            p.y += heightOffset; // pequeño offset para evitar z-fighting
            lineRenderer.SetPosition(i, p);

            if (enableDebugLogs && i == 0)
            {
                Debug.Log($"  First (virtual): {corners[0]:F3}");
                Debug.Log($"  First (AR used): {p:F3}");
                Debug.Log($"  Dist a cámara:   {Vector3.Distance(arCamera.transform.position, p):F2} m");
            }
        }

        if (showGapIndicator && corners.Length > 0)
        {
            if (xr != null) ShowGapIndicator(xr.TransformPoint(corners[corners.Length - 1]));
        }
    }

    private void ShowGapIndicator(Vector3 worldPosition)
    {
        if (gapIndicatorObject == null)
        {
            gapIndicatorObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gapIndicatorObject.name = "GapIndicator";
            gapIndicatorObject.transform.localScale = Vector3.one * gapIndicatorSize;
            var col = gapIndicatorObject.GetComponent<Collider>();
            if (col) Destroy(col);
        }

        worldPosition.y += heightOffset;
        gapIndicatorObject.transform.position = worldPosition;

        if (!gapIndicatorObject.activeSelf)
            gapIndicatorObject.SetActive(true);
    }

    private void HidePath()
    {
        if (lineRenderer != null && lineRenderer.enabled)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
            DebugLog("Línea oculta");
        }

        if (gapIndicatorObject != null && gapIndicatorObject.activeSelf)
        {
            gapIndicatorObject.SetActive(false);
            DebugLog("Gap indicator oculto");
        }
    }

    private void DebugLog(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[PathVisualizer] {msg}");
    }

    // API opcional
    public void SetLineWidth(float width)
    {
        lineWidth = width;
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }
    }

    public void SetLineColor(Color color)
    {
        lineColor = color;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            if (lineRenderer.material != null) lineRenderer.material.color = color;
        }
    }

    public void TogglePathVisibility(bool visible)
    {
        if (lineRenderer != null) lineRenderer.enabled = visible;
    }
}
 
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ArrowGenerator : MonoBehaviour
{
    public Material arrowMaterial;

    [Header("Shaft Settings")]
    public float shaftLength = 1.0f;
    public float shaftRadius = 0.05f;

    [Header("Head Settings")]
    public float headLength = 0.3f;
    public float headRadius = 0.15f;

    void Awake()
    {
        // Create the shaft GameObject
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Shaft";
        shaft.transform.SetParent(transform, false);
        shaft.transform.localScale = new Vector3(shaftRadius * 2, shaftLength / 2, shaftRadius * 2);
        shaft.transform.localPosition = new Vector3(0, 0, shaftLength / 2);
        shaft.transform.localRotation = Quaternion.Euler(90, 0, 0);

        // Create the head GameObject
        GameObject head = new GameObject("Head");
        head.transform.SetParent(transform, false);
        head.transform.localPosition = new Vector3(0, 0, shaftLength);
        GenerateConeOnGameObject(head, headLength, headRadius); // Custom cone generation

        // Assign material to both parts
        if (arrowMaterial != null)
        {
            shaft.GetComponent<MeshRenderer>().material = arrowMaterial;
            head.GetComponent<MeshRenderer>().material = arrowMaterial;
        }
        else
        {
            Debug.LogWarning("Arrow material is not assigned in the ArrowGenerator.", this);
        }
    }

    void GenerateConeOnGameObject(GameObject target, float height, float radius)
    {
        MeshFilter meshFilter = target.AddComponent<MeshFilter>();
        target.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;

        int numSegments = 32;

        // Vertices
        Vector3[] vertices = new Vector3[numSegments + 2];

        // Apex
        vertices[0] = new Vector3(0, 0, height); // Adjusted for Z-forward

        // Base center
        vertices[numSegments + 1] = Vector3.zero;

        float angle = 2 * Mathf.PI / numSegments;
        for (int i = 0; i < numSegments; i++)
        {
            float x = Mathf.Sin(i * angle) * radius;
            float y = Mathf.Cos(i * angle) * radius;
            vertices[i + 1] = new Vector3(x, y, 0); // Adjusted for XY plane base
        }

        mesh.vertices = vertices;

        // Triangles
        int[] triangles = new int[numSegments * 6];
        int triIndex = 0;

        // Cone body
        for (int i = 0; i < numSegments; i++)
        {
            triangles[triIndex++] = 0;
            triangles[triIndex++] = (i == numSegments - 1) ? 1 : i + 2;
            triangles[triIndex++] = i + 1;
        }

        // Base
        for (int i = 0; i < numSegments; i++)
        {
            triangles[triIndex++] = numSegments + 1;
            triangles[triIndex++] = i + 1;
            triangles[triIndex++] = (i == numSegments - 1) ? 1 : i + 2;
        }

        mesh.triangles = triangles;

        // Normals
        mesh.RecalculateNormals();
    }
}

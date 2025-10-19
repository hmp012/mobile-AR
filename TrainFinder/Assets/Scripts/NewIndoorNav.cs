using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;

// A new helper class to link image names to positions in the 3D model.
[System.Serializable]
public class ImageAnchor
{
    // The name of the reference image (must match the name in your ReferenceImageLibrary).
    public string imageName;
    // The transform of the empty GameObject marking the image's location in the 3D model.
    public Transform anchorTransform;
}

public class NewIndoorNav : MonoBehaviour {
    [Header("AR Setup")]
    [SerializeField] private ARTrackedImageManager m_TrackedImageManager;
    // This is the XROrigin, which represents the user's camera rig.
    [SerializeField] private Transform xrOrigin;

    [Header("Navigation Setup")]
    // The NavMeshSurface component on your static 3D model.
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private LineRenderer line;
    // A list of all possible navigation targets in the scene.
    [SerializeField] private List<NavigationTarget> navigationTargets;
    
    [Header("Visualization")]
    [Tooltip("Height above the target to place the marker")]
    [SerializeField] private float markerHeight = 2.0f;
    [Tooltip("Distance ahead to look for next waypoint")]
    [SerializeField] private float waypointDistanceThreshold = 2.0f;
    [Tooltip("Size of the destination marker")]
    [SerializeField] private float markerSize = 0.5f;

    [Header("Image Anchors")]
    // The list where you will link image names to their 3D locations.
    [SerializeField] private List<ImageAnchor> imageAnchors;
    
    [Header("Simple Mode")]
    [Tooltip("If true, the virtual world will simply appear at the tracked image location without complex alignment")]
    [SerializeField] private bool useSimpleLocalization = true;

    private NavMeshPath navMeshPath;
    private bool isLocalized = false;
    private GameObject destinationMarker;
    private GameObject arrow3D;
    private TextMesh distanceText3D;
    private int currentWaypointIndex = 0;
    private Camera arCamera;

    private void Start() {
        navMeshPath = new NavMeshPath();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        
        Debug.Log("=== NewIndoorNav START ===");
        
        // IMPORTANT: Disable the line renderer immediately
        if (line != null)
        {
            line.enabled = false;
            line.gameObject.SetActive(false);
            Debug.Log("Line Renderer disabled at start");
        }
        
        // Create a destination marker
        CreateDestinationMarker();
        // Create 3D arrow
        Create3DArrow();
        
        Debug.Log("=== NewIndoorNav START COMPLETE ===");
    }
    
    private void CreateDestinationMarker() {
        // Create a simple marker - a bright colored cube/sphere
        destinationMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        destinationMarker.name = "DestinationMarker";
        destinationMarker.transform.localScale = Vector3.one * markerSize;
        
        // Remove collider so it doesn't interfere with anything
        Destroy(destinationMarker.GetComponent<Collider>());
        
        // Make it bright and visible
        Renderer renderer = destinationMarker.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (renderer.material.shader.name == "Hidden/InternalErrorShader")
        {
            // Fallback if URP shader not found
            renderer.material.shader = Shader.Find("Unlit/Color");
        }
        renderer.material.color = Color.green;
        
        // Start hidden
        destinationMarker.SetActive(false);
        
        Debug.Log("Destination marker created!");
    }
    
    private void Create3DArrow() {
        Debug.Log("Creating 3D Arrow...");
        
        arrow3D = new GameObject("3DNavigationArrow");
        
        // Create arrow using CUBES - MUCH SMALLER scales for AR
        // Main shaft - elongated cube pointing forward (Z-axis)
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.name = "ArrowShaft";
        shaft.transform.SetParent(arrow3D.transform, false);
        shaft.transform.localPosition = Vector3.zero;
        shaft.transform.localRotation = Quaternion.identity;
        shaft.transform.localScale = new Vector3(0.01f, 0.01f, 0.06f); // TINY - appropriate for AR
        Destroy(shaft.GetComponent<Collider>());
        
        // Arrow head - larger cube at the tip
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "ArrowHead";
        head.transform.SetParent(arrow3D.transform, false);
        head.transform.localPosition = new Vector3(0, 0, 0.04f); // At the front
        head.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f); // Small tip
        Destroy(head.GetComponent<Collider>());
        
        // Add side fins to make it more arrow-like
        GameObject finLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finLeft.name = "FinLeft";
        finLeft.transform.SetParent(arrow3D.transform, false);
        finLeft.transform.localPosition = new Vector3(-0.015f, 0, 0.03f);
        finLeft.transform.localScale = new Vector3(0.01f, 0.005f, 0.02f);
        Destroy(finLeft.GetComponent<Collider>());
        
        GameObject finRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finRight.name = "FinRight";
        finRight.transform.SetParent(arrow3D.transform, false);
        finRight.transform.localPosition = new Vector3(0.015f, 0, 0.03f);
        finRight.transform.localScale = new Vector3(0.01f, 0.005f, 0.02f);
        Destroy(finRight.GetComponent<Collider>());
        
        // Make arrow BRIGHT YELLOW for visibility
        Material arrowMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (arrowMaterial.shader.name == "Hidden/InternalErrorShader")
        {
            arrowMaterial.shader = Shader.Find("Unlit/Color");
        }
        arrowMaterial.color = Color.yellow; // Yellow
        
        // Make sure it renders on top
        arrowMaterial.renderQueue = 3000;
        
        shaft.GetComponent<Renderer>().material = arrowMaterial;
        head.GetComponent<Renderer>().material = arrowMaterial;
        finLeft.GetComponent<Renderer>().material = arrowMaterial;
        finRight.GetComponent<Renderer>().material = arrowMaterial;
        
        // Create 3D text for distance - MUCH SMALLER
        GameObject textObj = new GameObject("DistanceText3D");
        textObj.transform.SetParent(arrow3D.transform, false);
        textObj.transform.localPosition = new Vector3(0, 0.04f, 0); // Above arrow
        
        distanceText3D = textObj.AddComponent<TextMesh>();
        distanceText3D.text = "0m";
        
        // Load Arial font
        Font arial = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        if (arial != null)
        {
            distanceText3D.font = arial;
            textObj.GetComponent<MeshRenderer>().material = arial.material;
        }
        else
        {
            Debug.LogWarning("Arial font not found!");
        }
        
        distanceText3D.fontSize = 20;
        distanceText3D.characterSize = 0.002f; // VERY small text
        distanceText3D.anchor = TextAnchor.MiddleCenter;
        distanceText3D.alignment = TextAlignment.Center;
        distanceText3D.color = Color.white;
        
        arrow3D.SetActive(false);
        
        Debug.Log($"3D Arrow created! Arrow object: {arrow3D}, Children count: {arrow3D.transform.childCount}");
    }

    private void Update() {
        // Cache camera reference ONCE
        if (arCamera == null && xrOrigin != null)
        {
            arCamera = xrOrigin.GetComponentInChildren<Camera>();
            if (arCamera != null)
            {
                Debug.Log($"Camera found and cached: {arCamera.name}");
            }
        }
        
        // Less verbose logging - only every 60 frames
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[FRAME {Time.frameCount}] isLocalized={isLocalized}, targets={navigationTargets?.Count ?? 0}, camera={arCamera != null}");
            if (arrow3D != null)
            {
                Debug.Log($"[FRAME {Time.frameCount}] Arrow exists: active={arrow3D.activeSelf}, pos={arrow3D.transform.position}");
            }
        }
        
        // Only calculate a path if the user has been localized and there's a target.
        if (isLocalized && navigationTargets != null && navigationTargets.Count > 0 && arCamera != null) {
            // Get camera position EVERY FRAME in real-time
            Vector3 cameraPosition = arCamera.transform.position;
            Vector3 endPoint = navigationTargets[0].transform.position;
            
            // Position the destination marker above the target
            if (destinationMarker != null)
            {
                destinationMarker.SetActive(true);
                Vector3 markerPosition = endPoint + Vector3.up * markerHeight;
                destinationMarker.transform.position = markerPosition;
                destinationMarker.transform.Rotate(Vector3.up, 50 * Time.deltaTime);
            }

            // Calculate path on NavMesh EVERY FRAME using CURRENT camera position!
            NavMeshPath path = new NavMeshPath();
            bool pathFound = NavMesh.CalculatePath(cameraPosition, endPoint, NavMesh.AllAreas, path);

            if (pathFound && path.status == NavMeshPathStatus.PathComplete && path.corners != null && path.corners.Length > 0) {
                navMeshPath = path;
                
                // Find the next waypoint to navigate to based on CURRENT position
                Vector3 nextWaypoint = FindNextWaypoint(cameraPosition, navMeshPath.corners);
                
                // Update 3D arrow to point toward the waypoint - RUNS EVERY FRAME!
                if (arrow3D != null)
                {
                    arrow3D.SetActive(true);
                    
                    // Position arrow CLOSE to camera for visibility
                    float arrowDistance = 0.3f; // 30cm in front
                    Vector3 cameraForward = arCamera.transform.forward;
                    Vector3 arrowPosition = cameraPosition + cameraForward * arrowDistance;
                    
                    arrow3D.transform.position = arrowPosition;
                    
                    // Calculate FULL 3D direction to waypoint
                    Vector3 directionToWaypoint = (nextWaypoint - cameraPosition);
                    
                    // Make the arrow point in that FULL 3D direction
                    if (directionToWaypoint.sqrMagnitude > 0.001f)
                    {
                        directionToWaypoint.Normalize();
                        
                        // Rotate arrow to point at waypoint (Z-axis points forward)
                        Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
                        arrow3D.transform.rotation = targetRotation;
                        
                        // Calculate distance for text and logging
                        float distance = Vector3.Distance(cameraPosition, nextWaypoint);
                        
                        // Update distance text
                        if (distanceText3D != null)
                        {
                            distanceText3D.text = $"{distance:F1}m";
                            
                            // Make text face camera correctly
                            Vector3 cameraToText = distanceText3D.transform.position - arCamera.transform.position;
                            distanceText3D.transform.rotation = Quaternion.LookRotation(cameraToText);
                        }
                        
                        // Reduced logging
                        if (Time.frameCount % 30 == 0)
                        {
                            Debug.Log($"[FRAME {Time.frameCount}] Arrow: pos={arrow3D.transform.position.ToString("F2")}, rot={arrow3D.transform.rotation.eulerAngles.ToString("F1")}, dist={distance:F1}m");
                        }
                    }
                }
                
                // Disable the line
                if (line != null)
                {
                    line.enabled = false;
                }
            } else {
                if (arrow3D != null) arrow3D.SetActive(false);
                if (line != null) line.positionCount = 0;
            }
        } else {
            // Hide arrow when not navigating
            if (arrow3D != null) arrow3D.SetActive(false);
        }
    }
    
    private Vector3 FindNextWaypoint(Vector3 currentPosition, Vector3[] waypoints) {
        // Reset waypoint index if we're far from all waypoints (user moved back or repositioned)
        bool nearAnyWaypoint = false;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (Vector3.Distance(currentPosition, waypoints[i]) < waypointDistanceThreshold * 3)
            {
                nearAnyWaypoint = true;
                break;
            }
        }
        
        if (!nearAnyWaypoint)
        {
            currentWaypointIndex = 0; // Reset to first waypoint
        }
        
        // Find the next waypoint that's beyond the distance threshold
        for (int i = currentWaypointIndex; i < waypoints.Length; i++)
        {
            float distance = Vector3.Distance(currentPosition, waypoints[i]);
            
            // If we're close to current waypoint, move to next one
            if (distance < waypointDistanceThreshold && i < waypoints.Length - 1)
            {
                currentWaypointIndex = i + 1;
                Debug.Log($"Reached waypoint {i}, advancing to waypoint {currentWaypointIndex}");
                continue;
            }
            
            // Return the current target waypoint
            currentWaypointIndex = i;
            return waypoints[i];
        }
        
        // Return the last waypoint (destination)
        currentWaypointIndex = waypoints.Length - 1;
        return waypoints[waypoints.Length - 1];
    }

    private void OnEnable() => m_TrackedImageManager.trackablesChanged.AddListener(OnChanged);

    private void OnDisable() => m_TrackedImageManager.trackablesChanged.RemoveListener(OnChanged);

    private void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs) {
        // If we are already localized, do not attempt to re-localize or de-localize.
        if (isLocalized) {
            return;
        }

        Debug.Log("OnChanged: Event fired! Attempting to localize...");

        // Handle added and updated images to try and localize for the first time.
        foreach (var trackedImage in eventArgs.added.Concat(eventArgs.updated)) {
            Debug.Log($"OnChanged: Found tracked image '{trackedImage.referenceImage.name}'. Attempting to localize.");
            LocalizeUser(trackedImage);

            if (isLocalized) {
                Debug.Log("OnChanged: Localization successful!");
                break;
            }
        }
    }

    private void LocalizeUser(ARTrackedImage trackedImage) {
        Debug.Log($"LocalizeUser: Attempting to find anchor for image '{trackedImage.referenceImage.name}'.");

        // Check that AR tracking is stable
        if (trackedImage.trackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking || trackedImage.transform.position == Vector3.zero)
        {
            Debug.LogWarning($"LocalizeUser: Image '{trackedImage.referenceImage.name}' detected, but tracking not stable yet. " +
                             $"State: {trackedImage.trackingState}, Position: {trackedImage.transform.position}");
            return;
        }

        var anchor = imageAnchors.FirstOrDefault(a => a.imageName == trackedImage.referenceImage.name);

        if (anchor != null) {
            Debug.Log($"DIAGNOSTIC: Tracked image position in AR space: {trackedImage.transform.position.ToString("F3")}");
            Debug.Log($"DIAGNOSTIC: Virtual anchor position in scene: {anchor.anchorTransform.position.ToString("F3")}");

            // Get the camera to determine current user position
            Camera arCamera = xrOrigin.GetComponentInChildren<Camera>();
            if (arCamera == null)
            {
                Debug.LogError("No camera found in XR Origin! Cannot localize.");
                return;
            }

            // Get the camera's current position in AR space (before we move anything)
            Vector3 cameraPositionInARSpace = arCamera.transform.position;
            Quaternion cameraRotationInARSpace = arCamera.transform.rotation;
            
            Debug.Log($"DIAGNOSTIC: Camera position in AR space (before localization): {cameraPositionInARSpace.ToString("F3")}");

            if (useSimpleLocalization) {
                // SIMPLE MODE: 
                // The tracked image is at some position in AR space (e.g., 0.3m in front of camera)
                // The anchor marks where that image SHOULD be in the virtual world (e.g., at position 65, -0.7, 40)
                // After localization, we want the camera to be positioned in the virtual world
                // such that when it looks at the anchor position, it sees the tracked image
                
                // Calculate the offset from camera to tracked image in AR space
                Vector3 cameraToImage = trackedImage.transform.position - cameraPositionInARSpace;
                
                // Position the XR Origin so that: camera's world position + cameraToImage = anchor's position
                // This means: cameraWorldPos = anchor.position - cameraToImage
                // Since cameraWorldPos = xrOrigin.position + cameraLocalPos:
                // xrOrigin.position = anchor.position - cameraToImage - cameraLocalPos
                
                Vector3 cameraLocalPosition = arCamera.transform.localPosition;
                
                // First, handle rotation
                Quaternion imageRotationOffset = trackedImage.transform.rotation * Quaternion.Inverse(anchor.anchorTransform.rotation);
                xrOrigin.rotation = imageRotationOffset;
                
                // Then position: we want the camera to end up at (anchor.position - rotated(cameraToImage))
                Vector3 rotatedCameraToImage = xrOrigin.rotation * cameraToImage;
                Vector3 desiredCameraWorldPos = anchor.anchorTransform.position - rotatedCameraToImage;
                xrOrigin.position = desiredCameraWorldPos - (xrOrigin.rotation * cameraLocalPosition);
                
                Debug.Log($"Simple localization complete.");
                Debug.Log($"XR Origin position: {xrOrigin.position.ToString("F3")}");
                Debug.Log($"Camera should now be at: {arCamera.transform.position.ToString("F3")}");
                Debug.Log($"Distance from camera to anchor: {Vector3.Distance(arCamera.transform.position, anchor.anchorTransform.position).ToString("F2")}m");
            } else {
                // COMPLEX MODE: Original logic
                Quaternion rotationOffset = trackedImage.transform.rotation * Quaternion.Inverse(anchor.anchorTransform.rotation);
                xrOrigin.rotation = rotationOffset;
                Vector3 positionOffset = trackedImage.transform.position - (xrOrigin.rotation * anchor.anchorTransform.position);
                xrOrigin.position = positionOffset;
                
                Debug.Log($"Complex localization complete. XR Origin position: {xrOrigin.position.ToString("F3")}");
            }

            isLocalized = true;
        }
        else
        {
            Debug.LogWarning($"LocalizeUser: No matching anchor found for image '{trackedImage.referenceImage.name}'. Check your ImageAnchors list in the Inspector.");
        }
    }
}

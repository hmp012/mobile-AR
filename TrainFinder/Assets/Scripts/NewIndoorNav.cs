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
    [SerializeField] private GameObject arrowObject;
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
    
    [Header("Debug Settings")]
    [Tooltip("Enable verbose debug logging")]
    [SerializeField] private bool enableDebugLogs = true;
    [Tooltip("Automatically start navigation to first target after localization (for testing)")]
    [SerializeField] private bool autoStartNavigation = true; // Changed default to TRUE
    [Tooltip("Target name to navigate to when auto-start is enabled")]
    [SerializeField] private string autoNavigationTargetName = "";
    [Tooltip("If auto-navigation target name is empty, use first target in list")]
    [SerializeField] private bool useFirstTargetIfEmpty = true;

    private NavMeshPath navMeshPath;
    private bool isLocalized = false;
    private int currentWaypointIndex = 0;
    private Camera arCamera;
    private bool hasStartedNavigation = false;

    // New variables for arrow positioning
    [Header("Arrow Settings")]
    [SerializeField] private float arrowDistance = 0.5f; // Distance in front of the camera
    [SerializeField] private float arrowVerticalOffset = -0.2f; // Vertical offset
    [SerializeField] private float arrowScale = 0.5f; // Scale for the arrow

    private void Start() {
        navMeshPath = new NavMeshPath();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        
        DebugLog("=== NewIndoorNav START ===");
        
        // Validate all required components
        if (m_TrackedImageManager == null)
            Debug.LogError("❌ ARTrackedImageManager is NOT assigned!");
        else
            DebugLog("✓ ARTrackedImageManager is assigned");
            
        if (xrOrigin == null)
            Debug.LogError("❌ XR Origin is NOT assigned!");
        else
            DebugLog("✓ XR Origin is assigned");
            
        if (navMeshSurface == null)
            Debug.LogWarning("⚠️ NavMeshSurface is NOT assigned");
        else
            DebugLog("✓ NavMeshSurface is assigned");
            
        if (arrowObject == null)
            Debug.LogError("❌ Arrow Object is NOT assigned! This is why arrow won't show!");
        else
        {
            DebugLog($"✓ Arrow Object is assigned: {arrowObject.name}");
            DebugLog($"  Arrow initial position: {arrowObject.transform.position}");
            DebugLog($"  Arrow initial active state: {arrowObject.activeSelf}");
            arrowObject.SetActive(false);
            DebugLog("  Arrow set to inactive initially");
        }
        
        if (imageAnchors == null || imageAnchors.Count == 0)
            Debug.LogError("❌ No Image Anchors configured!");
        else
        {
            DebugLog($"✓ {imageAnchors.Count} Image Anchor(s) configured:");
            for (int i = 0; i < imageAnchors.Count; i++)
            {
                var anchor = imageAnchors[i];
                if (anchor.anchorTransform == null)
                    Debug.LogError($"  ❌ Anchor {i}: '{anchor.imageName}' has NO transform assigned!");
                else
                    DebugLog($"  ✓ Anchor {i}: '{anchor.imageName}' -> {anchor.anchorTransform.name} at {anchor.anchorTransform.position}");
            }
        }
        
        if (navigationTargets == null || navigationTargets.Count == 0)
            Debug.LogWarning("⚠️ No navigation targets configured");
        else
        {
            DebugLog($"✓ {navigationTargets.Count} navigation target(s) configured");
            for (int i = 0; i < navigationTargets.Count; i++)
            {
                DebugLog($"  Target {i}: '{navigationTargets[i].name}' at {navigationTargets[i].transform.position.ToString("F2")}");
            }
        }
        
        if (autoStartNavigation)
        {
            if (string.IsNullOrEmpty(autoNavigationTargetName))
                Debug.LogWarning("⚠️ Auto-navigation enabled but no target name specified!");
            else
                DebugLog($"✓ Auto-navigation enabled, will navigate to: '{autoNavigationTargetName}' after localization");
        }
        
        DebugLog("=== NewIndoorNav START COMPLETE ===");
    }
    
    private void Update()
    {
        // Cache camera reference ONCE
        if (arCamera == null && xrOrigin != null)
        {
            arCamera = xrOrigin.GetComponentInChildren<Camera>();
            if (arCamera != null)
                DebugLog($"Camera found and cached: {arCamera.name}");
        }

        // 🔍 NEW: Auto-start navigation after localization (for testing)
        if (isLocalized && !hasStartedNavigation && autoStartNavigation)
        {
            string targetToUse = autoNavigationTargetName;
            
            // If no target specified, use first target in list
            if (string.IsNullOrEmpty(targetToUse) && useFirstTargetIfEmpty && navigationTargets != null && navigationTargets.Count > 0)
            {
                targetToUse = navigationTargets[0].name;
                DebugLog($"🚀 Auto-starting navigation to FIRST target: '{targetToUse}'");
            }
            else if (!string.IsNullOrEmpty(targetToUse))
            {
                DebugLog($"🚀 Auto-starting navigation to specified target: '{targetToUse}'");
            }
            
            if (!string.IsNullOrEmpty(targetToUse))
            {
                hasStartedNavigation = true;
                StartNavigationToTarget(targetToUse);
            }
            else
            {
                Debug.LogError("❌ Auto-navigation enabled but no target available!");
                hasStartedNavigation = true; // Prevent repeated errors
            }
        }

        // Only proceed if we are localized and have a valid path to follow.
        if (!isLocalized || navMeshPath == null || navMeshPath.corners.Length == 0 || arCamera == null)
        {
            // Debug why we're not navigating
            if (!isLocalized && Time.frameCount % 300 == 0) // Log every ~5 seconds at 60fps
            {
                DebugLog("⏸️ Not navigating - NOT LOCALIZED yet. Waiting for image recognition...");
            }
            
            if (isLocalized && (navMeshPath == null || navMeshPath.corners.Length == 0) && Time.frameCount % 300 == 0)
            {
                Debug.LogWarning("⚠️ ⚠️ ⚠️ LOCALIZED BUT NO PATH! ⚠️ ⚠️ ⚠️");
                Debug.LogWarning($"   isLocalized = {isLocalized}");
                Debug.LogWarning($"   hasStartedNavigation = {hasStartedNavigation}");
                Debug.LogWarning($"   navMeshPath = {(navMeshPath != null ? "exists" : "NULL")}");
                Debug.LogWarning($"   navMeshPath.corners.Length = {(navMeshPath != null ? navMeshPath.corners.Length : 0)}");
                Debug.LogWarning("   ❓ Did you call StartNavigationToTarget()? Or enable autoStartNavigation?");
                if (navigationTargets != null && navigationTargets.Count > 0)
                {
                    Debug.LogWarning($"   💡 TIP: Call StartNavigationToTarget(\"{navigationTargets[0].name}\") to start navigation");
                }
            }
            
            // If we aren't navigating, make sure the arrow is hidden.
            if (arrowObject != null && arrowObject.activeSelf)
            {
                DebugLog("Hiding arrow - not navigating");
                arrowObject.SetActive(false);
            }

            return; // Exit the Update loop early.
        }

        // --- Core Navigation Logic ---

        Vector3 cameraPosition = arCamera.transform.position;
        Vector3[] pathCorners = navMeshPath.corners;

        // Find the next corner/waypoint the user should be heading towards.
        // Your FindNextWaypoint function is good for this.
        Vector3 nextWaypoint = FindNextWaypoint(cameraPosition, pathCorners);

        // --- 3D Arrow Visualization Logic ---
        if (arrowObject != null)
        {
            // 1. Make sure the arrow is visible now that we are navigating.
            if (!arrowObject.activeSelf)
            {
                DebugLog("🎯 ACTIVATING ARROW - Navigation is active!");
                arrowObject.SetActive(true);
                
                // Set arrow scale to make it visible but not overwhelming
                arrowObject.transform.localScale = Vector3.one * arrowScale;
                DebugLog($"   Arrow scale set to: {arrowScale}");
                
                // 🔍 DEBUG: Log arrow details when first activated
                DebugLog($"   Arrow GameObject: {arrowObject.name}");
                DebugLog($"   Arrow local scale: {arrowObject.transform.localScale}");
                DebugLog($"   Arrow world scale: {arrowObject.transform.lossyScale}");
                DebugLog($"   Arrow has MeshRenderer: {arrowObject.GetComponent<MeshRenderer>() != null}");
                DebugLog($"   Arrow children count: {arrowObject.transform.childCount}");
                
                var meshRenderer = arrowObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    DebugLog($"   Arrow MeshRenderer enabled: {meshRenderer.enabled}");
                    DebugLog($"   Arrow material: {(meshRenderer.material != null ? meshRenderer.material.name : "NULL")}");
                }
                
                // Check child renderers
                var childRenderers = arrowObject.GetComponentsInChildren<MeshRenderer>();
                DebugLog($"   Arrow total renderers (including children): {childRenderers.Length}");
                foreach (var renderer in childRenderers)
                {
                    DebugLog($"      - {renderer.gameObject.name}: enabled={renderer.enabled}, material={renderer.material?.name}");
                }
            }

            // 2. Position the arrow a fixed distance in front of the camera.
            // This keeps it in the user's view.
            Vector3 arrowPosition = cameraPosition + (arCamera.transform.forward * arrowDistance);

            // Add vertical offset to position it in the view (negative = lower)
            arrowPosition.y += arrowVerticalOffset;

            arrowObject.transform.position = arrowPosition;
            
            // 🔍 DEBUG: Log arrow position every 60 frames (~1 second)
            if (Time.frameCount % 60 == 0)
            {
                DebugLog($"📍 Arrow position: {arrowPosition.ToString("F2")}");
                DebugLog($"   Camera position: {cameraPosition.ToString("F2")}");
                DebugLog($"   Camera forward: {arCamera.transform.forward.ToString("F2")}");
                DebugLog($"   Distance from camera: {Vector3.Distance(cameraPosition, arrowPosition):F2}m");
                DebugLog($"   Next waypoint: {nextWaypoint.ToString("F2")}");
                DebugLog($"   Arrow world position: {arrowObject.transform.position.ToString("F2")}");
                DebugLog($"   Arrow local position: {arrowObject.transform.localPosition.ToString("F2")}");
                DebugLog($"   Arrow rotation: {arrowObject.transform.rotation.eulerAngles.ToString("F1")}");
            }

            // 3. Calculate the direction from the arrow to the next waypoint.
            Vector3 directionToWaypoint = (nextWaypoint - arrowPosition).normalized;

            // 4. Rotate the arrow to point in that direction.
            // Quaternion.LookRotation creates a rotation that looks along the forward vector.
            if (directionToWaypoint != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);

                // Optional: Smoothly rotate the arrow instead of snapping it instantly.
                arrowObject.transform.rotation =
                    Quaternion.Slerp(arrowObject.transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
        else if (Time.frameCount % 300 == 0)
        {
            Debug.LogError("❌ Arrow object is NULL during navigation!");
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
                DebugLog($"Reached waypoint {i}, advancing to waypoint {currentWaypointIndex}");
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

    private void OnEnable() 
    {
        DebugLog("OnEnable: Subscribing to trackablesChanged event");
        m_TrackedImageManager.trackablesChanged.AddListener(OnChanged);
    }

    private void OnDisable() 
    {
        DebugLog("OnDisable: Unsubscribing from trackablesChanged event");
        m_TrackedImageManager.trackablesChanged.RemoveListener(OnChanged);
    }

    private void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs) {
        DebugLog($"📸 OnChanged: Event fired! Added: {eventArgs.added.Count}, Updated: {eventArgs.updated.Count}, Removed: {eventArgs.removed.Count}");
        
        // If we are already localized, do not attempt to re-localize or de-localize.
        if (isLocalized) {
            DebugLog("Already localized - ignoring trackable changes");
            return;
        }

        // Handle added and updated images to try and localize for the first time.
        foreach (var trackedImage in eventArgs.added.Concat(eventArgs.updated)) {
            DebugLog($"📸 Processing tracked image: '{trackedImage.referenceImage.name}'");
            DebugLog($"   Tracking state: {trackedImage.trackingState}");
            DebugLog($"   Position: {trackedImage.transform.position}");
            DebugLog($"   Rotation: {trackedImage.transform.rotation.eulerAngles}");
            
            LocalizeUser(trackedImage);

            if (isLocalized) {
                DebugLog("✅ LOCALIZATION SUCCESSFUL!");
                DebugLog($"   isLocalized = {isLocalized}");
                DebugLog($"   Path exists = {navMeshPath != null}");
                DebugLog($"   Path corners = {(navMeshPath != null ? navMeshPath.corners.Length : 0)}");
                DebugLog("   NOTE: Arrow will only show when navigation path is set!");
                break;
            }
        }
    }

    private void LocalizeUser(ARTrackedImage trackedImage) {
        DebugLog($"🔍 LocalizeUser: Attempting to find anchor for image '{trackedImage.referenceImage.name}'");

        // Check that AR tracking is stable
        if (trackedImage.trackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking || trackedImage.transform.position == Vector3.zero)
        {
            Debug.LogWarning($"⚠️ Image '{trackedImage.referenceImage.name}' detected, but tracking not stable yet. " +
                             $"State: {trackedImage.trackingState}, Position: {trackedImage.transform.position}");
            return;
        }

        var anchor = imageAnchors.FirstOrDefault(a => a.imageName == trackedImage.referenceImage.name);

        if (anchor == null)
        {
            Debug.LogWarning($"❌ No matching anchor found for image '{trackedImage.referenceImage.name}'!");
            DebugLog("Available anchors:");
            foreach (var a in imageAnchors)
            {
                DebugLog($"  - '{a.imageName}'");
            }
            return;
        }
        
        DebugLog($"✓ Found matching anchor for '{trackedImage.referenceImage.name}'");
        DebugLog($"📍 Tracked image position in AR space: {trackedImage.transform.position.ToString("F3")}");
        DebugLog($"📍 Virtual anchor position in scene: {anchor.anchorTransform.position.ToString("F3")}");

        // Get the camera to determine current user position
        Camera arCamera = xrOrigin.GetComponentInChildren<Camera>();
        if (arCamera == null)
        {
            Debug.LogError("❌ No camera found in XR Origin! Cannot localize.");
            return;
        }

        // Get the camera's current position in AR space (before we move anything)
        Vector3 cameraPositionInARSpace = arCamera.transform.position;
        Quaternion cameraRotationInARSpace = arCamera.transform.rotation;
        
        DebugLog($"📷 Camera position in AR space (before localization): {cameraPositionInARSpace.ToString("F3")}");

        if (useSimpleLocalization) {
            DebugLog("Using SIMPLE localization mode");
            
            // Calculate the offset from camera to tracked image in AR space
            Vector3 cameraToImage = trackedImage.transform.position - cameraPositionInARSpace;
            
            Vector3 cameraLocalPosition = arCamera.transform.localPosition;
            
            // First, handle rotation
            Quaternion imageRotationOffset = trackedImage.transform.rotation * Quaternion.Inverse(anchor.anchorTransform.rotation);
            xrOrigin.rotation = imageRotationOffset;
            
            // Then position: we want the camera to end up at (anchor.position - rotated(cameraToImage))
            Vector3 rotatedCameraToImage = xrOrigin.rotation * cameraToImage;
            Vector3 desiredCameraWorldPos = anchor.anchorTransform.position - rotatedCameraToImage;
            xrOrigin.position = desiredCameraWorldPos - (xrOrigin.rotation * cameraLocalPosition);
            
            DebugLog($"✅ Simple localization complete");
            DebugLog($"   XR Origin position: {xrOrigin.position.ToString("F3")}");
            DebugLog($"   XR Origin rotation: {xrOrigin.rotation.eulerAngles.ToString("F1")}");
            DebugLog($"   Camera now at: {arCamera.transform.position.ToString("F3")}");
            DebugLog($"   Distance from camera to anchor: {Vector3.Distance(arCamera.transform.position, anchor.anchorTransform.position).ToString("F2")}m");
        } else {
            DebugLog("Using COMPLEX localization mode");
            
            // COMPLEX MODE: Original logic
            Quaternion rotationOffset = trackedImage.transform.rotation * Quaternion.Inverse(anchor.anchorTransform.rotation);
            xrOrigin.rotation = rotationOffset;
            Vector3 positionOffset = trackedImage.transform.position - (xrOrigin.rotation * anchor.anchorTransform.position);
            xrOrigin.position = positionOffset;
            
            DebugLog($"✅ Complex localization complete");
            DebugLog($"   XR Origin position: {xrOrigin.position.ToString("F3")}");
            DebugLog($"   XR Origin rotation: {xrOrigin.rotation.eulerAngles.ToString("F1")}");
        }

        isLocalized = true;
        DebugLog($"🎉 isLocalized set to TRUE");
        DebugLog($"⚠️ IMPORTANT: Arrow will NOT appear until you start navigation by setting a path!");
        DebugLog($"   Current path corners: {(navMeshPath != null ? navMeshPath.corners.Length : 0)}");
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[NewIndoorNav] {message}");
        }
    }
    
    // Public method to start navigation to a target - you'll need to call this somewhere
    public void StartNavigationToTarget(string targetName)
    {
        DebugLog($"🎯 StartNavigationToTarget called for: '{targetName}'");
        
        if (!isLocalized)
        {
            Debug.LogError("❌ Cannot start navigation - not localized yet!");
            return;
        }
        
        var target = navigationTargets.FirstOrDefault(t => t.name == targetName);
        if (target == null)
        {
            Debug.LogError($"❌ Navigation target '{targetName}' not found!");
            DebugLog("Available targets:");
            foreach (var t in navigationTargets)
            {
                DebugLog($"  - '{t.name}'");
            }
            return;
        }
        
        Camera cam = xrOrigin.GetComponentInChildren<Camera>();
        if (cam == null)
        {
            Debug.LogError("❌ Camera not found!");
            return;
        }
        
        Vector3 startPos = cam.transform.position;
        Vector3 targetPos = target.transform.position;
        
        DebugLog($"   Start position: {startPos.ToString("F2")}");
        DebugLog($"   Target position: {targetPos.ToString("F2")}");
        
        // 🔍 NEW: Check if positions are on NavMesh
        NavMeshHit startHit;
        bool startOnNavMesh = NavMesh.SamplePosition(startPos, out startHit, 5.0f, NavMesh.AllAreas);
        
        NavMeshHit targetHit;
        bool targetOnNavMesh = NavMesh.SamplePosition(targetPos, out targetHit, 5.0f, NavMesh.AllAreas);
        
        DebugLog($"   🔍 NavMesh Diagnostics:");
        DebugLog($"      Start position on NavMesh: {startOnNavMesh}");
        if (startOnNavMesh)
        {
            DebugLog($"         Nearest NavMesh point: {startHit.position.ToString("F2")}");
            DebugLog($"         Distance to NavMesh: {Vector3.Distance(startPos, startHit.position):F2}m");
        }
        else
        {
            Debug.LogError($"      ❌ START POSITION NOT ON NAVMESH! Position: {startPos.ToString("F2")}");
            Debug.LogError($"         This is why path has 0 corners!");
            Debug.LogError($"         💡 Make sure NavMesh is baked and covers this area");
        }
        
        DebugLog($"      Target position on NavMesh: {targetOnNavMesh}");
        if (targetOnNavMesh)
        {
            DebugLog($"         Nearest NavMesh point: {targetHit.position.ToString("F2")}");
            DebugLog($"         Distance to NavMesh: {Vector3.Distance(targetPos, targetHit.position):F2}m");
        }
        else
        {
            Debug.LogError($"      ❌ TARGET POSITION NOT ON NAVMESH! Position: {targetPos.ToString("F2")}");
            Debug.LogError($"         This is why path has 0 corners!");
            Debug.LogError($"         💡 Make sure NavMesh is baked and covers this area");
        }
        
        // Try to calculate path
        Vector3 pathStart = startOnNavMesh ? startHit.position : startPos;
        Vector3 pathEnd = targetOnNavMesh ? targetHit.position : targetPos;
        
        bool pathFound = NavMesh.CalculatePath(pathStart, pathEnd, NavMesh.AllAreas, navMeshPath);
        
        DebugLog($"   NavMesh.CalculatePath result: {pathFound}");
        DebugLog($"   Path status: {navMeshPath.status}");
        DebugLog($"   Path corners: {navMeshPath.corners.Length}");
        
        if (pathFound && navMeshPath.corners.Length > 0)
        {
            DebugLog($"✅ Path calculated successfully! {navMeshPath.corners.Length} waypoints");
            currentWaypointIndex = 0;
            
            for (int i = 0; i < navMeshPath.corners.Length; i++)
            {
                DebugLog($"   Waypoint {i}: {navMeshPath.corners[i].ToString("F2")}");
            }
            
            DebugLog("🎯 Arrow should now be visible in Update loop!");
        }
        else
        {
            Debug.LogError($"❌ ❌ ❌ FAILED TO CALCULATE PATH! ❌ ❌ ❌");
            Debug.LogError($"   pathFound: {pathFound}");
            Debug.LogError($"   path.status: {navMeshPath.status}");
            Debug.LogError($"   path.corners.Length: {navMeshPath.corners.Length}");
            
            if (navMeshPath.status == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogError("   🔍 PathInvalid - No valid path exists between start and target");
                Debug.LogError("   💡 Possible causes:");
                Debug.LogError("      1. NavMesh not baked - Check NavMeshSurface and bake it");
                Debug.LogError("      2. Start or target position not on NavMesh (see above)");
                Debug.LogError("      3. NavMesh doesn't connect start to target (gaps/obstacles)");
            }
            else if (navMeshPath.status == NavMeshPathStatus.PathPartial)
            {
                Debug.LogError("   🔍 PathPartial - Can only reach part way to target");
                Debug.LogError("   💡 There may be obstacles or gaps in the NavMesh");
            }
            
            // Additional NavMesh info
            if (navMeshSurface != null)
            {
                DebugLog($"   NavMeshSurface exists: {navMeshSurface.name}");
                DebugLog($"   NavMeshSurface active: {navMeshSurface.gameObject.activeInHierarchy}");
                DebugLog($"   NavMeshSurface navMeshData: {(navMeshSurface.navMeshData != null ? "EXISTS" : "NULL - NOT BAKED!")}");
                
                if (navMeshSurface.navMeshData == null)
                {
                    Debug.LogError("   ❌ ❌ ❌ NAVMESH NOT BAKED! ❌ ❌ ❌");
                    Debug.LogError("   💡 Select the NavMeshSurface object in Unity and click 'Bake'");
                }
            }
            else
            {
                Debug.LogError("   NavMeshSurface is NULL!");
            }
        }
    }
    
    // 🔍 NEW: Public method to check NavMesh status at any time
    public void DebugNavMeshStatus()
    {
        DebugLog("=== NAVMESH STATUS DEBUG ===");
        
        Camera cam = xrOrigin?.GetComponentInChildren<Camera>();
        if (cam == null)
        {
            Debug.LogError("No camera found");
            return;
        }
        
        Vector3 camPos = cam.transform.position;
        DebugLog($"Camera position: {camPos.ToString("F2")}");
        
        NavMeshHit hit;
        bool onNavMesh = NavMesh.SamplePosition(camPos, out hit, 10.0f, NavMesh.AllAreas);
        
        DebugLog($"Camera on NavMesh (10m radius): {onNavMesh}");
        if (onNavMesh)
        {
            DebugLog($"   Nearest NavMesh point: {hit.position.ToString("F2")}");
            DebugLog($"   Distance: {Vector3.Distance(camPos, hit.position):F2}m");
        }
        
        if (navMeshSurface != null)
        {
            DebugLog($"NavMeshSurface: {navMeshSurface.name}");
            DebugLog($"   Active: {navMeshSurface.gameObject.activeInHierarchy}");
            DebugLog($"   NavMeshData: {(navMeshSurface.navMeshData != null ? "Baked" : "NOT BAKED!")}");
        }
        else
        {
            Debug.LogError("NavMeshSurface is NULL!");
        }
        
        if (navigationTargets != null)
        {
            DebugLog($"Checking {navigationTargets.Count} navigation targets:");
            foreach (var target in navigationTargets)
            {
                NavMeshHit targetHit;
                bool targetOnMesh = NavMesh.SamplePosition(target.transform.position, out targetHit, 5.0f, NavMesh.AllAreas);
                DebugLog($"   '{target.name}' at {target.transform.position.ToString("F2")} - On NavMesh: {targetOnMesh}");
            }
        }
        
        DebugLog("=== END NAVMESH STATUS ===");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.AI;

/// <summary>
/// Handles AR image tracking and user localization in the virtual world.
/// When a tracked image is detected, the XR Origin is moved to align the virtual anchor
/// with the real-world image position.
/// </summary>
public class ImageLocalization : MonoBehaviour
{
    [System.Serializable]
    public class ImageAnchor
    {
        public string imageName;
        public Transform anchorTransform;
    }

    [Header("AR Components")]
    [Tooltip("Reference to the AR Tracked Image Manager")]
    public ARTrackedImageManager trackedImageManager;
    
    [Tooltip("Reference to the XR Origin transform")]
    public Transform xrOrigin;

    [Header("Image Anchors")]
    [Tooltip("List of image names and their corresponding virtual anchor positions")]
    public List<ImageAnchor> imageAnchors = new List<ImageAnchor>();

    [Header("Localization Settings")]
    [Tooltip("Use simple localization algorithm (recommended)")]
    public bool useSimpleLocalization = true;
    
    [Tooltip("Enable detailed debug logging")]
    public bool enableDebugLogs = true;

    [Header("Navigation (Optional)")]
    [Tooltip("Reference to NavMesh path for navigation (optional)")]
    public NavMeshPath navMeshPath;
    
    [Tooltip("Target object with NavigationTarget component")]
    public GameObject navigationTarget;
    
    [Tooltip("Automatically calculate path after localization")]
    public bool autoCalculatePathOnLocalization = true;
    
    [Tooltip("Update path continuously in Update method")]
    public bool updatePathContinuously = true;
    
    [Tooltip("Minimum time between path recalculations (seconds)")]
    public float pathUpdateInterval = 0.5f;

    // State
    private bool isLocalized = false;
    private Dictionary<TrackableId, ARTrackedImage> trackedImages = new Dictionary<TrackableId, ARTrackedImage>();
    private float lastPathUpdateTime = 0f;

    private void OnEnable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }
        else
        {
            Debug.LogError("❌ ARTrackedImageManager is not assigned!");
        }
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }
    }

    private void Start()
    {
        // Auto-find components if not assigned
        if (trackedImageManager == null)
        {
            trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
            if (trackedImageManager != null)
            {
                DebugLog("✓ Auto-found ARTrackedImageManager");
            }
        }

        if (xrOrigin == null)
        {
            // Try to find XR Origin
            var xrOriginComponent = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOriginComponent != null)
            {
                xrOrigin = xrOriginComponent.transform;
                DebugLog("✓ Auto-found XR Origin");
            }
            else
            {
                Debug.LogError("❌ XR Origin not found! Please assign it manually.");
            }
        }
        
        // Auto-find navigation target if not assigned
        if (navigationTarget == null)
        {
            NavigationTarget targetComponent = FindObjectOfType<NavigationTarget>();
            if (targetComponent != null)
            {
                navigationTarget = targetComponent.gameObject;
                DebugLog($"✓ Auto-found NavigationTarget: {navigationTarget.name}");
            }
        }
        
        // Initialize NavMeshPath if needed
        if (navMeshPath == null)
        {
            navMeshPath = new NavMeshPath();
            DebugLog("✓ Created new NavMeshPath");
        }

        DebugLog($"🚀 ImageLocalization initialized with {imageAnchors.Count} anchors");
        if (imageAnchors.Count == 0)
        {
            Debug.LogWarning("⚠️ No image anchors configured! Add them in the Inspector.");
        }
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Handle newly detected images
        foreach (var trackedImage in eventArgs.added)
        {
            trackedImages[trackedImage.trackableId] = trackedImage;
            DebugLog($"🆕 New image detected: '{trackedImage.referenceImage.name}'");
            
            // Try to localize immediately
            if (!isLocalized)
            {
                LocalizeUser(trackedImage);
            }
        }

        // Handle updated images
        foreach (var trackedImage in eventArgs.updated)
        {
            trackedImages[trackedImage.trackableId] = trackedImage;
            
            // If not yet localized and image tracking is stable, try to localize
            if (!isLocalized && trackedImage.trackingState == TrackingState.Tracking)
            {
                DebugLog($"🔄 Image updated and tracking: '{trackedImage.referenceImage.name}'");
                LocalizeUser(trackedImage);
            }
        }

        // Handle removed images
        foreach (var trackedImage in eventArgs.removed)
        {
            if (trackedImages.ContainsKey(trackedImage.trackableId))
            {
                trackedImages.Remove(trackedImage.trackableId);
                DebugLog($"🗑️ Image removed: '{trackedImage.referenceImage.name}'");
            }
        }
    }

    private void LocalizeUser(ARTrackedImage trackedImage)
    {
        DebugLog($"🔍 LocalizeUser: Attempting to find anchor for image '{trackedImage.referenceImage.name}'");

        // Check that AR tracking is stable
        if (trackedImage.trackingState != TrackingState.Tracking || trackedImage.transform.position == Vector3.zero)
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

        if (useSimpleLocalization)
        {
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
        }
        else
        {
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
        
        // Calculate NavMesh path if enabled
        if (autoCalculatePathOnLocalization && navMeshPath != null && navigationTarget != null)
        {
            CalculatePathToTarget(navigationTarget.transform.position);
        }
    }

    /// <summary>
    /// Manually trigger localization for a specific image (useful for testing)
    /// </summary>
    public void ManualLocalization(string imageName)
    {
        var trackedImage = trackedImages.Values.FirstOrDefault(img => img.referenceImage.name == imageName);
        if (trackedImage != null)
        {
            LocalizeUser(trackedImage);
        }
        else
        {
            Debug.LogWarning($"⚠️ No tracked image found with name '{imageName}'");
        }
    }

    /// <summary>
    /// Check if user is currently localized
    /// </summary>
    public bool IsLocalized()
    {
        return isLocalized;
    }

    /// <summary>
    /// Reset localization state
    /// </summary>
    public void ResetLocalization()
    {
        isLocalized = false;
        DebugLog("🔄 Localization reset");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ImageLocalization] {message}");
        }
    }

    // Visualize anchors in Scene view
    private void OnDrawGizmos()
    {
        if (imageAnchors == null || imageAnchors.Count == 0) return;

        foreach (var anchor in imageAnchors)
        {
            if (anchor.anchorTransform != null)
            {
                // Draw a sphere at anchor position
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(anchor.anchorTransform.position, 0.1f);
                
                // Draw anchor orientation
                Gizmos.color = Color.red;
                Gizmos.DrawRay(anchor.anchorTransform.position, anchor.anchorTransform.forward * 0.5f);
                
                // Draw label
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(anchor.anchorTransform.position + Vector3.up * 0.2f, 
                    $"Anchor: {anchor.imageName}");
                #endif
            }
        }
    }

    /// <summary>
    /// Calculate NavMesh path to a target position
    /// </summary>
    public void CalculatePathToTarget(Vector3 targetPosition)
    {
        if (navMeshPath == null)
        {
            Debug.LogWarning("⚠️ NavMeshPath is not assigned!");
            return;
        }

        // Use camera position as start point instead of XR Origin
        Camera arCamera = xrOrigin.GetComponentInChildren<Camera>();
        Vector3 startPosition = arCamera != null ? arCamera.transform.position : xrOrigin.position;
        
        DebugLog($"🔍 Attempting path calculation:");
        DebugLog($"   Raw start position (camera): {startPosition.ToString("F3")}");
        DebugLog($"   Raw target position: {targetPosition.ToString("F3")}");
        
        // Sample nearest point on NavMesh for both start and end positions
        NavMeshHit startHit;
        NavMeshHit endHit;
        
        bool startOnNavMesh = NavMesh.SamplePosition(startPosition, out startHit, 10.0f, NavMesh.AllAreas);
        bool endOnNavMesh = NavMesh.SamplePosition(targetPosition, out endHit, 10.0f, NavMesh.AllAreas);
        
        DebugLog($"   Start on NavMesh: {startOnNavMesh}, Distance to NavMesh: {(startOnNavMesh ? Vector3.Distance(startPosition, startHit.position).ToString("F3") : "N/A")}");
        DebugLog($"   End on NavMesh: {endOnNavMesh}, Distance to NavMesh: {(endOnNavMesh ? Vector3.Distance(targetPosition, endHit.position).ToString("F3") : "N/A")}");
        
        if (!startOnNavMesh)
        {
            Debug.LogWarning($"⚠️ Start position {startPosition.ToString("F3")} is not on NavMesh! Searched within 10m radius.");
            Debug.LogWarning($"   Check that your scene has a baked NavMesh and the camera is within 10m of it.");
            return;
        }
        
        if (!endOnNavMesh)
        {
            Debug.LogWarning($"⚠️ Target position {targetPosition.ToString("F3")} is not on NavMesh! Searched within 10m radius.");
            Debug.LogWarning($"   Check that your NavigationTarget is placed on a NavMesh surface.");
            return;
        }
        
        // Calculate path using NavMesh positions
        bool pathFound = NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, navMeshPath);
        
        DebugLog($"   NavMesh.CalculatePath returned: {pathFound}");
        DebugLog($"   Path status: {navMeshPath.status}");
        DebugLog($"   Path corners: {navMeshPath.corners.Length}");
        
        // Accept both complete and partial paths for now to debug
        if (pathFound && navMeshPath.corners.Length >= 2)
        {
            if (navMeshPath.status == NavMeshPathStatus.PathComplete)
            {
                DebugLog($"✅ Path calculated successfully!");
                DebugLog($"   From: {startHit.position.ToString("F3")}");
                DebugLog($"   To: {endHit.position.ToString("F3")}");
                DebugLog($"   Path corners: {navMeshPath.corners.Length}");
                DebugLog($"   Path status: {navMeshPath.status}");
            }
            else if (navMeshPath.status == NavMeshPathStatus.PathPartial)
            {
                Vector3 pathEnd = navMeshPath.corners[navMeshPath.corners.Length - 1];
                float distanceToTarget = Vector3.Distance(pathEnd, endHit.position);
                
                Debug.LogWarning($"⚠️ NAVMESH GAP DETECTED! Path is partial.");
                Debug.LogWarning($"   Status: {navMeshPath.status}");
                Debug.LogWarning($"   Start position: {startHit.position.ToString("F3")}");
                Debug.LogWarning($"   Target position: {endHit.position.ToString("F3")}");
                Debug.LogWarning($"   Path reaches to: {pathEnd.ToString("F3")}");
                Debug.LogWarning($"   Distance from path end to target: {distanceToTarget.ToString("F2")}m");
                Debug.LogWarning($"   Straight-line distance: {Vector3.Distance(startHit.position, endHit.position).ToString("F2")}m");
                Debug.LogWarning($"");
                Debug.LogWarning($"🔧 TO FIX: There is a gap in your NavMesh between:");
                Debug.LogWarning($"   Gap starts near: {pathEnd.ToString("F3")}");
                Debug.LogWarning($"   Gap ends near: {endHit.position.ToString("F3")}");
                Debug.LogWarning($"   Re-bake your NavMesh to include this area, or move your NavigationTarget.");
                Debug.LogWarning($"");
                Debug.LogWarning($"   TEMPORARILY SHOWING PARTIAL PATH - Look for RED sphere at gap location");
                // Don't clear the path - let's see what it looks like
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ Failed to calculate path!");
            Debug.LogWarning($"   pathFound: {pathFound}");
            Debug.LogWarning($"   Status: {navMeshPath.status}");
            Debug.LogWarning($"   Corners: {navMeshPath.corners.Length}");
            Debug.LogWarning($"   Start: {startHit.position.ToString("F3")}");
            Debug.LogWarning($"   End: {endHit.position.ToString("F3")}");
            navMeshPath.ClearCorners();
        }
    }

    /// <summary>
    /// Set the navigation target and optionally calculate the path
    /// </summary>
    public void SetNavigationTarget(GameObject target, bool calculatePath = true)
    {
        navigationTarget = target;
        DebugLog($"🎯 Navigation target set to: {target.name}");
        
        if (calculatePath)
        {
            CalculatePathToTarget(target.transform.position);
        }
    }

    private void Update()
    {
        // Continuously update path if enabled and localized
        if (updatePathContinuously && isLocalized && navigationTarget != null)
        {
            // Check if enough time has passed since last update
            if (Time.time - lastPathUpdateTime >= pathUpdateInterval)
            {
                CalculatePathToTarget(navigationTarget.transform.position);
                lastPathUpdateTime = Time.time;
            }
        }
    }
}

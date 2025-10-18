
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

    [Header("Image Anchors")]
    // The list where you will link image names to their 3D locations.
    [SerializeField] private List<ImageAnchor> imageAnchors;

    private NavMeshPath navMeshPath;
    private bool isLocalized = false;

    private void Start() {
        navMeshPath = new NavMeshPath();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // --- FOR EDITOR TESTING ---
        // Force localization to true so we can test navigation without image tracking.
        // isLocalized = true;
    }

        private void Update() {
            // Only calculate a path if the user has been localized and there's a target.
            if (isLocalized && navigationTargets.Count > 0) {
                // Use the main camera's position as the starting point for pathfinding.
                Vector3 startPoint = xrOrigin.GetComponentInChildren<Camera>().transform.position;
                Vector3 endPoint = navigationTargets[0].transform.position;
    
                // --- DEBUG LOGS ---
                Debug.Log("Update: Localized. Attempting to calculate path from: " + startPoint + " to: " + endPoint);
    
                NavMesh.CalculatePath(startPoint, endPoint, NavMesh.AllAreas, navMeshPath);
    
                // Let's see what the status of the path is.
                Debug.Log("Update: Path Status: " + navMeshPath.status);
    
                if (navMeshPath.status == NavMeshPathStatus.PathComplete) {
                    // And let's see how many points are in our path.
                    Debug.Log("Update: Path found with " + navMeshPath.corners.Length + " corners.");
    
                    if (line == null)
                    {
                        Debug.LogError("Line Renderer is not assigned in the Inspector!");
                        return;
                    }
    
                    if (!line.enabled)
                    {
                        Debug.LogWarning("Line Renderer component is disabled!");
                    }
    
                    line.positionCount = navMeshPath.corners.Length;
                    line.SetPositions(navMeshPath.corners);
    
                    if (navMeshPath.corners.Length > 0)
                    {
                        Debug.Log("Update: Line drawn from " + navMeshPath.corners[0] + " to " + navMeshPath.corners[navMeshPath.corners.Length - 1]);
                    }
                } else {
                    Debug.LogWarning("Update: Path calculation failed or is incomplete.");
                    // If path is not complete, ensure the line is hidden.
                    if(line != null) line.positionCount = 0;
                }
            }
        }
    
        private void OnEnable() => m_TrackedImageManager.trackablesChanged.AddListener(OnChanged);
    
        private void OnDisable() => m_TrackedImageManager.trackablesChanged.RemoveListener(OnChanged);
    
        private void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs) {
            // If we are already localized, do not attempt to re-localize or de-localize.
            // The device's own tracking will handle movement from this point.
            if (isLocalized) {
                return;
            }
    
            Debug.Log("OnChanged: Event fired! Attempting to localize...");
    
            // Handle added and updated images to try and localize for the first time.
            foreach (var trackedImage in eventArgs.added.Concat(eventArgs.updated)) {
                Debug.Log($"OnChanged: Found tracked image '{trackedImage.referenceImage.name}'. Attempting to localize.");
                // Attempt to localize
                LocalizeUser(trackedImage);
    
                // If localization was successful, stop processing more images.
                if (isLocalized) {
                    Debug.Log("OnChanged: Localization successful, breaking loop.");
                    break; // Exit the loop once we've localized successfully.
                }
            }
        }
    
            private void LocalizeUser(ARTrackedImage trackedImage) {
    
                Debug.Log($"LocalizeUser: Attempting to find anchor for image '{trackedImage.referenceImage.name}'.");
    
                // --- NEW STABILITY CHECK ---
                // Before we use the image's position, we must check that the AR system is tracking it reliably.
                // If the state is not 'Tracking' or the position is zero, the data is not yet reliable.
                if (trackedImage.trackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking || trackedImage.transform.position == Vector3.zero)
                {
                    Debug.LogWarning($"LocalizeUser: Image '{trackedImage.referenceImage.name}' detected, but its position is not yet stable. " +
                                     $"State: {trackedImage.trackingState}, Position: {trackedImage.transform.position}. Waiting for better tracking data.");
                    return; // Exit without localizing. The system will try again on the next update.
                }
    
                // Find the anchor that matches the name of the recognized image.
    
                var anchor = imageAnchors.FirstOrDefault(a => a.imageName == trackedImage.referenceImage.name);
    
        
    
                if (anchor != null) {
    
                    // --- NEW DIAGNOSTIC LOGS ---
    
                    Debug.Log($"DIAGNOSTIC: Real-world trackedImage.transform.position = {trackedImage.transform.position.ToString("F3")}");
                    Debug.Log($"DIAGNOSTIC: Virtual anchor.anchorTransform.position = {anchor.anchorTransform.position.ToString("F3")}");
    
                    // --- EXTENDED DIAGNOSTIC LOGS FOR ROTATION ---
                    Debug.Log($"DIAGNOSTIC: Real-world trackedImage.transform.rotation (Euler) = {trackedImage.transform.rotation.eulerAngles.ToString("F3")}");
                    Debug.Log($"DIAGNOSTIC: Virtual anchor.anchorTransform.rotation (Euler) = {anchor.anchorTransform.rotation.eulerAngles.ToString("F3")}");
    
                    Debug.Log($"LocalizeUser: Found matching anchor '{anchor.imageName}'. Performing localization.");
    
                    // We found a matching anchor. Now, align the virtual world with the real world.
    
                    // We do this by moving the XR Origin (the user) so that the virtual anchor
    
                    // appears exactly where the real-world image is.
    
        
    
                    // 1. Calculate the rotation difference between the real image and the virtual anchor.
    
                    Quaternion rotationOffset = trackedImage.transform.rotation * Quaternion.Inverse(anchor.anchorTransform.rotation);
    
        
    
                    // 2. Set the XR Origin's rotation.
    
                    xrOrigin.rotation = rotationOffset;
    
                    // --- MORE DIAGNOSTIC LOGS ---
                    Debug.Log($"DIAGNOSTIC: Calculated xrOrigin.rotation (Euler) = {xrOrigin.rotation.eulerAngles.ToString("F3")}");
                    Debug.Log($"DIAGNOSTIC: Anchor position rotated by new origin rotation = {(xrOrigin.rotation * anchor.anchorTransform.position).ToString("F3")}");
    
                    // 3. Calculate the position offset. We need to account for the new rotation.
    
                    Vector3 positionOffset = trackedImage.transform.position - (xrOrigin.rotation * anchor.anchorTransform.position);
    
        
    
                    // 4. Set the XR Origin's position.
    
                    xrOrigin.position = positionOffset;
    
        
    
                    isLocalized = true;
    
                    Debug.Log($"LocalizeUser: Localization complete. Final xrOrigin position = {xrOrigin.position.ToString("F3")}");
    
                }
    
                else
    
                {
    
                    Debug.LogWarning($"LocalizeUser: No matching anchor found for image '{trackedImage.referenceImage.name}'. Check your ImageAnchors list in the Inspector.");
    
                }
    
            }}

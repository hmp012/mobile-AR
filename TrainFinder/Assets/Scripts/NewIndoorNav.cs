
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
    }

    private void Update() {
        // Only calculate a path if the user has been localized and there's a target.
        if (isLocalized && navigationTargets.Count > 0) {
            // Use the main camera's position as the starting point for pathfinding.
            Vector3 startPoint = xrOrigin.GetComponentInChildren<Camera>().transform.position;
            NavMesh.CalculatePath(startPoint, navigationTargets[0].transform.position, NavMesh.AllAreas, navMeshPath);

            if (navMeshPath.status == NavMeshPathStatus.PathComplete) {
                line.positionCount = navMeshPath.corners.Length;
                line.SetPositions(navMeshPath.corners);
            } else {
                line.positionCount = 0;
            }
        }
    }

    private void OnEnable() => m_TrackedImageManager.trackablesChanged.AddListener(OnChanged);

    private void OnDisable() => m_TrackedImageManager.trackablesChanged.RemoveListener(OnChanged);

    private void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs) {
        // When an image is first detected or updated, relocalize the user.
        foreach (var trackedImage in eventArgs.added.Concat(eventArgs.updated)) {
            LocalizeUser(trackedImage);
        }

        // When an image is lost, you could optionally de-localize the user.
        foreach (var removedImage in eventArgs.removed) {
            isLocalized = false;
            line.positionCount = 0; // Hide the line
        }
    }

    private void LocalizeUser(ARTrackedImage trackedImage) {
        // Find the anchor that matches the name of the recognized image.
        var anchor = imageAnchors.FirstOrDefault(a => a.imageName == trackedImage.referenceImage.name);

        if (anchor != null) {
            // We found a matching anchor. Now, align the virtual world with the real world.
            // We do this by moving the XR Origin (the user) so that the virtual anchor
            // appears exactly where the real-world image is.

            // 1. Calculate the rotation difference between the real image and the virtual anchor.
            Quaternion rotationOffset = trackedImage.transform.rotation * Quaternion.Inverse(anchor.anchorTransform.rotation);

            // 2. Set the XR Origin's rotation.
            xrOrigin.rotation = rotationOffset;

            // 3. Calculate the position offset. We need to account for the new rotation.
            Vector3 positionOffset = trackedImage.transform.position - (xrOrigin.rotation * anchor.anchorTransform.position);

            // 4. Set the XR Origin's position.
            xrOrigin.position = positionOffset;

            isLocalized = true;
            Debug.Log($"Localized user to anchor: {anchor.imageName}");
        }
    }
}

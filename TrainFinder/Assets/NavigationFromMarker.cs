using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class NavigationFromMarker : MonoBehaviour
{
    [Header("AR")]
    public ARTrackedImageManager trackedImageManager;
    [Tooltip("Name of the image inside your ReferenceImageLibrary")]
    public string markerName = "SecurityCamera";

    [Header("Targets (Dots)")]
    public Transform platform1;   // Dot B
    public Transform platform2;   // Dot C

    [Header("Lines")]
    public LineRenderer lineToB;
    public LineRenderer lineToC;
    public float lineWidth = 0.03f;

    [SerializeField]
    private GameObject trainstation;

    // runtime "Dot A" (pose of the tracked image)
    Transform dotA;

    void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;

        SetupLine(lineToB);
        SetupLine(lineToC);
        EnableLines(false);
    }

    void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void SetupLine(LineRenderer lr)
    {
        if (!lr) return;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.startWidth = lineWidth;
        lr.endWidth   = lineWidth;
    }

    void EnableLines(bool on)
    {
        if (lineToB) lineToB.enabled = on;
        if (lineToC) lineToC.enabled = on;
    }

    void Update()
    {
        if (!dotA)
        {
            EnableLines(false);
            return;
        }

        // We have the marker; draw to B/C if assigned
        bool any = false;
        any |= UpdateLine(lineToB, platform1);
        any |= UpdateLine(lineToC, platform2);
        EnableLines(any);
    }

    bool UpdateLine(LineRenderer lr, Transform target)
    {
        if (!lr || !target || !dotA) return false;
        lr.SetPosition(0, dotA.position);
        lr.SetPosition(1, target.position);
        return true;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        // New images
        foreach (var img in args.added) TryUse(img);

        // Updated (tracking state / pose changes)
        // foreach (var img in args.updated) TryUse(img);

        // Lost images
        foreach (var img in args.removed)
        {
            if (img.referenceImage.name == markerName)
            {
                dotA = null;
                EnableLines(false);
            }
        }
    }

    void TryUse(ARTrackedImage img)
    {
        if (img.referenceImage.name != markerName) return;

        // Only use when the marker is actually tracked
        if (img.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
        {
            // Use the image transform itself as Dot A
            dotA = img.transform;

            var trainStationCalibrationPosition = transform.position + new Vector3(0f, 0f, 0f);
            var trainStationCalibrationRotation = Quaternion.identity;
            
            //instateshaph
            Instantiate(trainstation,trainStationCalibrationPosition,trainStationCalibrationRotation);
        }
    }
}

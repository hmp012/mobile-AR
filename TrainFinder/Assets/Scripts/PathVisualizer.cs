using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField] public Material lineMaterial;
    [SerializeField] public GameObject navTargetObject;
    [SerializeField] public XROrigin xrOrigin;
    [SerializeField] ARTrackedImageManager mImageManager;
    private bool _isLocalized = false;
    private NavMeshPath _path;
    private LineRenderer _lineRenderer;
    private bool _lineToggle = false;

    void Start()
    {
        _path = new NavMeshPath();
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.material = lineMaterial;
        _lineRenderer.startWidth = 0;
        _lineRenderer.endWidth = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Update()
    {
        // Check if AR session is tracking
        if (ARSession.state == ARSessionState.SessionTracking)
        {
            Debug.Log($"Camera position on {xrOrigin.Camera.transform.position}");
            // Only localize once when tracking starts
            if (!_isLocalized)
            {
                xrOrigin.MoveCameraToWorldLocation(new Vector3(66, 0, 41));
                Debug.Log($"Xr position SET on {xrOrigin.Camera.transform.position}");
                _isLocalized = true;
            }
            
            // Now draw the path (only when localized and tracking)
            if (navTargetObject)
            {
                NavMesh.CalculatePath(xrOrigin.Camera.transform.position, navTargetObject.transform.position,
                    NavMesh.AllAreas, _path);
                Debug.Log($"Xr position on {xrOrigin.Origin.transform.position}");
                Debug.Log($"Camera position on {xrOrigin.Camera.transform.position}");
                if (_path.status == NavMeshPathStatus.PathComplete)
                {
                    // Set the number of vertices
                    _lineRenderer.positionCount = _path.corners.Length;
                    Debug.Log($"Path with {_lineRenderer.positionCount} corners");
                    _lineRenderer.SetPositions(_path.corners);
                    _lineRenderer.startWidth = 0.2f;
                    _lineRenderer.endWidth = 0.2f;
                    Debug.Log($"Path start on {_path.corners[0]}");
                }
            }
            else
            {
                Debug.LogWarning("No Target Selected");
            }
        }
        else
        {
            Debug.LogWarning($"AR Session not tracking. Current state: {ARSession.state}");
        }
    }
}
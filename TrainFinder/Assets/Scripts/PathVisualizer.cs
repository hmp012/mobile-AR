using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using Unity.AI.Navigation;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField] public Material lineMaterial;
    [SerializeField] public XROrigin xrOrigin;
    [SerializeField] ARTrackedImageManager mImageManager;

    private bool _isLocalized = false;
    private NavMeshPath _path;
    private LineRenderer _lineRenderer;
    private bool _lineToggle = false;
    private GameObject _navigationBase;
    private List<ImageAnchor> _trackedImagePrefabs;
    private List<NavigationTarget> _navigationTargets = new();
    private NavMeshSurface _navMeshSurface;
    private List<Vector3> _pathCorners = new();
    private Vector3 _lastCalculatedPosition;
    private float _recalculationThreshold = 0.5f; // Only recalculate if moved 0.5 units
    private float _timeSinceLastCalculation = 0f;
    private float _recalculationInterval = 0.1f; // Max 10 times per second


    void Start()
    {
        _path = new NavMeshPath();
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.material = lineMaterial;
        _lineRenderer.startWidth = 0;
        _lineRenderer.endWidth = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        _navigationTargets = gameObject.scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<NavigationTarget>())
            .ToList();
        _trackedImagePrefabs = gameObject.scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<ImageAnchor>())
            .ToList();
    }

    void Update()
    {
        if (_navigationTargets.Count > 0 && _isLocalized && ARSession.state == ARSessionState.SessionTracking)
        {
            _timeSinceLastCalculation += Time.deltaTime;
        
            // Only recalculate if enough time has passed AND user has moved significantly
            float distanceMoved = Vector3.Distance(xrOrigin.Camera.transform.position, _lastCalculatedPosition);
        
            if (_timeSinceLastCalculation >= _recalculationInterval && distanceMoved >= _recalculationThreshold)
            {
                Vector3 targetPosition = _path.corners.Length <= 2 
                    ? _navigationTargets.ElementAt(0).transform.position 
                    : _pathCorners.ElementAtOrDefault(3);
            
                NavMesh.CalculatePath(xrOrigin.Camera.transform.position, targetPosition, NavMesh.AllAreas, _path);
            
                _pathCorners = _path.corners.ToList();
                _lineRenderer.positionCount = _path.corners.Length;
                _lineRenderer.SetPositions(_path.corners);
            
                _lastCalculatedPosition = xrOrigin.Camera.transform.position;
                _timeSinceLastCalculation = 0f;
            }
        }
        else
        {
            Debug.LogWarning($"navigationTargets count: {_navigationTargets.Count}, isLocalized: {_isLocalized}, ARSession state: {ARSession.state}");
        }
    }


    private void OnEnable() => mImageManager.trackablesChanged.AddListener(OnChanged);

    private void OnDisable() => mImageManager.trackablesChanged.RemoveListener(OnChanged);

    private void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var newImage in eventArgs.added)
        {
            LocalizeUser();
        }

        foreach (var updatedImage in eventArgs.updated)
        {
            LocalizeUser();
        }
    }

    private void LocalizeUser()
    {
        xrOrigin.MoveCameraToWorldLocation(new Vector3(65, 0, 40));
        xrOrigin.Camera.transform.eulerAngles = new Vector3(0, 62, 0);
        _isLocalized = true;
        DisableImageTracking();
        NavMesh.CalculatePath(xrOrigin.Camera.transform.position,
            _navigationTargets.ElementAt(0).transform.position,
            NavMesh.AllAreas, _path);
        _pathCorners = _path.corners.ToList();
        _lineRenderer.positionCount = _path.corners.Length > 2 ? 3 : _path.corners.Length;
        _lineRenderer.SetPositions(_path.corners[.._lineRenderer.positionCount]);
        _lineRenderer.startWidth = 0.2f;
        _lineRenderer.endWidth = 0.2f;
    }


    private void DisableImageTracking()
    {
        if (mImageManager != null)
        {
            mImageManager.enabled = false;
            Debug.LogWarning("Image tracking disabled");
            Debug.Log($"Image tracking disabled with XrOrigin at: {xrOrigin.Camera.transform.position}");
        }
    }
}
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GraphRenderDistanceController : MonoBehaviour
{
    [SerializeField] private float maximumVisibleDistance = 140f;
    [SerializeField] private float distanceHysteresis = 3f;

    private Renderer[] _renderers;
    private LODGroup[] _lodGroups;
    private Camera _viewerCamera;
    private bool _isVisible = true;

    private void Start()
    {
        CacheRenderSettings();
        DisableLocalDistanceCulling();
        _viewerCamera = Camera.main;
        if (_viewerCamera != null)
            UpdateVisibility(true);
    }

    private void LateUpdate()
    {
        if (_viewerCamera == null || !_viewerCamera.isActiveAndEnabled)
            _viewerCamera = FindActiveViewerCamera();

        if (_viewerCamera == null)
            return;

        UpdateVisibility(false);
    }

    private void CacheRenderSettings()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _lodGroups = GetComponentsInChildren<LODGroup>(true);
    }

    private void DisableLocalDistanceCulling()
    {
        foreach (var lodGroup in _lodGroups)
        {
            if (lodGroup != null)
                lodGroup.enabled = false;
        }

        foreach (var renderer in _renderers)
        {
            if (renderer != null)
            {
                renderer.allowOcclusionWhenDynamic = false;
                renderer.forceRenderingOff = false;
            }
        }
    }

    private Camera FindActiveViewerCamera()
    {
        var cameras = Camera.allCameras;
        foreach (var camera in cameras)
        {
            if (camera != null && camera.isActiveAndEnabled && camera.stereoTargetEye != StereoTargetEyeMask.None)
                return camera;
        }

        return null;
    }

    private void UpdateVisibility(bool force)
    {
        var distance = Vector3.Distance(transform.position, _viewerCamera.transform.position);
        var threshold = _isVisible
            ? maximumVisibleDistance + distanceHysteresis
            : maximumVisibleDistance - distanceHysteresis;
        var shouldBeVisible = distance <= Mathf.Max(0f, threshold);

        if (!force && shouldBeVisible == _isVisible)
            return;

        _isVisible = shouldBeVisible;
        foreach (var renderer in _renderers)
        {
            if (renderer != null)
                renderer.forceRenderingOff = !_isVisible;
        }
    }
}

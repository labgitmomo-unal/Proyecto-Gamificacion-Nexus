using UnityEngine;

[DisallowMultipleComponent]
public sealed class GraphRenderDistanceController : MonoBehaviour
{
    [SerializeField] private float maximumVisibleDistance = 60f;
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
            _viewerCamera = Camera.main;

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
        var mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.isActiveAndEnabled && !mainCamera.orthographic && mainCamera.targetTexture == null)
            return mainCamera;

        var cameras = Camera.allCameras;
        Camera fallbackCamera = null;
        foreach (var camera in cameras)
        {
            if (camera == null || !camera.isActiveAndEnabled || camera.stereoTargetEye == StereoTargetEyeMask.None)
                continue;

            fallbackCamera ??= camera;
            if (camera.orthographic || camera.targetTexture != null)
                continue;
            return camera;
        }

        return fallbackCamera;
    }

    private void UpdateVisibility(bool force)
    {
        var distanceSq = (transform.position - _viewerCamera.transform.position).sqrMagnitude;
        var threshold = _isVisible
            ? maximumVisibleDistance + distanceHysteresis
            : maximumVisibleDistance - distanceHysteresis;
        var shouldBeVisible = distanceSq <= Mathf.Max(0f, threshold * threshold);

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

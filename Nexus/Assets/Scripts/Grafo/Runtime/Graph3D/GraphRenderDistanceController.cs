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
        {
            _viewerCamera = Camera.main;
            if (_viewerCamera == null)
                return;
        }

        UpdateVisibility(false);
    }

    private void CacheRenderSettings()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _lodGroups = GetComponentsInChildren<LODGroup>(true);
    }

    private void DisableLocalDistanceCulling()
    {
        foreach (var renderer in _renderers)
        {
            if (renderer != null)
            {
                renderer.allowOcclusionWhenDynamic = true;
                renderer.forceRenderingOff = false;
            }
        }
    }

    private Camera FindActiveViewerCamera()
    {
        // Return cached camera if still valid
        if (_viewerCamera != null && _viewerCamera.isActiveAndEnabled && !_viewerCamera.orthographic && _viewerCamera.targetTexture == null)
            return _viewerCamera;

        // Only search main camera to avoid Camera.allCameras array allocation
        var mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.isActiveAndEnabled && !mainCamera.orthographic && mainCamera.targetTexture == null)
        {
            _viewerCamera = mainCamera;
            return _viewerCamera;
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

using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class MapMarker : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Camera mapCamera;
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private Canvas mapCanvas;
    [SerializeField] private float edgeClampRadius = 280f;

    private RectTransform _markerRect;
    private bool _trackingEnabled;
    private bool _warnedMissingReferences;

    private void Awake()
    {
        _markerRect = GetComponent<RectTransform>();
        if (mapRect == null && transform.parent != null)
            mapRect = transform.parent.GetComponent<RectTransform>();
        if (mapCanvas == null)
            mapCanvas = GetComponentInParent<Canvas>();
        _trackingEnabled = ValidateReferences();
    }

    private void LateUpdate()
    {
        if (!_trackingEnabled || !ValidateReferences())
        {
            _trackingEnabled = false;
            return;
        }

        var viewportPosition = mapCamera.WorldToViewportPoint(targetTransform.position);
        var bounds = mapRect.rect;
        var position = new Vector2(
            Mathf.Lerp(bounds.xMin, bounds.xMax, viewportPosition.x),
            Mathf.Lerp(bounds.yMin, bounds.yMax, viewportPosition.y));
        var halfMarkerSize = _markerRect.rect.size * 0.5f;
        var min = bounds.min + halfMarkerSize;
        var max = bounds.max - halfMarkerSize;
        position.x = Mathf.Clamp(position.x, min.x, max.x);
        position.y = Mathf.Clamp(position.y, min.y, max.y);

        if (edgeClampRadius > 0f)
        {
            var offset = position - bounds.center;
            var distance = offset.magnitude;
            if (distance > edgeClampRadius && distance > Mathf.Epsilon)
                position = bounds.center + offset * (edgeClampRadius / distance);
            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.y = Mathf.Clamp(position.y, min.y, max.y);
        }

        _markerRect.anchoredPosition = position;
    }

    private bool ValidateReferences()
    {
        if (targetTransform != null && mapCamera != null && _markerRect != null && mapRect != null && mapCanvas != null)
            return true;
        if (!_warnedMissingReferences)
        {
            Debug.LogWarning($"[MapMarker] {name}: faltan targetTransform, mapCamera, RectTransform del mapa, Canvas o una cámara activa. El seguimiento se desactivará.", this);
            _warnedMissingReferences = true;
        }
        return false;
    }
}

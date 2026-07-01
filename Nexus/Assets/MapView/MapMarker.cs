using UnityEngine;

/// <summary>
/// Mapea la posición de un objeto en el mundo real a un RectTransform en el mapa.
/// </summary>
public class MapMarker : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Camera mapCamera;
    private RectTransform _markerRect;
    private RectTransform _mapRect;

    private void Awake()
    {
        _markerRect = GetComponent<RectTransform>();
        _mapRect = transform.parent.GetComponent<RectTransform>();

        if (targetTransform == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) targetTransform = player.transform;
        }

        if (mapCamera == null)
        {
            var camObj = GameObject.Find("MapViewCamera");
            if (camObj != null) mapCamera = camObj.GetComponent<Camera>();
        }
    }

    private void Update()
    {
        if (targetTransform == null || mapCamera == null) return;

        // Convertir posición de mundo a viewport de la cámara ortográfica del mapa
        Vector3 viewportPos = mapCamera.WorldToViewportPoint(targetTransform.position);

        // Si está fuera de la vista de la cámara, opcionalmente ocultar (aquí lo dejamos visible)
        // Mapear viewport (0-1) a las dimensiones del RectTransform del mapa
        float x = (viewportPos.x - 0.5f) * _mapRect.sizeDelta.x;
        float y = (viewportPos.y - 0.5f) * _mapRect.sizeDelta.y;

        _markerRect.anchoredPosition = new Vector2(x, y);
    }
}

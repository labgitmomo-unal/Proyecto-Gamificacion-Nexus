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

    /// <summary>
    /// Radio del marcador en píxeles del canvas. Si el jugador sale del área visible,
    /// el marcador se mantiene en el borde más cercano dentro de este radio.
    /// </summary>
    [SerializeField] private float edgeClampRadius = 280f;

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
        if (targetTransform == null || mapCamera == null || _mapRect == null) return;

        // Convertir posición de mundo a viewport de la cámara ortográfica del mapa
        Vector3 viewportPos = mapCamera.WorldToViewportPoint(targetTransform.position);

        // Mapear viewport (0-1) a las dimensiones del RectTransform del mapa
        float x = (viewportPos.x - 0.5f) * _mapRect.sizeDelta.x;
        float y = (viewportPos.y - 0.5f) * _mapRect.sizeDelta.y;

        // Clamp al borde si el jugador está fuera del área visible del mapa
        Vector2 pos = new Vector2(x, y);
        float magnitude = pos.magnitude;
        if (magnitude > edgeClampRadius && edgeClampRadius > 0f)
        {
            pos = pos.normalized * edgeClampRadius;
        }

        _markerRect.anchoredPosition = pos;
    }
}

using UnityEngine;

/// <summary>
/// Mantiene la rotación de la Rosa de los Vientos alineada con el Norte global
/// basándose en la rotación de la cámara del mapa o el jugador.
/// </summary>
public class CompassUI : MonoBehaviour
{
    [SerializeField] private Transform referenceTransform;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (referenceTransform == null)
        {
            // Buscar el XR Origin (jugador VR) primero
            var xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin != null)
            {
                referenceTransform = xrOrigin.transform;
            }
            else
            {
                // Fallback: cámara del mapa
                var cam = GameObject.Find("MapViewCamera");
                if (cam != null) referenceTransform = cam.transform;
            }
        }
    }

    private void Update()
    {
        if (referenceTransform == null) return;

        // La rosa de los vientos rota en sentido opuesto a la rotación Y del jugador
        // para indicar dónde está el Norte relativo a su orientación.
        float playerYaw = referenceTransform.eulerAngles.y;
        _rectTransform.localRotation = Quaternion.Euler(0, 0, -playerYaw);
    }
}

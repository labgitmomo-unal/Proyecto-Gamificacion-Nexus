using UnityEngine;
using UnityEngine.UI;

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
            // Intentar encontrar la cámara del mapa si no se asigna
            var cam = GameObject.Find("MapViewCamera");
            if (cam != null) referenceTransform = cam.transform;
        }
    }

    private void Update()
    {
        if (referenceTransform == null) return;

        // La rosa de los vientos suele apuntar al Norte (0,0,1).
        // Si la cámara rota, la rosa debe rotar en sentido opuesto en el eje Z de la UI.
        float rotation = referenceTransform.eulerAngles.y;
        _rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
    }
}

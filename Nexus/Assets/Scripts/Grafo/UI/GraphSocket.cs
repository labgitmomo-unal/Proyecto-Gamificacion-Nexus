using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Punto de conexión circular (luz) en un nodo.
/// </summary>
public class GraphSocket : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Configuración")]
    public Image lightImage;
    public Color inactiveColor = Color.gray;
    public Color activeColor = Color.cyan;
    public GameObject edgePrefab; // Prefab con LineRenderer y GraphEdge

    private GraphEdge _currentEdge;
    private bool _isConnected = false;
    private Canvas _canvas;

    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData) { }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Crear un nuevo cable al empezar a arrastrar desde la luz
        if (edgePrefab != null)
        {
            GameObject go = Instantiate(edgePrefab, transform.parent);
            _currentEdge = go.GetComponent<GraphEdge>();
            _currentEdge.Initialize(transform, null, null);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_currentEdge == null) return;

        // Mover el extremo del cable con el puntero/rayo
        var parentRect = transform.parent as RectTransform;
        if (GraphUiUtility.TryGetPointerWorldPoint(parentRect, _canvas, eventData, out Vector3 worldPoint))
        {
            _currentEdge.UpdateEndPoint(worldPoint);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_currentEdge == null) return;

        // Verificar si soltamos sobre otro socket
        GraphSocket targetSocket = null;
        if (eventData.pointerEnter != null)
        {
            targetSocket = eventData.pointerEnter.GetComponent<GraphSocket>();
        }

        if (targetSocket != null && targetSocket != this)
        {
            _currentEdge.Initialize(transform, targetSocket.transform, null);
            _isConnected = true;
            targetSocket.SetConnected(true);
            UpdateVisuals();
        }
        else
        {
            Destroy(_currentEdge.gameObject);
        }
        
        _currentEdge = null;
    }

    public void SetConnected(bool connected)
    {
        _isConnected = connected;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (lightImage != null)
            lightImage.color = _isConnected ? activeColor : inactiveColor;
    }
}

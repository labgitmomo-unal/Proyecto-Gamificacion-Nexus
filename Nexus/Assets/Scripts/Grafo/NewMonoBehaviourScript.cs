using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gestiona el grafo en un Canvas World Space para VR.
/// En el contenedor raíz: configura los nodos hijos y el raycaster XR.
/// En cada nodo: permite arrastrar y crea puntos de conexión (sockets).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class NewMonoBehaviourScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Arrastre")]
    [SerializeField] private bool bringToFrontOnDrag = true;

    [Header("Configuración de Luces")]
    [SerializeField] private GameObject socketPrefab;
    [SerializeField] private int socketsPerNode = 1;

    private RectTransform _rectTransform;
    private Image _image;
    private Canvas _canvas;
    private Vector3 _worldOffset;
    private int _originalSiblingIndex;
    private bool _isRootContainer;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
        _canvas = GetComponentInParent<Canvas>();

        // Si tiene Image, es un nodo arrastrable
        if (_image != null)
        {
            _image.raycastTarget = true;

            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            CreateSockets();
            return;
        }

        // Si no tiene Image, es el contenedor raíz: configurar hijos
        _isRootContainer = true;
        ConfigureChildNodes();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying && _isRootContainer)
            ConfigureChildNodes();
    }

    private void Start()
    {
        if (_isRootContainer)
            EnsureTrackedDevicesCanRaycast();
    }

    /// <summary>
    /// Crea los sockets (luces circulares) en los bordes del nodo.
    /// </summary>
    private void CreateSockets()
    {
        if (socketPrefab == null) return;

        for (int i = 0; i < socketsPerNode; i++)
        {
            GameObject socketGo = Instantiate(socketPrefab, transform);
            socketGo.name = "Socket_" + i;
            RectTransform rt = socketGo.GetComponent<RectTransform>();
            if (rt != null)
            {
                float angle = (360f / Mathf.Max(1, socketsPerNode)) * i;
                float radius = 50f;
                rt.anchoredPosition = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            }
        }
    }

    /// <summary>
    /// Recorre la jerarquía y configura los nodos hijos.
    /// </summary>
    private void ConfigureChildNodes()
    {
        InstallInHierarchy(transform);
    }

    private void InstallInHierarchy(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child == null) continue;

            // Configurar objetos que empiezan con "Base" o que tienen Image
            var childImage = child.GetComponent<Image>();
            if (childImage != null)
            {
                childImage.raycastTarget = true;
                if (child.GetComponent<NewMonoBehaviourScript>() == null)
                    child.gameObject.AddComponent<NewMonoBehaviourScript>();
            }

            if (child.childCount > 0)
                InstallInHierarchy(child);
        }
    }

    /// <summary>
    /// Asegura que el Canvas tenga un TrackedDeviceGraphicRaycaster para VR.
    /// </summary>
    private void EnsureTrackedDevicesCanRaycast()
    {
        if (_canvas == null) return;

        if (_canvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() == null)
            _canvas.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _originalSiblingIndex = transform.GetSiblingIndex();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (bringToFrontOnDrag)
            transform.SetAsLastSibling();
        CachePointerOffset(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_rectTransform == null || _rectTransform.parent == null) return;

        if (TryGetPointerWorldPoint(eventData, out Vector3 worldPoint))
            _rectTransform.position = worldPoint + _worldOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (bringToFrontOnDrag)
            transform.SetSiblingIndex(_originalSiblingIndex);
    }

    private void CachePointerOffset(PointerEventData eventData)
    {
        if (TryGetPointerWorldPoint(eventData, out Vector3 worldPoint))
            _worldOffset = _rectTransform.position - worldPoint;
        else
            _worldOffset = Vector3.zero;
    }

    private bool TryGetPointerWorldPoint(PointerEventData eventData, out Vector3 worldPoint)
    {
        worldPoint = default;
        var parentRect = _rectTransform.parent as RectTransform;
        if (parentRect == null) return false;

        Camera eventCamera = eventData.pressEventCamera;
        if (eventCamera == null && _canvas != null)
            eventCamera = _canvas.worldCamera;

        return RectTransformUtility.ScreenPointToWorldPointInRectangle(
            parentRect, eventData.position, eventCamera, out worldPoint);
    }
}

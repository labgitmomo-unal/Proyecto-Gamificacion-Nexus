using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Permite arrastrar un nodo del grafo en un Canvas World Space con rayos XR o mouse.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class NewMonoBehaviourScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Arrastre")]
    [SerializeField] private bool bringToFrontOnDrag = true;

    private RectTransform _rectTransform;
    private Image _image;
    private Canvas _canvas;
    private Vector3 _worldOffset;
    private int _originalSiblingIndex;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
        _canvas = GetComponentInParent<Canvas>();

        if (_image != null)
        {
            _image.raycastTarget = true;

            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = gameObject.AddComponent<LayoutElement>();

            layoutElement.ignoreLayout = true;
            return;
        }

        ConfigureChildNodes();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            ConfigureChildNodes();
    }

    private void Start()
    {
        if (_image == null)
            EnsureTrackedDevicesCanRaycast();
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
        if (_rectTransform == null || _rectTransform.parent == null)
            return;

        if (TryGetPointerWorldPoint(eventData, out Vector3 worldPoint))
        {
            _rectTransform.position = worldPoint + _worldOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (bringToFrontOnDrag)
            transform.SetSiblingIndex(_originalSiblingIndex);
    }

    private void ConfigureChildNodes()
    {
        // Si este componente está en el contenedor del grafo, lo usamos como instalador.
        InstallInHierarchy(transform);
    }

    private void InstallInHierarchy(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child == null)
                continue;

            if (child.name.StartsWith("Base"))
            {
                var childImage = child.GetComponent<Image>();
                if (childImage != null)
                    childImage.raycastTarget = true;

                if (child.GetComponent<NewMonoBehaviourScript>() == null)
                    child.gameObject.AddComponent<NewMonoBehaviourScript>();
            }

            if (child.childCount > 0)
                InstallInHierarchy(child);
        }
    }

    private void EnsureTrackedDevicesCanRaycast()
    {
        if (_canvas == null)
            return;

        if (_canvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() == null)
            _canvas.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
    }

    private void CachePointerOffset(PointerEventData eventData)
    {
        if (TryGetPointerWorldPoint(eventData, out Vector3 worldPoint))
        {
            _worldOffset = _rectTransform.position - worldPoint;
        }
        else
        {
            _worldOffset = Vector3.zero;
        }
    }

    private bool TryGetPointerWorldPoint(PointerEventData eventData, out Vector3 worldPoint)
    {
        worldPoint = default;

        var parentRect = _rectTransform.parent as RectTransform;
        if (parentRect == null)
            return false;

        Camera eventCamera = eventData.pressEventCamera;
        if (eventCamera == null && _canvas != null)
            eventCamera = _canvas.worldCamera;

        return RectTransformUtility.ScreenPointToWorldPointInRectangle(
            parentRect,
            eventData.position,
            eventCamera,
            out worldPoint);
    }
}

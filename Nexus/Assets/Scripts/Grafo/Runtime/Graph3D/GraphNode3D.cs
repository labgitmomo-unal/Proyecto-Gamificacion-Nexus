using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
public sealed class GraphNode3D : MonoBehaviour
{
    private const int SocketCount = 4;
    private const float DefaultGrabColliderSize = 1f;

    [SerializeField] private GraphSocket3D socketPrefab;
    [SerializeField] private float socketScale = 0.18f;
    [SerializeField] private Color socketColor = Color.cyan;
    [SerializeField] private float socketLightIntensity = 3f;
    [SerializeField] private float socketLightRange = 2.5f;
    [SerializeField] private string windowNameToken = "Window";
    [SerializeField] private Collider placementSurface;
    [SerializeField] private float placementHorizontalPadding = 0.1f;

    private readonly List<GraphSocket3D> _sockets = new();
    private XRGrabInteractable _grabInteractable;
    private Rigidbody _rigidbody;
    private bool _initialized;

    public IReadOnlyList<GraphSocket3D> Sockets => _sockets;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        RefreshGrabRegistration();
        SetSocketsConnectionAvailable(IsAbovePlacementSurface());
    }

    /// <summary>Configures the node interaction and creates its connection sockets once.</summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        ConfigureNodeInteraction();
        CreateSocketsAtWindows();
    }

    /// <summary>Sets the map surface used to validate placement and activate connection sockets.</summary>
    public void ConfigurePlacementSurface(Collider surface)
    {
        placementSurface = surface;
        if (isActiveAndEnabled && _grabInteractable != null && !_grabInteractable.isSelected)
            SetSocketsConnectionAvailable(IsAbovePlacementSurface());
    }

    private void ConfigureNodeInteraction()
    {
        var body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _rigidbody = body;

        var grabCollider = GetComponent<BoxCollider>();
        if (grabCollider == null)
        {
            grabCollider = gameObject.AddComponent<BoxCollider>();
            grabCollider.center = Vector3.zero;
            grabCollider.size = Vector3.one * DefaultGrabColliderSize;
        }

        _grabInteractable = GetComponent<XRGrabInteractable>();
        if (_grabInteractable == null)
            _grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
        _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        _grabInteractable.useDynamicAttach = true;
        _grabInteractable.snapToColliderVolume = false;
        _grabInteractable.distanceCalculationMode = XRBaseInteractable.DistanceCalculationMode.ColliderPosition;
        _grabInteractable.trackPosition = true;
        _grabInteractable.trackRotation = true;
        _grabInteractable.trackScale = false;
        _grabInteractable.throwOnDetach = false;
        _grabInteractable.forceGravityOnDetach = false;
        _grabInteractable.retainTransformParent = false;
        _grabInteractable.selectMode = InteractableSelectMode.Single;
        _grabInteractable.selectEntered.AddListener(HandleNodeGrabbed);
        _grabInteractable.selectExited.AddListener(HandleNodePlaced);

        RegisterGrabColliders(grabCollider);
        DisableNestedNodeInteractables();
    }

    private void OnDestroy()
    {
        if (_grabInteractable == null)
            return;

        _grabInteractable.selectEntered.RemoveListener(HandleNodeGrabbed);
        _grabInteractable.selectExited.RemoveListener(HandleNodePlaced);
    }

    private void RegisterGrabColliders(BoxCollider grabCollider)
    {
        if (_grabInteractable == null || grabCollider == null)
            return;

        _grabInteractable.colliders.Clear();
        _grabInteractable.colliders.Add(grabCollider);

        foreach (var collider in GetComponentsInChildren<Collider>(true))
        {
            if (collider == grabCollider || collider.GetComponentInParent<GraphSocket3D>() != null)
                continue;
            _grabInteractable.colliders.Add(collider);
        }
    }

    private void RefreshGrabRegistration()
    {
        if (_grabInteractable == null)
            return;

        var wasEnabled = _grabInteractable.enabled;
        _grabInteractable.enabled = false;
        RegisterGrabColliders(GetComponent<BoxCollider>());
        _grabInteractable.enabled = wasEnabled;
    }

    private void DisableNestedNodeInteractables()
    {
        foreach (var interactable in GetComponentsInChildren<XRGrabInteractable>(true))
        {
            if (interactable == _grabInteractable || interactable.GetComponentInParent<GraphSocket3D>() != null)
                continue;
            interactable.enabled = false;
        }
    }

    private void HandleNodeGrabbed(SelectEnterEventArgs args)
    {
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
        SetSocketsConnectionAvailable(false);
    }

    private void HandleNodePlaced(SelectExitEventArgs args)
    {
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
        }
        var isOnMap = IsAbovePlacementSurface();
        SetSocketsConnectionAvailable(isOnMap);
        Debug.Log($"[{nameof(GraphNode3D)}] {name}: soltado {(isOnMap ? "sobre" : "fuera de")} la plataforma del mapa.", this);
    }

    private bool IsAbovePlacementSurface()
    {
        if (placementSurface == null || !placementSurface.enabled || !placementSurface.gameObject.activeInHierarchy)
            return false;

        var surfaceBounds = placementSurface.bounds;
        var nodePosition = transform.position;
        return nodePosition.x >= surfaceBounds.min.x - placementHorizontalPadding &&
               nodePosition.x <= surfaceBounds.max.x + placementHorizontalPadding &&
               nodePosition.z >= surfaceBounds.min.z - placementHorizontalPadding &&
               nodePosition.z <= surfaceBounds.max.z + placementHorizontalPadding;
    }

    private void SetSocketsConnectionAvailable(bool available)
    {
        foreach (var socket in _sockets)
        {
            if (socket != null)
                socket.SetConnectionAvailable(available);
        }
    }

    private void CreateSocketsAtWindows()
    {
        if (_sockets.Count > 0)
            return;
        if (socketPrefab == null)
        {
            Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: socketPrefab no está asignado; no se crearán sockets.", this);
            return;
        }

        var anchors = FindWindowAnchors();
        for (var i = 0; i < Mathf.Min(SocketCount, anchors.Count); i++)
        {
            var anchor = anchors[i];
            if (anchor == null)
            {
                Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: la ventana {i} no tiene un Transform válido.", this);
                continue;
            }

            var socket = Instantiate(socketPrefab, anchor, false);
            socket.name = $"GraphSocket_Light_{i + 1}";
            socket.transform.localPosition = Vector3.zero;
            socket.transform.localRotation = Quaternion.identity;
            socket.transform.localScale = Vector3.one * socketScale;
            socket.Configure(socketColor, this, socketLightIntensity, socketLightRange);
            socket.SetConnectionAvailable(false);
            _sockets.Add(socket);
        }

        RegisterGrabColliders(GetComponent<BoxCollider>());

        if (_sockets.Count != SocketCount)
            Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: se encontraron {_sockets.Count} ventanas; se esperaban {SocketCount}.", this);
    }

    private List<Transform> FindWindowAnchors()
    {
        var anchors = new List<Transform>();
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child != transform && child.name.IndexOf(windowNameToken, StringComparison.OrdinalIgnoreCase) >= 0)
                anchors.Add(child);
        }
        anchors.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        return anchors;
    }
}

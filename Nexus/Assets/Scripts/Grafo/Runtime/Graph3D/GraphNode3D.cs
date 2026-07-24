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
    }

    /// <summary>Configures node interaction and registers the sockets already stored in this prefab.</summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        ConfigureNodeInteraction();
        RegisterPersistentSockets();
    }

    /// <summary>Sets the map surface used to validate node placement without changing socket availability.</summary>
    public void ConfigurePlacementSurface(Collider surface)
    {
        placementSurface = surface;
        placementHorizontalPadding = Mathf.Max(0f, placementHorizontalPadding);
    }

    /// <summary>Registers a persistent socket and associates it with its original window anchor.</summary>
    public void RegisterSocket(GraphSocket3D socket, Transform windowAnchor)
    {
        if (socket == null || windowAnchor == null)
        {
            Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: no se puede registrar un socket o ventana nulos.", this);
            return;
        }

        if (_sockets.Contains(socket))
        {
            Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: el socket {socket.name} está duplicado.", this);
            return;
        }

        socket.Configure(socketColor, this, socketLightIntensity, socketLightRange);
        socket.SetAttachedWindow(windowAnchor);
        socket.SetConnectionAvailable(true);
        _sockets.Add(socket);
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

        _grabInteractable.selectEntered.RemoveListener(HandleNodeGrabbed);
        _grabInteractable.selectExited.RemoveListener(HandleNodePlaced);
        _grabInteractable.colliders.Clear();
        _grabInteractable.colliders.Add(grabCollider);

        foreach (var collider in GetComponentsInChildren<Collider>(true))
        {
            if (collider == grabCollider || collider.GetComponentInParent<GraphSocket3D>() != null)
                continue;
            _grabInteractable.colliders.Add(collider);
        }

        _grabInteractable.selectEntered.AddListener(HandleNodeGrabbed);
        _grabInteractable.selectExited.AddListener(HandleNodePlaced);
    }

    private void RefreshGrabRegistration()
    {
        RegisterGrabColliders(GetComponent<BoxCollider>());
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
        if (_rigidbody == null)
            return;

        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    private void HandleNodePlaced(SelectExitEventArgs args)
    {
        if (_rigidbody == null)
            return;

        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        Debug.Log($"[{nameof(GraphNode3D)}] {name}: nodo soltado; los sockets permanecen activos.", this);
    }

    private void RegisterPersistentSockets()
    {
        var anchors = FindWindowAnchors();
        var persistentSockets = new List<GraphSocket3D>(GetComponentsInChildren<GraphSocket3D>(true));
        persistentSockets.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

        if (anchors.Count != SocketCount)
            Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: se encontraron {anchors.Count} ventanas; se esperaban {SocketCount}.", this);
        if (persistentSockets.Count != SocketCount)
            Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: se encontraron {persistentSockets.Count} esferas persistentes; se esperaban {SocketCount}.", this);

        var registrationCount = Mathf.Min(SocketCount, Mathf.Min(anchors.Count, persistentSockets.Count));
        for (var i = 0; i < registrationCount; i++)
            RegisterSocket(persistentSockets[i], anchors[i]);

        if (persistentSockets.Count == 0 && socketPrefab != null)
        {
            Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: no hay sockets persistentes; se ejecuta una migración temporal desde socketPrefab.", this);
            CreateLegacySocketsAtWindows(anchors);
        }
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

    private void CreateLegacySocketsAtWindows(List<Transform> anchors)
    {
        for (var i = 0; i < Mathf.Min(SocketCount, anchors.Count); i++)
        {
            var anchor = anchors[i];
            if (anchor == null)
                continue;

            var socket = Instantiate(socketPrefab, anchor, false);
            socket.name = $"GraphSocket_Light_{i + 1}";
            socket.transform.localScale = Vector3.one * socketScale;
            RegisterSocket(socket, anchor);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GraphNode3D : MonoBehaviour
{
    private const int SocketCount = 4;
    private const float DefaultGrabColliderSize = 1f;

    [SerializeField] private GraphSocket3D socketPrefab;
    [SerializeField] private float socketScale = 0.18f;
    [SerializeField] private Color socketColor01 = Color.white;
    [SerializeField] private Color socketColor02 = Color.red;
    [SerializeField] private Color socketColor03 = new(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color socketColor04 = Color.yellow;
    [SerializeField] private float socketLightIntensity = 0.75f;
    [SerializeField] private float socketLightRange = 0.75f;
    [SerializeField] private string windowNameToken = "Window";
    [SerializeField] private Collider placementSurface;
    [SerializeField] private BoxCollider nodeGrabCollider;
    [SerializeField] private float placementHorizontalPadding = 0.1f;

    private readonly List<GraphSocket3D> _sockets = new();
    private Rigidbody _rigidbody;
    private bool _initialized;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialLocalScale;


    public IReadOnlyList<GraphSocket3D> Sockets => _sockets;

    /// <summary>Gets the Rigidbody attached directly to this node root.</summary>
    public Rigidbody PhysicsBody => _rigidbody;

    /// <summary>Gets the dedicated node raycast collider attached to this node root.</summary>
    public Collider GrabCollider => nodeGrabCollider;

    private void Awake()
    {
        Initialize();
    }

    /// <summary>Configures node physics and registers the sockets already stored in this prefab.</summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
        _initialLocalScale = transform.localScale;
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
        RegisterSocket(socket, windowAnchor, GetSocketColor(_sockets.Count));
    }

    private void RegisterSocket(GraphSocket3D socket, Transform windowAnchor, Color color)
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

        socket.Configure(color, this, socketLightIntensity, socketLightRange);
        socket.SetAttachedWindow(windowAnchor);
        windowAnchor.GetComponent<GraphWindow3D>()?.RegisterAnchorSocket(socket);
        socket.SetConnectionAvailable(true);
        _sockets.Add(socket);

        IgnoreSocketCollisions(socket);

    }

    /// <summary>Prepares the node for ray movement while it is held.</summary>
    public void SetGrabbedPhysicsState()
    {
        Initialize();
        if (_rigidbody == null)
            return;

        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    /// <summary>Restores dynamic gravity-driven physics after the node is released.</summary>
    public void SetReleasedPhysicsState()
    {
        Initialize();
        if (_rigidbody == null)
            return;

        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        _rigidbody.WakeUp();
    }

    /// <summary>Restores this node root to the pose captured when it was initialized.</summary>
    public void ResetToInitialPose()
    {
        Initialize();
        if (_rigidbody == null)
            return;

        _rigidbody.isKinematic = true;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(_initialPosition, _initialRotation);
        transform.localScale = _initialLocalScale;
        _rigidbody.position = _initialPosition;
        _rigidbody.rotation = _initialRotation;
        SetReleasedPhysicsState();
    }

    internal void AddAssignedSocket(GraphSocket3D socket)
    {
        if (socket != null && !_sockets.Contains(socket))
            _sockets.Add(socket);
    }

    internal void RemoveAssignedSocket(GraphSocket3D socket)
    {
        if (socket != null)
            _sockets.Remove(socket);
    }

    internal void IgnoreSocketCollisions(GraphSocket3D socket)
    {
        if (socket == null)
            return;

        var nodeColliders = GetComponentsInChildren<Collider>(true);
        var socketColliders = socket.GetComponentsInChildren<Collider>(true);
        foreach (var nodeCollider in nodeColliders)
        {
            if (nodeCollider == null)
                continue;
            foreach (var socketCollider in socketColliders)
            {
                if (socketCollider != null && nodeCollider != socketCollider)
                    Physics.IgnoreCollision(nodeCollider, socketCollider, true);
            }
        }
    }

    private void ConfigureNodeInteraction()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
            _rigidbody = gameObject.AddComponent<Rigidbody>();

        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        if (nodeGrabCollider != null && nodeGrabCollider.gameObject != gameObject)
        {
            Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: nodeGrabCollider debe pertenecer al GameObject raíz; se usará un BoxCollider raíz.", this);
            nodeGrabCollider = null;
        }

        if (nodeGrabCollider == null)
            nodeGrabCollider = GetComponent<BoxCollider>();
        if (nodeGrabCollider != null)
            return;

        nodeGrabCollider = gameObject.AddComponent<BoxCollider>();
        nodeGrabCollider.center = Vector3.zero;
        nodeGrabCollider.size = Vector3.one * DefaultGrabColliderSize;
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
            RegisterSocket(persistentSockets[i], anchors[i], GetSocketColor(i));

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
            RegisterSocket(socket, anchor, GetSocketColor(i));
        }
    }

    private Color GetSocketColor(int socketIndex)
    {
        if (socketIndex < 0 || socketIndex >= SocketCount)
        {
            Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: índice de socket fuera de rango ({socketIndex}); se usará el último color.", this);
            socketIndex = SocketCount - 1;
        }

        return socketIndex switch
        {
            0 => socketColor01,
            1 => socketColor02,
            2 => socketColor03,
            _ => socketColor04
        };
    }
}

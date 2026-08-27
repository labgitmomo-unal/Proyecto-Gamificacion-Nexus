using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GraphSocket3D : MonoBehaviour
{
    private const float DefaultSnapRadius = 0.9f;
    private const float DefaultLineWidth = 0.045f;
    private const float AttractionDuration = 0.18f;
    private const int DefaultOverlapCapacity = 16;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] private float snapRadius = DefaultSnapRadius;
    [SerializeField] private float lineWidth = DefaultLineWidth;
    [SerializeField] private Color edgeColor = Color.cyan;
    [SerializeField] private Material edgeMaterial;
    [SerializeField] private LayerMask socketLayerMask = ~0;
    [SerializeField] private int overlapBufferCapacity = DefaultOverlapCapacity;
    [SerializeField] private Transform originalWindowAnchor;
    [SerializeField] private Transform attachedWindowAnchor;
    [SerializeField] private Rigidbody _rigidbodyReference;

    private sealed class SuspendedConnection
    {
        public GraphEdge Edge;
        public GraphSocket3D TargetSocket;
        public Transform TargetWindow;
        public Material Material;
        public float Width;
        public Color Color;
        public bool PreserveOnReset;
    }

    private readonly List<GraphEdge> _connectedEdges = new();
    private readonly List<SuspendedConnection> _suspendedConnections = new();

    private GraphNode3D _ownerNode;
    private GraphNode3D _originalOwnerNode;
    private bool _preserveNextConnection;

    private bool _isDragging;
    private bool _isAttracting;
    private GraphSocket3D _attractionTargetSocket;
    private Transform _attractionTargetWindow;
    private float _attractionElapsed;
    private bool _hasCachedOriginalPose;

    private bool _windowAttractionEnabled;
    private Vector3 _originalAnchorLocalPosition;
    private Quaternion _originalAnchorLocalRotation;
    private Vector3 _originalAnchorLocalScale;
    private Vector3 _lastValidLocalPosition;
    private Quaternion _lastValidLocalRotation;
    private Vector3 _dragTargetPosition;
    private LineRenderer _temporaryLine;
    private GraphEdge _connectedEdge;
    private Collider[] _overlapBuffer;
    private MaterialPropertyBlock _propertyBlock;
    private Renderer _renderer;
    private Light[] _lights;
    private Collider[] _interactionColliders;
    private bool _isAnchored;

    private void Awake()
    {
        _lastValidLocalPosition = transform.localPosition;
        _lastValidLocalRotation = transform.localRotation;
        _overlapBuffer = new Collider[Mathf.Max(1, overlapBufferCapacity)];
        _propertyBlock = new MaterialPropertyBlock();
        _renderer = GetComponentInChildren<Renderer>(true);
        _lights = GetComponentsInChildren<Light>(true);
        EnsureInteractionCollider();
        _interactionColliders = GetComponents<Collider>();
        ConfigurePhysicsBody();
        CacheOriginalAnchorPose();
        DisableRealtimeLightsForPerformance();
        SetAlwaysOnVisualState();
    }

    private void OnValidate()
    {
        _propertyBlock ??= new MaterialPropertyBlock();
        _renderer ??= GetComponentInChildren<Renderer>(true);
        _lights ??= GetComponentsInChildren<Light>(true);
        DisableRealtimeLightsForPerformance();
        SetAlwaysOnVisualState();
    }

    private void DisableRealtimeLightsForPerformance()
    {
        if (_lights == null)
            return;
        foreach (var light in _lights)
        {
            if (light == null)
                continue;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForceVertex;
        }
    }

    private void Update()
    {
        if (!_isAnchored)
            SanitizeTransform();
        if (_temporaryLine == null)
            return;

        var lineStart = originalWindowAnchor != null ? originalWindowAnchor.position : transform.position;
        _temporaryLine.SetPosition(0, lineStart);
        _temporaryLine.SetPosition(1, transform.position);
    }

    private void FixedUpdate()
    {
        ApplyAttractionMovement();
    }

    /// <summary>Associates this persistent socket with its owning node and visual configuration.</summary>
    public void Configure(Color color, GraphNode3D ownerNode)
    {
        Configure(color, ownerNode, -1f, -1f);
    }

    /// <summary>Associates this persistent socket with its owning node and visual configuration.</summary>
    public void Configure(Color color, GraphNode3D ownerNode, float lightIntensity, float lightRange)
    {
        edgeColor = color;
        _ownerNode = ownerNode;
        if (_originalOwnerNode == null)
            _originalOwnerNode = ownerNode;
        if (_lights == null)
            return;

        foreach (var light in _lights)
        {
            if (light == null)
                continue;
            light.enabled = false;
        }
        SetAlwaysOnVisualState();
    }

    /// <summary>Associates the socket with its stable original window anchor.</summary>
    public void SetAttachedWindow(Transform window)
    {
        if (window == null)
            return;

        if (originalWindowAnchor == null)
        {
            originalWindowAnchor = window;
            CacheOriginalAnchorPose();
        }
        attachedWindowAnchor = window;
    }

    internal Color EdgeColor => edgeColor;

    public GraphNode3D OriginalOwnerNode => _originalOwnerNode;

    /// <summary>Marks the next created graph edge as the demonstration edge.</summary>
    public void MarkNextConnectionAsExample()
    {
        _preserveNextConnection = true;
    }

    /// <summary>Restores this socket, its parent window, and its connection to the initial state.</summary>
    public void ResetToOriginalState()
    {
        RemoveAllConnections();
        _suspendedConnections.Clear();
        _isDragging = false;
        _isAttracting = false;
        _windowAttractionEnabled = false;
        _attractionTargetSocket = null;
        _attractionTargetWindow = null;
        _preserveNextConnection = false;
        RestoreToOriginalWindow();
    }


    public bool IsFreeBody => _rigidbodyReference != null
        && !_isDragging
        && !_isAttracting
        && !_rigidbodyReference.isKinematic
        && transform.parent == null;

    internal bool TryAttachToWindow(GraphWindow3D window)
    {
        if (window == null || !IsFreeBody || originalWindowAnchor == null || window.transform == null)
            return false;

        var targetSocket = window.AnchorSocket;
        return TryCreateConnectionAtRelease(targetSocket, window.transform);
    }


    /// <summary>Gets the node currently owning this socket assignment.</summary>
    public GraphNode3D AssignedOwnerNode => _ownerNode;


    public void SetConnectionAvailable(bool available)
    {
        if (_interactionColliders != null)
        {
            foreach (var interactionCollider in _interactionColliders)
            {
                if (interactionCollider != null)
                    interactionCollider.enabled = true;
            }
        }
        SetAlwaysOnVisualState();
    }

    /// <summary>Begins a physical socket drag and caches its exact return pose.</summary>
    public bool StartDrag()
    {
        if (_isDragging || _isAttracting || originalWindowAnchor == null)
        {
            return false;
        }

        _isAttracting = false;
        _windowAttractionEnabled = false;
        _attractionTargetSocket = null;
        _attractionTargetWindow = null;
        CacheOriginalAnchorPose();
        _dragTargetPosition = transform.position;
        CaptureOwnConnectionsForDrag();
        _isDragging = true;
        _isAnchored = false;
        ClearTemporaryLine();
        SetInteractionColliders(false);
        SetAnchoredPhysicsState();
        transform.SetParent(_ownerNode != null ? _ownerNode.transform : null, true);
        _temporaryLine = CreateLine("TemporaryGraphCable");
        SetAlwaysOnVisualState();
        return true;
    }

    /// <summary>Stores the next world-space position applied during the physics step.</summary>
    public void MoveDragTarget(Vector3 worldPosition)
    {
        if (!_isDragging || !IsFinite(worldPosition))
            return;
        _dragTargetPosition = worldPosition;
    }

    /// <summary>Moves the dragged socket body to its pending target from FixedUpdate.</summary>
    public void ApplyDragMovement()
    {
        if (!_isDragging || _rigidbodyReference == null)
            return;
        _rigidbodyReference.MovePosition(_dragTargetPosition);
    }

    /// <summary>Ends dragging, creates a logical edge inside the snap radius, and restores the socket.</summary>
    public void ReleaseDrag()
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        var target = FindClosestWindowSocket();
        if (!IsValidConnectionTarget(target))
        {
            RestoreToOriginalWindow();
            return;
        }

        TryCreateConnectionAtRelease(target, target.attachedWindowAnchor);
    }

    /// <summary>Cancels an interrupted drag and restores the socket to its original anchor.</summary>
    public void CancelDrag()
    {
        if (!_isDragging && !_isAttracting)
            return;
        _isDragging = false;
        RestoreToOriginalWindow();
    }

    private bool TryCreateConnectionAtRelease(GraphSocket3D target, Transform targetWindow)
    {
        if (!IsValidConnectionTarget(target) || originalWindowAnchor == null || targetWindow == null)
        {
            RestoreToOriginalWindow();
            return false;
        }

        var preserveOnReset = _preserveNextConnection;
        var edge = CreatePersistentEdge(
            target,
            targetWindow,
            edgeMaterial,
            lineWidth,
            preserveOnReset,
            edgeColor);
        if (edge == null)
        {
            RestoreToOriginalWindow();
            return false;
        }

        _preserveNextConnection = false;
        _isAttracting = false;
        _windowAttractionEnabled = false;
        _attractionTargetSocket = null;
        _attractionTargetWindow = null;
        ClearTemporaryLine();
        RestoreToOriginalWindow();
        return true;
    }

    private void ApplyAttractionMovement()
    {
        if (!_isAttracting || _rigidbodyReference == null)
            return;
        if (_attractionTargetSocket == null || _attractionTargetWindow == null)
        {
            RestoreToOriginalWindow();
            return;
        }

        _attractionElapsed += Time.fixedDeltaTime;
        var t = Mathf.Clamp01(_attractionElapsed / AttractionDuration);
        _rigidbodyReference.MovePosition(Vector3.Lerp(transform.position, _attractionTargetWindow.position, t));
        if (t < 1f)
            return;

        var target = _attractionTargetSocket;
        var targetWindow = _attractionTargetWindow;
        _isAttracting = false;
        _attractionTargetSocket = null;
        _attractionTargetWindow = null;
        TryCreateConnectionAtRelease(target, targetWindow);
    }

    private void ReleaseAsFreeBody()
    {
        RestoreToOriginalWindow();
    }

    internal void NotifyEdgeRemoved(GraphEdge edge)
    {
        if (edge != null)
            _connectedEdges.Remove(edge);
        SetAlwaysOnVisualState();
    }

    internal void RegisterEdge(GraphEdge edge)
    {
        if (edge != null && !_connectedEdges.Contains(edge))
            _connectedEdges.Add(edge);
    }

    internal void UnregisterEdge(GraphEdge edge)
    {
        if (edge != null)
            _connectedEdges.Remove(edge);
    }

    private void SetInteractionColliders(bool enabled)
    {
        if (_interactionColliders == null)
            return;

        foreach (var interactionCollider in _interactionColliders)
        {
            if (interactionCollider != null)
                interactionCollider.enabled = enabled;
        }
    }


    private void EnsureInteractionCollider()
    {
        if (GetComponent<SphereCollider>() == null)
            gameObject.AddComponent<SphereCollider>();
    }

    internal bool CanBeAutoAttached => _windowAttractionEnabled;

    private void ConfigurePhysicsBody()
    {
        if (_rigidbodyReference == null || _rigidbodyReference.gameObject != gameObject)
            _rigidbodyReference = GetComponent<Rigidbody>();
        if (_rigidbodyReference == null)
            _rigidbodyReference = gameObject.AddComponent<Rigidbody>();
        _rigidbodyReference.interpolation = RigidbodyInterpolation.Interpolate;
        _rigidbodyReference.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _rigidbodyReference.detectCollisions = true;
        SetAnchoredPhysicsState();
    }

    private void CacheOriginalAnchorPose()
    {
        if (_hasCachedOriginalPose || originalWindowAnchor == null)
            return;

        _originalAnchorLocalPosition = originalWindowAnchor.InverseTransformPoint(transform.position);
        _originalAnchorLocalRotation = Quaternion.Inverse(originalWindowAnchor.rotation) * transform.rotation;
        _originalAnchorLocalScale = transform.localScale;
        _hasCachedOriginalPose = true;
    }

    private void RestoreToOriginalWindow()
    {
        ClearTemporaryLine();
        _isAttracting = false;
        _windowAttractionEnabled = false;
        _attractionTargetSocket = null;
        _attractionTargetWindow = null;
        if (originalWindowAnchor == null || !_hasCachedOriginalPose)
        {
            RestoreSuspendedConnections();
            return;
        }

        if (_ownerNode != _originalOwnerNode)
        {
            _ownerNode?.RemoveAssignedSocket(this);
            _originalOwnerNode?.AddAssignedSocket(this);
            _ownerNode = _originalOwnerNode;
        }

        attachedWindowAnchor = originalWindowAnchor;
        transform.SetParent(originalWindowAnchor, false);
        transform.localPosition = _originalAnchorLocalPosition;
        transform.localRotation = _originalAnchorLocalRotation;
        transform.localScale = _originalAnchorLocalScale;
        _lastValidLocalPosition = transform.localPosition;
        _lastValidLocalRotation = transform.localRotation;
        if (_rigidbodyReference != null)
        {
            _rigidbodyReference.position = transform.position;
            _rigidbodyReference.rotation = transform.rotation;
            _rigidbodyReference.linearVelocity = Vector3.zero;
            _rigidbodyReference.angularVelocity = Vector3.zero;
        }
        SetInteractionColliders(true);
        SetAnchoredPhysicsState();
        SetAlwaysOnVisualState();
        RestoreSuspendedConnections();
    }

    private void SetAnchoredPhysicsState()
    {
        if (_rigidbodyReference == null)
            return;
        if (!_rigidbodyReference.isKinematic)
        {
            _rigidbodyReference.linearVelocity = Vector3.zero;
            _rigidbodyReference.angularVelocity = Vector3.zero;
        }
        _rigidbodyReference.isKinematic = true;
        _rigidbodyReference.useGravity = false;
        _isAnchored = true;
    }

    private void ClearTemporaryLine()
    {
        if (_temporaryLine == null)
            return;
        Destroy(_temporaryLine.gameObject);
        _temporaryLine = null;
    }

    private bool IsValidConnectionTarget(GraphSocket3D candidate)
    {
        return candidate != null
            && candidate != this
            && candidate.isActiveAndEnabled
            && candidate._ownerNode != null
            && candidate._ownerNode != _ownerNode
            && candidate.attachedWindowAnchor != null;
    }

    private GraphSocket3D FindClosestWindowSocket()
    {
        var count = Physics.OverlapSphereNonAlloc(transform.position, snapRadius, _overlapBuffer, socketLayerMask, QueryTriggerInteraction.Collide);
        if (count == _overlapBuffer.Length)
        {
            _overlapBuffer = new Collider[_overlapBuffer.Length * 2];
            count = Physics.OverlapSphereNonAlloc(transform.position, snapRadius, _overlapBuffer, socketLayerMask, QueryTriggerInteraction.Collide);
        }

        GraphSocket3D closest = null;
        var closestDistance = float.MaxValue;
        for (var i = 0; i < count; i++)
        {
            var hitCollider = _overlapBuffer[i];
            if (hitCollider == null || !hitCollider.enabled)
                continue;

            var candidate = hitCollider.GetComponentInParent<GraphSocket3D>();
            if (!IsValidConnectionTarget(candidate))
                continue;

            var distance = (candidate.attachedWindowAnchor.position - transform.position).sqrMagnitude;
            if (distance >= closestDistance)
                continue;
            closest = candidate;
            closestDistance = distance;
        }
        return closest;
    }

    private void CaptureOwnConnectionsForDrag()
    {
        _suspendedConnections.Clear();
        CleanupInvalidConnections();
        foreach (var edge in _connectedEdges)
        {
            if (edge == null || edge.StartSocket != this)
                continue;

            edge.SetVisible(false);
            _suspendedConnections.Add(new SuspendedConnection
            {
                Edge = edge,
                TargetSocket = edge.EndSocket,
                TargetWindow = edge.EndPoint,
                Material = edgeMaterial,
                Width = lineWidth,
                Color = edge.EdgeColor,
                PreserveOnReset = edge.PreserveOnReset
            });
        }
    }

    private void RestoreSuspendedConnections()
    {
        if (_suspendedConnections.Count == 0)
            return;

        foreach (var connection in _suspendedConnections)
        {
            if (connection == null || connection.TargetWindow == null || connection.TargetWindow == originalWindowAnchor)
            {
                if (connection?.Edge != null)
                    Destroy(connection.Edge.gameObject);
                continue;
            }

            if (connection.Edge != null && connection.Edge.EndPoint != null)
            {
                connection.Edge.SetVisible(true);
                RegisterEdge(connection.Edge);
                continue;
            }

            if (connection.Edge != null)
                Destroy(connection.Edge.gameObject);
            CreatePersistentEdge(
                connection.TargetSocket,
                connection.TargetWindow,
                connection.Material,
                connection.Width,
                connection.PreserveOnReset,
                connection.Color);
        }

        _suspendedConnections.Clear();
    }

    private void RemoveSuspendedConnections()
    {
        RemoveOwnConnections();
        _suspendedConnections.Clear();
    }

    private void RemoveAllConnections()
    {
        foreach (var edge in new List<GraphEdge>(_connectedEdges))
        {
            if (edge == null)
                continue;
            _connectedEdges.Remove(edge);
            Destroy(edge.gameObject);
        }

        _connectedEdges.Clear();
    }

    private void RemoveOwnConnections()
    {
        foreach (var edge in new List<GraphEdge>(_connectedEdges))
        {
            if (edge == null || edge.StartSocket != this)
                continue;
            _connectedEdges.Remove(edge);
            Destroy(edge.gameObject);
        }
    }

    private void CleanupInvalidConnections()
    {
        _connectedEdges.RemoveAll(edge => edge == null);
    }

    private GraphEdge CreatePersistentEdge(
        GraphSocket3D targetSocket,
        Transform targetWindow,
        Material material,
        float width,
        bool preserveOnReset,
        Color color)
    {
        if (originalWindowAnchor == null || targetWindow == null)
            return null;

        var ownerName = _ownerNode != null ? _ownerNode.name : name;
        var targetOwnerName = targetSocket != null && targetSocket._ownerNode != null
            ? targetSocket._ownerNode.name
            : targetWindow.name;
        var edgeObject = new GameObject($"GraphEdge_{ownerName}_{targetOwnerName}");
        var edge = edgeObject.AddComponent<GraphEdge>();
        edge.SetPreserveOnReset(preserveOnReset);
        edge.Initialize(
            this,
            targetSocket,
            originalWindowAnchor,
            targetWindow,
            material != null ? material : edgeMaterial,
            width,
            color);
        return edge;
    }

    private void AttachToWindow(Transform targetWindow, bool preserveWorldPose = false)
    {
        if (targetWindow == null)
            return;
        var targetNode = targetWindow.GetComponentInParent<GraphNode3D>();
        if (targetNode != null && targetNode != _ownerNode)
        {
            _ownerNode?.RemoveAssignedSocket(this);
            targetNode.AddAssignedSocket(this);
            _ownerNode = targetNode;
        }
        attachedWindowAnchor = targetWindow;
        transform.SetParent(targetWindow, preserveWorldPose);
        targetNode?.IgnoreSocketCollisions(this);
        if (!preserveWorldPose)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    private void SanitizeTransform()
    {
        if (IsFinite(transform.localPosition) && IsFinite(transform.localRotation))
        {
            _lastValidLocalPosition = transform.localPosition;
            _lastValidLocalRotation = transform.localRotation;
            return;
        }

        Debug.LogError($"[{nameof(GraphSocket3D)}] {name}: pose no válida; se restaurará la última pose válida.", this);
        transform.localPosition = _lastValidLocalPosition;
        transform.localRotation = _lastValidLocalRotation;
        SetAnchoredPhysicsState();
    }

    private void SetAlwaysOnVisualState()
    {
        if (_renderer != null)
        {
            _renderer.enabled = true;
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, edgeColor);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }

    private LineRenderer CreateLine(string lineName)
    {
        var lineObject = new GameObject(lineName);
        var line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.numCapVertices = 6;
        line.sharedMaterial = edgeMaterial;
        var lineStart = originalWindowAnchor != null ? originalWindowAnchor.position : transform.position;
        line.SetPosition(0, lineStart);
        line.SetPosition(1, transform.position);
        return line;
    }

    private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    private static bool IsFinite(Quaternion value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = edgeColor;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}

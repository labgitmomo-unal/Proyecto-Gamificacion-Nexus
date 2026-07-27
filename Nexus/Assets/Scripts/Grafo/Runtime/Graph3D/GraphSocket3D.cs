using UnityEngine;

[DisallowMultipleComponent]
public sealed class GraphSocket3D : MonoBehaviour
{
    private const float DefaultSnapRadius = 0.9f;
    private const float DefaultLineWidth = 0.045f;
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

    private GraphNode3D _ownerNode;
    private bool _isDragging;
    private bool _hasCachedOriginalPose;
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
        SetAlwaysOnVisualState();
    }

    private void Update()
    {
        SanitizeTransform();
        if (_temporaryLine == null)
            return;

        var lineStart = originalWindowAnchor != null ? originalWindowAnchor.position : transform.position;
        _temporaryLine.SetPosition(0, lineStart);
        _temporaryLine.SetPosition(1, transform.position);
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
        if (_lights == null)
            return;

        foreach (var light in _lights)
        {
            if (light == null)
                continue;
            light.enabled = true;
            light.color = edgeColor;
            if (lightIntensity >= 0f)
                light.intensity = lightIntensity;
            if (lightRange >= 0f)
                light.range = lightRange;
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

    /// <summary>Keeps the socket colliders and visual state available for ray interaction.</summary>
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
    public void StartDrag()
    {
        if (_isDragging || originalWindowAnchor == null)
            return;

        CacheOriginalAnchorPose();
        _isDragging = true;
        RemoveExistingEdge();
        ClearTemporaryLine();
        SetAnchoredPhysicsState();
        _dragTargetPosition = transform.position;
        transform.SetParent(_ownerNode != null ? _ownerNode.transform : null, true);
        _temporaryLine = CreateLine("TemporaryGraphCable");
        SetAlwaysOnVisualState();
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

    /// <summary>Ends dragging and either connects to a valid window or restores the original pose.</summary>
    public void ReleaseDrag()
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        var target = FindClosestWindowSocket();
        if (!IsValidConnectionTarget(target))
        {
            ClearTemporaryLine();
            RestoreToOriginalWindow();
            return;
        }

        var targetWindow = target.attachedWindowAnchor;
        var displacedSocket = target.FindSocketOccupyingWindow(targetWindow);
        if (displacedSocket != null && displacedSocket != this)
            displacedSocket.RestoreToOriginalWindow();

        target.RemoveExistingEdge();
        var ownerName = _ownerNode != null ? _ownerNode.name : name;
        var targetOwnerName = target._ownerNode != null ? target._ownerNode.name : target.name;
        var edgeObject = new GameObject($"GraphEdge_{ownerName}_{targetOwnerName}");
        var edge = edgeObject.AddComponent<GraphEdge>();
        edge.Initialize(originalWindowAnchor, targetWindow, edgeMaterial, lineWidth);
        _connectedEdge = edge;
        target.SetIncomingConnection(edge);

        ClearTemporaryLine();
        AttachToWindow(targetWindow);
        SetAnchoredPhysicsState();
        SetAlwaysOnVisualState();
    }

    /// <summary>Cancels an interrupted drag and restores the socket to its original window.</summary>
    public void CancelDrag()
    {
        if (!_isDragging)
            return;
        _isDragging = false;
        ClearTemporaryLine();
        RestoreToOriginalWindow();
    }

    internal void NotifyEdgeRemoved(GraphEdge edge)
    {
        if (_connectedEdge == edge)
            _connectedEdge = null;
        SetAlwaysOnVisualState();
    }

    private void EnsureInteractionCollider()
    {
        if (GetComponent<SphereCollider>() == null)
            gameObject.AddComponent<SphereCollider>();
    }

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
        if (originalWindowAnchor == null || !_hasCachedOriginalPose)
            return;

        attachedWindowAnchor = originalWindowAnchor;
        transform.SetParent(originalWindowAnchor, false);
        transform.localPosition = _originalAnchorLocalPosition;
        transform.localRotation = _originalAnchorLocalRotation;
        transform.localScale = _originalAnchorLocalScale;
        _lastValidLocalPosition = transform.localPosition;
        _lastValidLocalRotation = transform.localRotation;
        SetAnchoredPhysicsState();
        SetAlwaysOnVisualState();
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

    private GraphSocket3D FindSocketOccupyingWindow(Transform window)
    {
        if (_ownerNode == null || window == null)
            return null;
        foreach (var socket in _ownerNode.Sockets)
        {
            if (socket != null && socket != this && socket.attachedWindowAnchor == window)
                return socket;
        }
        return null;
    }

    private void SetIncomingConnection(GraphEdge edge)
    {
        _connectedEdge = edge;
        SetAlwaysOnVisualState();
    }

    private void RemoveExistingEdge()
    {
        if (_connectedEdge == null)
            return;
        var edge = _connectedEdge;
        _connectedEdge = null;
        Destroy(edge.gameObject);
    }

    private void AttachToWindow(Transform targetWindow)
    {
        if (targetWindow == null)
            return;
        attachedWindowAnchor = targetWindow;
        transform.SetParent(targetWindow, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
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

        if (_lights == null)
            return;
        foreach (var socketLight in _lights)
        {
            if (socketLight == null)
                continue;
            socketLight.enabled = true;
            socketLight.color = edgeColor;
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

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

    private XRGrabInteractable _grabInteractable;
    private GraphNode3D _ownerNode;
    private Vector3 _lastValidLocalPosition;
    private Quaternion _lastValidLocalRotation;
    private Vector3 _dragStartPosition;
    private Transform _dragStartParent;
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
        _interactionColliders = GetComponentsInChildren<Collider>(true);
        ConfigurePhysicsBody();

        _grabInteractable = GetComponent<XRGrabInteractable>();
        if (_grabInteractable == null)
            _grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
        _grabInteractable.enabled = true;
        _grabInteractable.autoFindParentInteractableInHierarchy = false;
        _grabInteractable.parentInteractable = null;
        _grabInteractable.colliders.Clear();
        foreach (var collider in _interactionColliders)
            if (collider != null)
                _grabInteractable.colliders.Add(collider);
        _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        _grabInteractable.trackPosition = true;
        _grabInteractable.trackRotation = false;
        _grabInteractable.trackScale = false;
        _grabInteractable.throwOnDetach = false;
        _grabInteractable.forceGravityOnDetach = false;
        _grabInteractable.retainTransformParent = true;
        _grabInteractable.selectEntered.AddListener(BeginCableDrag);
        _grabInteractable.selectExited.AddListener(EndCableDrag);
        SetAlwaysOnVisualState();
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
            originalWindowAnchor = window;
        attachedWindowAnchor = window;
    }

    /// <summary>Leaves the socket interactable and its visual state enabled.</summary>
    public void SetConnectionAvailable(bool available)
    {
        if (_grabInteractable != null)
            _grabInteractable.enabled = true;
        if (_interactionColliders != null)
        {
            foreach (var collider in _interactionColliders)
                if (collider != null)
                    collider.enabled = true;
        }
        SetAlwaysOnVisualState();
    }

    private void OnDestroy()
    {
        if (_grabInteractable == null)
            return;
        _grabInteractable.selectEntered.RemoveListener(BeginCableDrag);
        _grabInteractable.selectExited.RemoveListener(EndCableDrag);
    }

    private void ConfigurePhysicsBody()
    {
        if (_rigidbodyReference == null)
            _rigidbodyReference = GetComponent<Rigidbody>();
        if (_rigidbodyReference == null)
            _rigidbodyReference = gameObject.AddComponent<Rigidbody>();
        _rigidbodyReference.isKinematic = true;
        _rigidbodyReference.useGravity = false;
        _rigidbodyReference.interpolation = RigidbodyInterpolation.None;
        _rigidbodyReference.collisionDetectionMode = CollisionDetectionMode.Discrete;
        _rigidbodyReference.detectCollisions = true;
    }

    private void Update()
    {
        SanitizeTransform();
        if (_temporaryLine == null)
            return;
        _temporaryLine.SetPosition(0, _dragStartPosition);
        _temporaryLine.SetPosition(1, transform.position);
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
        SetPhysicsState(false, true);
    }

    private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    private static bool IsFinite(Quaternion value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private void BeginCableDrag(SelectEnterEventArgs args)
    {
        RemoveExistingEdge();
        if (_rigidbodyReference != null)
        {
            _rigidbodyReference.linearVelocity = Vector3.zero;
            _rigidbodyReference.angularVelocity = Vector3.zero;
        }
        SetPhysicsState(false, true);
        _dragStartPosition = originalWindowAnchor != null ? originalWindowAnchor.position : transform.position;
        _dragStartParent = transform.parent;
        transform.SetParent(_ownerNode != null ? _ownerNode.transform : null, true);
        _temporaryLine = CreateLine("TemporaryGraphCable");
        SetAlwaysOnVisualState();
    }

    private void EndCableDrag(SelectExitEventArgs args)
    {
        if (_temporaryLine == null)
            return;

        var target = FindClosestWindowSocket();
        if (target == null)
        {
            Destroy(_temporaryLine.gameObject);
            _temporaryLine = null;
            ReleaseWithGravity();
            return;
        }

        var targetWindow = target.attachedWindowAnchor;
        if (targetWindow == null || target._ownerNode == _ownerNode)
        {
            ReleaseWithGravity();
            return;
        }

        var displacedSocket = target.FindSocketOccupyingWindow(targetWindow);
        if (displacedSocket != null && displacedSocket != this)
            displacedSocket.ReleaseWithGravity();

        target.RemoveExistingEdge();
        var ownerName = _ownerNode != null ? _ownerNode.name : name;
        var targetOwnerName = target._ownerNode != null ? target._ownerNode.name : target.name;
        var edgeObject = new GameObject($"GraphEdge_{ownerName}_{targetOwnerName}");
        var edge = edgeObject.AddComponent<GraphEdge>();
        var startAnchor = originalWindowAnchor != null ? originalWindowAnchor : _dragStartParent;
        edge.Initialize(startAnchor, targetWindow, edgeMaterial, lineWidth);
        _connectedEdge = edge;
        target.SetIncomingConnection(edge);
        Destroy(_temporaryLine.gameObject);
        _temporaryLine = null;
        AttachToWindow(targetWindow);
        SetPhysicsState(false, true);
        SetAlwaysOnVisualState();
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
            var collider = _overlapBuffer[i];
            if (collider == null)
                continue;
            var candidate = collider.GetComponentInParent<GraphSocket3D>();
            if (candidate == null || candidate == this || !candidate.isActiveAndEnabled || candidate._ownerNode == _ownerNode || candidate.attachedWindowAnchor == null)
                continue;
            var distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = candidate;
                closestDistance = distance;
            }
        }
        return closest;
    }

    private GraphSocket3D FindSocketOccupyingWindow(Transform window)
    {
        if (_ownerNode == null || window == null)
            return null;
        foreach (var socket in _ownerNode.Sockets)
            if (socket != null && socket != this && socket.attachedWindowAnchor == window)
                return socket;
        return null;
    }

    private void SetIncomingConnection(GraphEdge edge)
    {
        _connectedEdge = edge;
        SetAlwaysOnVisualState();
    }

    internal void NotifyEdgeRemoved(GraphEdge edge)
    {
        if (_connectedEdge == edge)
            _connectedEdge = null;
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

    private void ReleaseWithGravity()
    {
        if (_temporaryLine != null)
        {
            Destroy(_temporaryLine.gameObject);
            _temporaryLine = null;
        }
        attachedWindowAnchor = null;
        transform.SetParent(_ownerNode != null ? _ownerNode.transform : null, true);
        SetPhysicsState(true, false);
        SetAlwaysOnVisualState();
    }

    private void SetPhysicsState(bool useGravity, bool isKinematic)
    {
        if (_rigidbodyReference == null)
            return;
        _rigidbodyReference.useGravity = useGravity;
        _rigidbodyReference.isKinematic = isKinematic;
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
        foreach (var light in _lights)
        {
            if (light == null)
                continue;
            light.enabled = true;
            light.color = edgeColor;
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
        line.SetPosition(0, _dragStartPosition);
        line.SetPosition(1, transform.position);
        return line;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = edgeColor;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}

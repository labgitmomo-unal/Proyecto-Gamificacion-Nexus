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

    private XRGrabInteractable _grabInteractable;
    private GraphNode3D _ownerNode;
    private Vector3 _homeLocalPosition;
    private Quaternion _homeLocalRotation;
    private Vector3 _dragStartPosition;
    private LineRenderer _temporaryLine;
    private GraphEdge _connectedEdge;
    private Collider[] _overlapBuffer;
    private MaterialPropertyBlock _propertyBlock;
    private Renderer _renderer;
    private bool _visualStateActive;

    public void Configure(Color color, GraphNode3D ownerNode)
    {
        Configure(color, ownerNode, -1f, -1f);
    }

    public void Configure(Color color, GraphNode3D ownerNode, float lightIntensity, float lightRange)
    {
        edgeColor = color;
        _ownerNode = ownerNode;
        ApplyVisualState(_visualStateActive);
        if (lightIntensity >= 0f || lightRange >= 0f)
        {
            foreach (var light in GetComponentsInChildren<Light>(true))
            {
                light.color = edgeColor;
                if (lightIntensity >= 0f)
                    light.intensity = lightIntensity;
                if (lightRange >= 0f)
                    light.range = lightRange;
            }
        }
    }

    private void Awake()
    {
        _homeLocalPosition = transform.localPosition;
        _homeLocalRotation = transform.localRotation;
        _overlapBuffer = new Collider[Mathf.Max(1, overlapBufferCapacity)];
        _propertyBlock = new MaterialPropertyBlock();
        _renderer = GetComponent<Renderer>();
        ConfigurePhysicsBody();
        _grabInteractable = GetComponent<XRGrabInteractable>();
        if (_grabInteractable == null)
            _grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
        _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        _grabInteractable.trackPosition = true;
        _grabInteractable.trackRotation = false;
        _grabInteractable.trackScale = false;
        _grabInteractable.throwOnDetach = false;
        _grabInteractable.forceGravityOnDetach = false;
        _grabInteractable.retainTransformParent = true;
        _grabInteractable.selectEntered.AddListener(BeginCableDrag);
        _grabInteractable.selectExited.AddListener(EndCableDrag);
        ApplyVisualState(false);
    }

    private void ConfigurePhysicsBody()
    {
        var body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.None;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.detectCollisions = true;
        body.angularVelocity = Vector3.zero;
        body.linearVelocity = Vector3.zero;
    }

    private void OnDestroy()
    {
        if (_grabInteractable == null)
            return;
        _grabInteractable.selectEntered.RemoveListener(BeginCableDrag);
        _grabInteractable.selectExited.RemoveListener(EndCableDrag);
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
            return;
        Debug.LogError($"[{nameof(GraphSocket3D)}] {name}: pose no válida; se restaurará.", this);
        transform.localPosition = _homeLocalPosition;
        transform.localRotation = _homeLocalRotation;
        var body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    private static bool IsFinite(Quaternion value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private void BeginCableDrag(SelectEnterEventArgs args)
    {
        RemoveExistingEdge();
        _dragStartPosition = transform.position;
        _temporaryLine = CreateLine("TemporaryGraphCable");
        SetVisualState(true);
    }

    private void EndCableDrag(SelectExitEventArgs args)
    {
        if (_temporaryLine == null)
            return;
        var target = FindClosestSocket();
        if (target == null)
        {
            Destroy(_temporaryLine.gameObject);
            _temporaryLine = null;
            ResetToHomePosition();
            SetVisualState(false);
            return;
        }

        var ownerName = _ownerNode != null ? _ownerNode.name : name;
        var targetOwnerName = target._ownerNode != null ? target._ownerNode.name : target.name;
        var edgeObject = new GameObject($"GraphEdge_{ownerName}_{targetOwnerName}");
        var edge = edgeObject.AddComponent<GraphEdge>();
        edge.Initialize(transform, target.transform, _temporaryLine.sharedMaterial, lineWidth);
        _connectedEdge = edge;
        target.SetIncomingConnection(edge);
        Destroy(_temporaryLine.gameObject);
        _temporaryLine = null;
        ResetToHomePosition();
        SetVisualState(true);
    }

    private GraphSocket3D FindClosestSocket()
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
            if (candidate == null || candidate == this || !candidate.isActiveAndEnabled || candidate._ownerNode == _ownerNode)
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

    private void SetIncomingConnection(GraphEdge edge)
    {
        _connectedEdge = edge;
        SetVisualState(true);
    }

    private void RemoveExistingEdge()
    {
        if (_connectedEdge == null)
            return;
        if (_connectedEdge != null)
            Destroy(_connectedEdge.gameObject);
        _connectedEdge = null;
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

    private void ResetToHomePosition()
    {
        transform.localPosition = _homeLocalPosition;
        transform.localRotation = _homeLocalRotation;
    }

    private void SetVisualState(bool active)
    {
        if (_visualStateActive == active && _renderer != null)
            return;
        _visualStateActive = active;
        ApplyVisualState(active);
    }

    private void ApplyVisualState(bool active)
    {
        if (_renderer == null)
            return;
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(BaseColorId, active ? edgeColor : Color.gray);
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = edgeColor;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}

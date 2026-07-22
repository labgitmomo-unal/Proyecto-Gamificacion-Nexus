using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Permite agarrar una luz y arrastrar un cable vivo hasta otra luz de un nodo.
/// </summary>
[DisallowMultipleComponent]
public sealed class GraphSocket3D : MonoBehaviour
{
    private const float DefaultSnapRadius = 0.9f;
    private const float DefaultLineWidth = 0.045f;

    [SerializeField] private float snapRadius = DefaultSnapRadius;
    [SerializeField] private float lineWidth = DefaultLineWidth;
    [SerializeField] private Color edgeColor = Color.cyan;

    private XRGrabInteractable _grabInteractable;
    private GraphNode3D _ownerNode;
    private Vector3 _homeLocalPosition;
    private Quaternion _homeLocalRotation;
    private Vector3 _dragStartPosition;
    private LineRenderer _temporaryLine;
    private GraphEdge _connectedEdge;

    /// <summary>
    /// Configura el color y el nodo dueño de esta luz.
    /// </summary>
    public void Configure(Color color, GraphNode3D ownerNode)
    {
        edgeColor = color;
        _ownerNode = ownerNode;
    }

    private void Awake()
    {
        _homeLocalPosition = transform.localPosition;
        _homeLocalRotation = transform.localRotation;
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

        Debug.LogError($"{nameof(GraphSocket3D)} en {name} recibió una pose no válida y será restaurado.", this);
        transform.localPosition = _homeLocalPosition;
        transform.localRotation = _homeLocalRotation;
        var body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(Quaternion value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

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

        var edgeObject = new GameObject($"GraphEdge_{_ownerNode.name}_{target._ownerNode.name}");
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
        var colliders = Physics.OverlapSphere(transform.position, snapRadius);
        GraphSocket3D closest = null;
        var closestDistance = float.MaxValue;

        foreach (var collider in colliders)
        {
            var candidate = collider.GetComponent<GraphSocket3D>();
            if (candidate == null || candidate == this || candidate._ownerNode == _ownerNode)
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
        line.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = edgeColor };
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
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = active ? edgeColor : Color.gray;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = edgeColor;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}

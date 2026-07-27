using UnityEngine;
using UnityEngine.XR;

/// <summary>Handles ray-based graph interaction with independent XR triggers.</summary>
public sealed class GraphGrabController : MonoBehaviour
{
    private const float TriggerThreshold = 0.5f;
    private const float TriggerReleaseThreshold = 0.3f;
    private const float MinimumGrabDistance = 0.01f;
    private const int RaycastHitCapacity = 32;

    [SerializeField] private Transform rightAimTransform;
    [SerializeField] private Transform leftAimTransform;
    [SerializeField] private float maximumGrabDistance = 20f;
    [SerializeField] private LayerMask nodeLayerMask = ~0;
    [SerializeField] private LayerMask socketLayerMask = ~0;

    private readonly RaycastHit[] _nodeRaycastHits = new RaycastHit[RaycastHitCapacity];
    private readonly RaycastHit[] _socketRaycastHits = new RaycastHit[RaycastHitCapacity];

    private GraphNode3D _heldNode;
    private Rigidbody _heldNodeBody;
    private float _nodeRayDistance;
    private Vector3 _nodeRootOffset;
    private Quaternion _nodeTargetRotation;
    private Vector3 _nodeTargetPosition;
    private bool _nodeWasKinematic;
    private bool _nodeUsedGravity;

    private GraphSocket3D _heldSocket;
    private float _socketRayDistance;
    private Vector3 _socketTargetPosition;

    private bool _rightPressed;
    private bool _leftPressed;

    private void Update()
    {
        HandleRightTrigger(ReadTrigger(XRNode.RightHand));
        HandleLeftTrigger(ReadTrigger(XRNode.LeftHand));

        if (_heldNode != null)
            UpdateNodeTargetPose();
        if (_heldSocket != null)
            UpdateSocketTargetPosition();
    }

    private void FixedUpdate()
    {
        MoveHeldBodies();
    }

    private void OnDisable()
    {
        ReleaseNode();
        if (_heldSocket != null)
            _heldSocket.CancelDrag();
        _heldSocket = null;
        _rightPressed = false;
        _leftPressed = false;
    }

    private void HandleRightTrigger(float trigger)
    {
        var pressing = trigger > TriggerThreshold;
        var releasing = trigger < TriggerReleaseThreshold;

        if (pressing && !_rightPressed)
        {
            _rightPressed = true;
            TryAcquireNodeFromRay();
        }
        else if (releasing && _rightPressed)
        {
            _rightPressed = false;
            ReleaseNode();
        }
    }

    private void HandleLeftTrigger(float trigger)
    {
        var pressing = trigger > TriggerThreshold;
        var releasing = trigger < TriggerReleaseThreshold;

        if (pressing && !_leftPressed)
        {
            _leftPressed = true;
            TryAcquireSocketFromRay();
        }
        else if (releasing && _leftPressed)
        {
            _leftPressed = false;
            ReleaseSocket();
        }
    }

    private void TryAcquireNodeFromRay()
    {
        if (rightAimTransform == null || _heldNode != null || _heldSocket != null)
            return;

        var ray = new Ray(rightAimTransform.position, rightAimTransform.forward);
        var hitCount = Physics.RaycastNonAlloc(ray, _nodeRaycastHits, maximumGrabDistance, nodeLayerMask, QueryTriggerInteraction.Collide);
        GraphNode3D closestNode = null;
        Rigidbody closestBody = null;
        var closestHit = default(RaycastHit);
        var closestDistance = float.MaxValue;

        for (var i = 0; i < hitCount; i++)
        {
            var hit = _nodeRaycastHits[i];
            if (hit.distance >= closestDistance || hit.collider == null || !hit.collider.enabled || !hit.collider.gameObject.activeInHierarchy)
                continue;

            var node = hit.collider.GetComponentInParent<GraphNode3D>();
            if (node == null || !node.isActiveAndEnabled)
                continue;

            var body = node.PhysicsBody;
            if (body == null || body.gameObject != node.gameObject)
            {
                Debug.LogWarning($"[{nameof(GraphGrabController)}] {node.name}: el nodo no tiene un Rigidbody raíz válido.", node);
                continue;
            }

            closestNode = node;
            closestBody = body;
            closestHit = hit;
            closestDistance = hit.distance;
        }

        if (closestNode == null || closestBody == null)
            return;

        _heldNode = closestNode;
        _heldNodeBody = closestBody;
        _nodeRayDistance = Mathf.Max(MinimumGrabDistance, closestHit.distance);
        _nodeRootOffset = closestBody.position - closestHit.point;
        _nodeTargetPosition = closestBody.position;
        _nodeTargetRotation = closestBody.rotation;
        _nodeWasKinematic = closestBody.isKinematic;
        _nodeUsedGravity = closestBody.useGravity;

        closestBody.isKinematic = true;
        closestBody.useGravity = false;
        closestBody.linearVelocity = Vector3.zero;
        closestBody.angularVelocity = Vector3.zero;
    }

    private void TryAcquireSocketFromRay()
    {
        if (leftAimTransform == null || _heldSocket != null || _heldNode != null)
            return;

        var ray = new Ray(leftAimTransform.position, leftAimTransform.forward);
        var hitCount = Physics.RaycastNonAlloc(ray, _socketRaycastHits, maximumGrabDistance, socketLayerMask, QueryTriggerInteraction.Collide);
        GraphSocket3D closestSocket = null;
        var closestHit = default(RaycastHit);
        var closestDistance = float.MaxValue;

        for (var i = 0; i < hitCount; i++)
        {
            var hit = _socketRaycastHits[i];
            if (hit.distance >= closestDistance || hit.collider == null || !hit.collider.enabled || !hit.collider.gameObject.activeInHierarchy)
                continue;

            var socket = hit.collider.GetComponent<GraphSocket3D>();
            if (socket == null || !socket.isActiveAndEnabled || !(hit.collider is SphereCollider))
                continue;

            closestSocket = socket;
            closestHit = hit;
            closestDistance = hit.distance;
        }

        if (closestSocket == null)
            return;

        _heldSocket = closestSocket;
        _socketRayDistance = Mathf.Max(MinimumGrabDistance, closestHit.distance);
        _socketTargetPosition = closestHit.point;
        closestSocket.StartDrag();
    }

    private void UpdateNodeTargetPose()
    {
        if (rightAimTransform == null)
            return;
        var rayPoint = rightAimTransform.position + rightAimTransform.forward * _nodeRayDistance;
        _nodeTargetPosition = rayPoint + _nodeRootOffset;
    }

    private void UpdateSocketTargetPosition()
    {
        if (leftAimTransform == null)
            return;
        _socketTargetPosition = leftAimTransform.position + leftAimTransform.forward * _socketRayDistance;
        _heldSocket.MoveDragTarget(_socketTargetPosition);
    }

    private void MoveHeldBodies()
    {
        if (_heldNodeBody != null)
        {
            _heldNodeBody.MovePosition(_nodeTargetPosition);
            _heldNodeBody.MoveRotation(_nodeTargetRotation);
        }

        if (_heldSocket != null)
            _heldSocket.ApplyDragMovement();
    }

    private void ReleaseNode()
    {
        if (_heldNodeBody != null)
        {
            _heldNodeBody.isKinematic = false;
            _heldNodeBody.useGravity = true;
            _heldNodeBody.linearVelocity = Vector3.zero;
            _heldNodeBody.angularVelocity = Vector3.zero;
        }

        _heldNode = null;
        _heldNodeBody = null;
        _nodeWasKinematic = false;
        _nodeUsedGravity = false;
    }

    private void ReleaseSocket()
    {
        if (_heldSocket == null)
            return;
        _heldSocket.ReleaseDrag();
        _heldSocket = null;
    }

    private static float ReadTrigger(XRNode node)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.trigger, out var value))
            return value;
        return 0f;
    }
}

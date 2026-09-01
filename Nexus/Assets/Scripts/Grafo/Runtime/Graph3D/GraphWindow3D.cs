using UnityEngine;

[DisallowMultipleComponent]
public sealed class GraphWindow3D : MonoBehaviour
{
    private const float DefaultAttractionRadius = 0.9f;
    private const int DefaultOverlapCapacity = 16;

    [SerializeField] private float attractionRadius = DefaultAttractionRadius;
    [SerializeField] private LayerMask socketLayerMask = ~0;
    [SerializeField] private int overlapBufferCapacity = DefaultOverlapCapacity;

    private Collider[] _overlapBuffer;
    private GraphSocket3D _anchorSocket;
    private GraphSocket3D _assignedSocket;
    private float _lastCheckTime;

    public GraphSocket3D AnchorSocket => _anchorSocket;
    public GraphSocket3D AssignedSocket => _assignedSocket;

    private void Awake()
    {
        _overlapBuffer = new Collider[Mathf.Max(DefaultOverlapCapacity, overlapBufferCapacity)];
    }

    private void FixedUpdate()
    {
        if (Time.time - _lastCheckTime < 0.2f) return;
        _lastCheckTime = Time.time;

        if (_assignedSocket != null && _assignedSocket.isActiveAndEnabled && !_assignedSocket.IsFreeBody)
            return;

        _assignedSocket = null;
        var count = Physics.OverlapSphereNonAlloc(transform.position, attractionRadius, _overlapBuffer, socketLayerMask, QueryTriggerInteraction.Collide);
        for (var i = 0; i < count; i++)
        {
            var collider = _overlapBuffer[i];
            if (collider == null)
                continue;

            var socket = collider.GetComponentInParent<GraphSocket3D>();
            if (socket == null || socket == _anchorSocket || !socket.IsFreeBody || !socket.CanBeAutoAttached)
                continue;

            if (socket.TryAttachToWindow(this))
            {
                _assignedSocket = socket;
                break;
            }
        }
    }

    public void RegisterAnchorSocket(GraphSocket3D socket)
    {
        if (socket != null)
            _anchorSocket = socket;
    }
}

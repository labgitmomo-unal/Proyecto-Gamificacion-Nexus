using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class GraphEdge : MonoBehaviour
{
    private const float DefaultLineWidth = 0.05f;

    private LineRenderer _lineRenderer;
    private Transform _startPoint;
    private Transform _endPoint;
    private GraphSocket3D _startSocket;
    private GraphSocket3D _endSocket;
    private Vector3 _freeEndPosition;
    private bool _usesFreeEnd;

    public bool PreserveOnReset { get; private set; }

    internal GraphSocket3D StartSocket => _startSocket;
    internal GraphSocket3D EndSocket => _endSocket;
    internal Transform EndPoint => _endPoint;

    /// <summary>Marks this edge as the demonstration edge that survives a graph reset.</summary>
    public void SetPreserveOnReset(bool preserve)
    {
        PreserveOnReset = preserve;
    }

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.numCapVertices = 6;
    }

    /// <summary>Initializes a persistent edge between two graph sockets.</summary>
    public void Initialize(GraphSocket3D start, GraphSocket3D end, Material edgeMaterial, float width)
    {
        Initialize(start, end, start != null ? start.transform : null, end != null ? end.transform : null, edgeMaterial, width);
    }

    internal void Initialize(GraphSocket3D start, GraphSocket3D end, Transform startPoint, Transform endPoint, Material edgeMaterial, float width)
    {
        _startSocket = start;
        _endSocket = end;
        Initialize(startPoint, endPoint, edgeMaterial, width);
        _startSocket?.RegisterEdge(this);
        _endSocket?.RegisterEdge(this);
    }

    /// <summary>Initializes an edge from world-space transforms.</summary>
    public void Initialize(Transform start, Transform end, Material edgeMaterial, float width)
    {
        _startPoint = start;
        _endPoint = end;
        _usesFreeEnd = end == null;
        _freeEndPosition = start != null ? start.position : Vector3.zero;
        if (edgeMaterial != null)
            _lineRenderer.sharedMaterial = edgeMaterial;
        _lineRenderer.startWidth = width;
        _lineRenderer.endWidth = width;
        Refresh();
    }

    /// <summary>Initializes an edge using the default line width.</summary>
    public void Initialize(Transform start, Transform end, Material edgeMaterial)
    {
        Initialize(start, end, edgeMaterial, DefaultLineWidth);
    }

    /// <summary>Updates the free endpoint of a temporary edge.</summary>
    public void UpdateEndPoint(Vector3 position)
    {
        _freeEndPosition = position;
        _usesFreeEnd = true;
        Refresh();
    }

    internal void SetVisible(bool visible)
    {
        if (_lineRenderer != null)
            _lineRenderer.enabled = visible;
    }

    private void OnDestroy()
    {
        _startSocket?.NotifyEdgeRemoved(this);
        _endSocket?.NotifyEdgeRemoved(this);
    }

    private void LateUpdate()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (_lineRenderer == null)
            return;
        if (_startPoint == null)
        {
            Destroy(gameObject);
            return;
        }
        _lineRenderer.SetPosition(0, _startPoint.position);
        if (_endPoint != null)
        {
            _lineRenderer.SetPosition(1, _endPoint.position);
            return;
        }
        if (_usesFreeEnd)
        {
            _lineRenderer.SetPosition(1, _freeEndPosition);
            return;
        }
        Destroy(gameObject);
    }
}

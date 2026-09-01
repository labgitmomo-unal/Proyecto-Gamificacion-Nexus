using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class GraphEdge : MonoBehaviour
{
    private const float DefaultLineWidth = 0.05f;
    private static readonly Color ExampleEdgeColor = new Color(1f, 0.92f, 0.02f, 1f);
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private LineRenderer _lineRenderer;
    private Transform _startPoint;
    private Transform _endPoint;
    private GraphSocket3D _startSocket;
    private GraphSocket3D _endSocket;
    private Vector3 _freeEndPosition;
    private Color _edgeColor = Color.white;
    private bool _usesFreeEnd;
    private Vector3 _lastStartPos;
    private Vector3 _lastEndPos;

    public static event System.Action TopologyChanged;

    public bool PreserveOnReset { get; private set; }

    internal GraphSocket3D StartSocket => _startSocket;
    internal GraphSocket3D EndSocket => _endSocket;
    internal Transform EndPoint => _endPoint;
    internal Color EdgeColor => _edgeColor;
    internal Color SelectedEdgeColor => _edgeColor;
    private Color EffectiveColor => PreserveOnReset ? ExampleEdgeColor : _edgeColor;

    /// <summary>Marks this edge as the demonstration edge that survives a graph reset.</summary>
    public void SetPreserveOnReset(bool preserve)
    {
        PreserveOnReset = preserve;
        ApplyColor();
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
        Initialize(start, end, start != null ? start.transform : null, end != null ? end.transform : null, edgeMaterial, width, start != null ? start.EdgeColor : Color.white);
    }

    internal void Initialize(GraphSocket3D start, GraphSocket3D end, Transform startPoint, Transform endPoint, Material edgeMaterial, float width)
    {
        Initialize(start, end, startPoint, endPoint, edgeMaterial, width, start != null ? start.EdgeColor : Color.white);
    }

    internal void Initialize(GraphSocket3D start, GraphSocket3D end, Transform startPoint, Transform endPoint, Material edgeMaterial, float width, Color color)
    {
        _startSocket = start;
        _endSocket = end;
        _edgeColor = color;
        Initialize(startPoint, endPoint, edgeMaterial, width, color);
        _startSocket?.RegisterEdge(this);
        _endSocket?.RegisterEdge(this);
        TopologyChanged?.Invoke();
    }

    /// <summary>Initializes an edge from world-space transforms.</summary>
    public void Initialize(Transform start, Transform end, Material edgeMaterial, float width)
    {
        Initialize(start, end, edgeMaterial, width, Color.white);
    }

    private void Initialize(Transform start, Transform end, Material edgeMaterial, float width, Color color)
    {
        _startPoint = start;
        _endPoint = end;
        _edgeColor = color;
        _usesFreeEnd = end == null;
        _freeEndPosition = start != null ? start.position : Vector3.zero;
        if (_lineRenderer == null)
            return;
        if (edgeMaterial != null)
            _lineRenderer.sharedMaterial = edgeMaterial;
        _lineRenderer.startWidth = width;
        _lineRenderer.endWidth = width;
        ApplyColor();
        Refresh();
    }

    /// <summary>Initializes an edge using the default line width.</summary>
    public void Initialize(Transform start, Transform end, Material edgeMaterial)
    {
        Initialize(start, end, edgeMaterial, DefaultLineWidth);
    }

    private void ApplyColor()
    {
        if (_lineRenderer == null)
            return;

        var propertyBlock = new MaterialPropertyBlock();
        _lineRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, EffectiveColor);
        _lineRenderer.SetPropertyBlock(propertyBlock);
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
        TopologyChanged?.Invoke();
    }

    private void LateUpdate()
    {
        if (_startPoint == null) return;
        var startPos = _startPoint.position;
        var endPos = _endPoint != null ? _endPoint.position : _freeEndPosition;
        if (startPos == _lastStartPos && endPos == _lastEndPos) return;
        _lastStartPos = startPos;
        _lastEndPos = endPos;
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

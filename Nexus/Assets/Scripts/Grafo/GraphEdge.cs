using UnityEngine;

/// <summary>
/// Representa una arista (cable/luz) entre dos nodos en el grafo.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class GraphEdge : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private Transform _startPoint;
    private Transform _endPoint;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        // Configuración por defecto para neón
        _lineRenderer.startWidth = 0.05f;
        _lineRenderer.endWidth = 0.05f;
        _lineRenderer.positionCount = 2;
    }

    public void Initialize(Transform start, Transform end, Material edgeMaterial)
    {
        _startPoint = start;
        _endPoint = end;
        if (edgeMaterial != null)
            _lineRenderer.material = edgeMaterial;
    }

    public void UpdateEndPoint(Vector3 position)
    {
        if (_startPoint != null)
        {
            _lineRenderer.SetPosition(0, _startPoint.position);
            _lineRenderer.SetPosition(1, position);
        }
    }

    private void Update()
    {
        if (_startPoint != null && _endPoint != null)
        {
            _lineRenderer.SetPosition(0, _startPoint.position);
            _lineRenderer.SetPosition(1, _endPoint.position);
        }
    }

    public Transform GetStart() => _startPoint;
    public Transform GetEnd() => _endPoint;
}

using UnityEngine;

/// <summary>
/// Mantiene una arista luminosa actualizada entre dos luces de nodos.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class GraphEdge : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private Transform _startPoint;
    private Transform _endPoint;
    private Vector3 _freeEndPosition;
    private bool _usesFreeEnd;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.numCapVertices = 6;
    }

    /// <summary>
    /// Asigna los extremos y el aspecto de la arista.
    /// </summary>
    public void Initialize(Transform start, Transform end, Material edgeMaterial, float width)
    {
        _startPoint = start;
        _endPoint = end;
        _usesFreeEnd = end == null;
        _freeEndPosition = start != null ? start.position : Vector3.zero;
        _lineRenderer.sharedMaterial = edgeMaterial;
        _lineRenderer.startWidth = width;
        _lineRenderer.endWidth = width;
        Refresh();
    }

    /// <summary>
    /// Inicializa una arista con el ancho predeterminado usado por la interfaz de mapa.
    /// </summary>
    public void Initialize(Transform start, Transform end, Material edgeMaterial)
    {
        Initialize(start, end, edgeMaterial, 0.05f);
    }

    /// <summary>
    /// Actualiza el extremo libre de una arista temporal de interfaz.
    /// </summary>
    public void UpdateEndPoint(Vector3 position)
    {
        _freeEndPosition = position;
        _usesFreeEnd = true;
        if (_startPoint != null)
        {
            _lineRenderer.SetPosition(0, _startPoint.position);
            _lineRenderer.SetPosition(1, position);
        }
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh()
    {
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

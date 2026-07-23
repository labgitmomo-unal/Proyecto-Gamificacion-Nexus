using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

/// <summary>
/// Permite crear aristas (líneas luminiscentes) entre nodos mediante interacción VR.
/// </summary>
public class NodeLinker : MonoBehaviour
{
    public static NodeLinker Instance { get; private set; }
    public Material edgeMaterial;
    public float lineWidth = 0.05f;

    private XRGrabInteractable _currentGrabbedNode;
    private GameObject _activeLine;
    private LineRenderer _currentLineRenderer;

    private void Awake()
    {
        Instance = this;
    }

    public void OnNodeGrabbed(SelectEnterEventArgs args)
    {
        _currentGrabbedNode = args.interactableObject as XRGrabInteractable;
    }

    public void OnNodeReleased(SelectExitEventArgs args)
    {
        _currentGrabbedNode = null;
        if (_activeLine != null)
        {
            // Si no se conectó a nada, destruir la línea temporal
            Destroy(_activeLine);
            _activeLine = null;
        }
    }

    // Método para ser llamado por un botón del controlador mientras se agarra un nodo
    public void StartConnection(XRGrabInteractable node)
    {
        if (node == null) return;

        _activeLine = new GameObject("TemporaryEdge");
        _currentLineRenderer = _activeLine.AddComponent<LineRenderer>();
        _currentLineRenderer.material = edgeMaterial;
        _currentLineRenderer.startWidth = lineWidth;
        _currentLineRenderer.endWidth = lineWidth;
        _currentLineRenderer.positionCount = 2;
        _currentLineRenderer.SetPositions(new Vector3[] { node.transform.position, node.transform.position });
    }

    public void UpdateConnection(Vector3 currentPos)
    {
        if (_currentLineRenderer != null)
        {
            _currentLineRenderer.SetPosition(1, currentPos);
        }
    }

    public void CompleteConnection(GameObject targetNode)
    {
        if (_activeLine == null || targetNode == null) return;

        // Fijar la línea permanentemente
        _currentLineRenderer.SetPosition(1, targetNode.transform.position);
        _activeLine.name = "Edge_" + _currentLineRenderer.GetPosition(0) + "_" + targetNode.name;
        
        // Añadir script para que la línea siga a los nodos si se mueven
        var follower = _activeLine.AddComponent<EdgeFollower>();
        follower.nodeA = _currentGrabbedNode.transform;
        follower.nodeB = targetNode.transform;

        _activeLine = null;
        _currentLineRenderer = null;
    }
}

public class EdgeFollower : MonoBehaviour
{
    public Transform nodeA;
    public Transform nodeB;
    private LineRenderer _lr;

    private void Start()
    {
        _lr = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (nodeA == null || nodeB == null) { Destroy(gameObject); return; }
        _lr.SetPosition(0, nodeA.position);
        _lr.SetPosition(1, nodeB.position);
    }
}

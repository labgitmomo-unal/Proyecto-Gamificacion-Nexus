using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inicializa los nodos 3D bajo un contenedor de grafo cuando la escena entra en ejecución.
/// </summary>
[DisallowMultipleComponent]
public sealed class GraphNodeGraphRuntime : MonoBehaviour
{
    [SerializeField] private bool includeInactiveNodes;

    private void Awake()
    {
        var nodes = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf || includeInactiveNodes)
                nodes.Add(child);
        }

        foreach (var nodeTransform in nodes)
        {
            if (nodeTransform.GetComponent<GraphNode3D>() == null)
                nodeTransform.gameObject.AddComponent<GraphNode3D>();
        }
    }
}

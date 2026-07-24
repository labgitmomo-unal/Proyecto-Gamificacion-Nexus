using UnityEngine;

[DisallowMultipleComponent]
public sealed class GraphNodeGraphRuntime : MonoBehaviour
{
    [SerializeField] private bool includeInactiveNodes;

    private void Awake()
    {
        var nodes = GetComponentsInChildren<GraphNode3D>(includeInactiveNodes);
        foreach (var node in nodes)
            node.Initialize();

        foreach (Transform child in transform)
        {
            if ((!includeInactiveNodes && !child.gameObject.activeSelf) || child.GetComponent<GraphNode3D>() != null)
                continue;
            if (child.GetComponent<Renderer>() != null)
                child.gameObject.AddComponent<GraphNode3D>();
        }
    }
}

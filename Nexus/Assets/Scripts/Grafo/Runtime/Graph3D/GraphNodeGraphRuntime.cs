using UnityEngine;

[DisallowMultipleComponent]
public sealed class GraphNodeGraphRuntime : MonoBehaviour
{
    private const string DefaultPlacementSurfaceName = "plataforma_grafo";

    [SerializeField] private bool includeInactiveNodes;
    [SerializeField] private Collider nodePlacementSurface;

    private void Awake()
    {
        ResolvePlacementSurface();

        var nodes = GetComponentsInChildren<GraphNode3D>(includeInactiveNodes);
        foreach (var node in nodes)
        {
            node.Initialize();
            node.ConfigurePlacementSurface(nodePlacementSurface);
        }

        foreach (Transform child in transform)
        {
            if ((!includeInactiveNodes && !child.gameObject.activeSelf) || child.GetComponent<GraphNode3D>() != null)
                continue;
            if (child.GetComponent<Renderer>() == null)
                continue;

            var node = child.gameObject.AddComponent<GraphNode3D>();
            node.ConfigurePlacementSurface(nodePlacementSurface);
        }
    }

    private void ResolvePlacementSurface()
    {
        if (nodePlacementSurface != null)
            return;

        var surfaceTransform = transform.Find(DefaultPlacementSurfaceName);
        if (surfaceTransform != null)
            nodePlacementSurface = surfaceTransform.GetComponent<Collider>();

        if (nodePlacementSurface == null)
            Debug.LogError($"[{nameof(GraphNodeGraphRuntime)}] No se encontró un Collider en {DefaultPlacementSurfaceName}.", this);
    }
}

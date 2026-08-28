using UnityEngine;

[DisallowMultipleComponent]
public sealed class GraphTrafficRoad : MonoBehaviour
{
    private const int DefaultWhiteMaximumVehicles = 2;
    private const int DefaultYellowMaximumVehicles = 6;
    private const int DefaultOrangeMaximumVehicles = 12;

    [SerializeField] private string roadName;
    [SerializeField] private Transform startIntersection;
    [SerializeField] private Transform endIntersection;
    [SerializeField] private Transform spawnerRoot;
    [SerializeField] private MovementController routeTemplate;
    [SerializeField] private int whiteMaximumVehicles = DefaultWhiteMaximumVehicles;
    [SerializeField] private int yellowMaximumVehicles = DefaultYellowMaximumVehicles;
    [SerializeField] private int orangeMaximumVehicles = DefaultOrangeMaximumVehicles;

    public int ActiveVehicleCount { get; private set; }
    public GraphTrafficColor ExpectedColor { get; private set; } = GraphTrafficColor.White;
    public bool IsConfigured => ValidateConfiguration();
    public Transform StartIntersection => startIntersection;
    public Transform EndIntersection => endIntersection;
    public Transform SpawnerRoot => spawnerRoot;
    public MovementController RouteTemplate => routeTemplate;
    public string RoadName => string.IsNullOrWhiteSpace(roadName) ? name : roadName;

    /// <summary>Refreshes the read-only traffic snapshot and expected traffic color.</summary>
    public void RefreshTrafficSnapshot()
    {
        if (!ValidateConfiguration())
        {
            ActiveVehicleCount = 0;
            ExpectedColor = GraphTrafficColor.White;
            return;
        }

        var trafficManager = TrafficManager.Instance;
        ActiveVehicleCount = trafficManager == null ? 0 : trafficManager.CountActiveVehicles(this);
        ExpectedColor = ClassifyTraffic(ActiveVehicleCount);
    }

    /// <summary>Returns whether a transform is the configured runtime spawner for this road.</summary>
    public bool OwnsSpawner(Transform candidate)
    {
        return candidate != null && spawnerRoot != null && candidate == spawnerRoot;
    }

    private GraphTrafficColor ClassifyTraffic(int activeVehicles)
    {
        if (activeVehicles <= Mathf.Max(whiteMaximumVehicles, 0))
            return GraphTrafficColor.White;
        if (activeVehicles <= Mathf.Max(yellowMaximumVehicles, whiteMaximumVehicles))
            return GraphTrafficColor.Yellow;
        if (activeVehicles <= Mathf.Max(orangeMaximumVehicles, yellowMaximumVehicles))
            return GraphTrafficColor.Orange;
        return GraphTrafficColor.Red;
    }

    private bool ValidateConfiguration()
    {
        if (!isActiveAndEnabled || startIntersection == null || endIntersection == null
            || startIntersection == endIntersection || !startIntersection.gameObject.activeInHierarchy
            || !endIntersection.gameObject.activeInHierarchy || spawnerRoot == null
            || !spawnerRoot.gameObject.activeInHierarchy || routeTemplate == null)
            return false;

        if (routeTemplate.transform.parent != spawnerRoot.parent)
            return false;

        return HasRuntimeSpawner();
    }

    private bool HasRuntimeSpawner()
    {
        var behaviours = spawnerRoot.GetComponents<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            var typeName = behaviour.GetType().Name;
            if (typeName == "DLNK_RandomObjectSpawner" || typeName == "RandomObjectSpawner")
                return true;
        }

        return false;
    }
}

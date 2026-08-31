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
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform despawnPoint;
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
    public Transform SpawnPoint => spawnPoint;
    public Transform DespawnPoint => despawnPoint;
    public string RoadName => string.IsNullOrWhiteSpace(roadName) ? name : roadName;

    /// <summary>Refreshes this road's read-only traffic snapshot from the observer.</summary>
    public void RefreshTrafficSnapshot()
    {
        if (!ValidateConfiguration())
        {
            ActiveVehicleCount = 0;
            ExpectedColor = GraphTrafficColor.White;
            return;
        }

        var telemetryAdapter = FindAnyObjectByType<GraphTrafficTelemetryAdapter>();
        ActiveVehicleCount = telemetryAdapter == null ? 0 : telemetryAdapter.GetActiveVehicleCount(this);
        ExpectedColor = ClassifyTraffic(ActiveVehicleCount);
    }

    /// <summary>Returns whether a transform is this road's configured runtime spawner.</summary>
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
            || routeTemplate == null || spawnPoint == null || despawnPoint == null
            || !spawnerRoot.gameObject.activeInHierarchy
            || !spawnPoint.gameObject.activeInHierarchy || !despawnPoint.gameObject.activeInHierarchy)
            return false;

        if (!spawnerRoot.IsChildOf(transform) || !routeTemplate.transform.IsChildOf(transform)
            || !spawnPoint.IsChildOf(transform) || !despawnPoint.IsChildOf(transform))
            return false;

        return HasRuntimeSpawner();
    }

    private bool HasRuntimeSpawner()
    {
        var behaviours = spawnerRoot.GetComponents<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            if (behaviour != null && behaviour.GetType().Name == "DLNK_RandomObjectSpawner")
                return true;
        }

        return false;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VehicleTelemetryRecord
{
    public GraphTrafficRoad SourceRoad { get; internal set; }
    public Transform SpawnerRoot => SourceRoad != null ? SourceRoad.SpawnerRoot : null;
    public float FirstObservedTime { get; internal set; }
    public float LastObservedTime { get; internal set; }
    public Vector3 LastPosition { get; internal set; }
    public bool IsActive { get; internal set; }
    public float AttributionConfidence { get; internal set; }
}

[DisallowMultipleComponent]
public sealed class GraphTrafficTelemetryAdapter : MonoBehaviour
{
    private const float DefaultRefreshInterval = 0.10f;
    private const float DefaultSpawnObservationRadius = 4f;
    private const float DefaultDespawnObservationRadius = 2.5f;
    private const float DefaultAttributionTimeout = 1.5f;
    private const float MinimumPositiveValue = 0.0001f;
    private const float MinimumDirectionDot = 0.9f;

    [SerializeField] private GraphTrafficRoad[] roads = Array.Empty<GraphTrafficRoad>();
    [SerializeField] private float refreshInterval = DefaultRefreshInterval;
    [SerializeField] private float spawnObservationRadius = DefaultSpawnObservationRadius;
    [SerializeField] private float despawnObservationRadius = DefaultDespawnObservationRadius;
    [SerializeField] private float attributionTimeout = DefaultAttributionTimeout;

    private readonly Dictionary<MovementController, VehicleTelemetryRecord> records
        = new Dictionary<MovementController, VehicleTelemetryRecord>();
    private readonly Dictionary<MovementController, float> pendingAttributions
        = new Dictionary<MovementController, float>();
    private readonly HashSet<int> warnedAmbiguousVehicles = new HashSet<int>();
    private readonly HashSet<GraphTrafficRoad> warnedRoads = new HashSet<GraphTrafficRoad>();
    private readonly HashSet<MovementController> _activeSetBuffer = new HashSet<MovementController>();
    private readonly List<MovementController> _staleBuffer = new List<MovementController>();
    private float refreshTimer;
    private int spawnedVehicleCount;
    private int activeVehicleCount;

    public event Action<int, int> TrafficCountChanged;

    private void Awake()
    {
        ClampConfiguration();
        if (roads == null)
            roads = Array.Empty<GraphTrafficRoad>();
        ValidateRoadList();
    }

    private void Start()
    {
        RefreshSnapshot();
    }

    private void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer < refreshInterval)
            return;

        refreshTimer = 0f;
        RefreshSnapshot();
    }

    /// <summary>Returns the number of currently attributed vehicles for a source road.</summary>
    public int GetActiveVehicleCount(GraphTrafficRoad road)
    {
        if (road == null)
            return 0;

        var count = 0;
        foreach (var pair in records)
        {
            if (pair.Key != null && pair.Value != null && pair.Value.IsActive
                && pair.Value.SourceRoad == road && pair.Key.gameObject.activeInHierarchy)
                count++;
        }

        return count;
    }

    /// <summary>Returns the stable telemetry record for an observed vehicle instance.</summary>
    public bool TryGetTelemetry(MovementController vehicle, out VehicleTelemetryRecord record)
    {
        if (vehicle != null && records.TryGetValue(vehicle, out record))
            return true;

        record = null;
        return false;
    }

    /// <summary>Reconciles active vehicle instances without changing any traffic component.</summary>
    public void RefreshSnapshot()
    {
        ClampConfiguration();
        var activeControllers = TrafficManager.Instance != null
            ? TrafficManager.Instance.ObtenerClones()
            : new List<MovementController>(FindObjectsByType<MovementController>(FindObjectsSortMode.None));
        _activeSetBuffer.Clear();
        foreach (var controller in activeControllers)
        {
            if (controller == null || !controller.gameObject.activeInHierarchy || IsRouteTemplate(controller))
                continue;

            _activeSetBuffer.Add(controller);
            if (records.TryGetValue(controller, out var record))
            {
                if (IsAtDespawnPoint(record.SourceRoad, controller.transform))
                {
                    RemoveRecord(controller);
                    continue;
                }

                record.LastObservedTime = Time.time;
                record.LastPosition = controller.transform.position;
                record.IsActive = true;
                continue;
            }

            TryAttribute(controller);
        }

        _staleBuffer.Clear();
        foreach (var pair in records)
        {
            if (pair.Key == null || !_activeSetBuffer.Contains(pair.Key) || !pair.Key.gameObject.activeInHierarchy)
                _staleBuffer.Add(pair.Key);
        }

        foreach (var vehicle in _staleBuffer)
            RemoveRecord(vehicle);

        _staleBuffer.Clear();
        foreach (var pair in pendingAttributions)
        {
            if (pair.Key == null || !_activeSetBuffer.Contains(pair.Key)
                || Time.time - pair.Value >= attributionTimeout)
                _staleBuffer.Add(pair.Key);
        }

        foreach (var vehicle in _staleBuffer)
            pendingAttributions.Remove(vehicle);

        PublishCounts();
    }

    /// <summary>Resets session spawn counters while retaining stable active-source attribution.</summary>
    public void ResetSessionCounters()
    {
        spawnedVehicleCount = 0;
        PublishCounts(true);
    }

    /// <summary>Returns whether a road is explicitly owned by this observer.</summary>
    public bool CanObserveRoad(GraphTrafficRoad road)
    {
        if (road == null || roads == null)
            return false;

        foreach (var configuredRoad in roads)
        {
            if (configuredRoad == road)
                return true;
        }

        return false;
    }

    private void TryAttribute(MovementController vehicle)
    {
        var matchCount = 0;
        GraphTrafficRoad matchedRoad = null;
        foreach (var road in roads)
        {
            if (!IsRoadObservable(road) || !IsWithinSpawnWindow(road, vehicle.transform)
                || !IsCompatibleWithRoute(road, vehicle))
                continue;

            matchCount++;
            matchedRoad = road;
        }

        if (matchCount != 1)
        {
            if (IsNearAnySpawnPoint(vehicle.transform))
            {
                if (!pendingAttributions.ContainsKey(vehicle))
                    pendingAttributions[vehicle] = Time.time;
                if (warnedAmbiguousVehicles.Add(vehicle.GetInstanceID()))
                    Debug.LogWarning("GraphTrafficTelemetryAdapter no pudo atribuir inequívocamente un vehículo a una fuente.", vehicle);
            }
            return;
        }

        pendingAttributions.Remove(vehicle);
        records[vehicle] = new VehicleTelemetryRecord
        {
            SourceRoad = matchedRoad,
            FirstObservedTime = Time.time,
            LastObservedTime = Time.time,
            LastPosition = vehicle.transform.position,
            IsActive = true,
            AttributionConfidence = 1f
        };
        spawnedVehicleCount++;
    }

    private bool IsRoadObservable(GraphTrafficRoad road)
    {
        if (road == null || !road.IsConfigured)
            return false;
        if (!CanObserveRoad(road))
            return false;
        if (warnedRoads.Contains(road))
            return true;

        warnedRoads.Add(road);
        return true;
    }

    private bool IsWithinSpawnWindow(GraphTrafficRoad road, Transform vehicle)
    {
        return road.SpawnPoint != null
            && Vector3.Distance(vehicle.position, road.SpawnPoint.position) <= spawnObservationRadius;
    }

    private bool IsNearAnySpawnPoint(Transform vehicle)
    {
        foreach (var road in roads)
        {
            if (road != null && road.SpawnPoint != null
                && Vector3.Distance(vehicle.position, road.SpawnPoint.position) <= spawnObservationRadius)
                return true;
        }

        return false;
    }

    private bool IsCompatibleWithRoute(GraphTrafficRoad road, MovementController vehicle)
    {
        if (road.RouteTemplate == null || vehicle == null)
            return false;

        var templateVelocity = road.RouteTemplate.initialVelocity;
        var vehicleVelocity = vehicle.initialVelocity;
        if (templateVelocity.sqrMagnitude < MinimumPositiveValue
            || vehicleVelocity.sqrMagnitude < MinimumPositiveValue)
            return false;

        return Vector3.Dot(templateVelocity.normalized, vehicleVelocity.normalized) >= MinimumDirectionDot;
    }

    private bool IsAtDespawnPoint(GraphTrafficRoad road, Transform vehicle)
    {
        return road != null && road.DespawnPoint != null
            && Vector3.Distance(vehicle.position, road.DespawnPoint.position) <= despawnObservationRadius;
    }

    private bool IsRouteTemplate(MovementController candidate)
    {
        foreach (var road in roads)
        {
            if (road != null && road.RouteTemplate == candidate)
                return true;
        }

        return false;
    }

    private void RemoveRecord(MovementController vehicle)
    {
        if (vehicle == null || !records.Remove(vehicle))
            return;

        activeVehicleCount = GetTotalActiveCount();
    }

    private int GetTotalActiveCount()
    {
        var count = 0;
        foreach (var pair in records)
        {
            if (pair.Key != null && pair.Value != null && pair.Value.IsActive
                && pair.Key.gameObject.activeInHierarchy)
                count++;
        }

        return count;
    }

    private void PublishCounts(bool force = false)
    {
        var newActiveCount = GetTotalActiveCount();
        if (!force && newActiveCount == activeVehicleCount)
            return;

        activeVehicleCount = newActiveCount;
        TrafficCountChanged?.Invoke(activeVehicleCount, spawnedVehicleCount);
    }

    private void ValidateRoadList()
    {
        var seenSpawners = new HashSet<Transform>();
        var seenSpawnPoints = new HashSet<Transform>();
        foreach (var road in roads)
        {
            if (road == null)
                continue;
            if (road.SpawnerRoot != null && !seenSpawners.Add(road.SpawnerRoot))
                Debug.LogWarning($"GraphTrafficTelemetryAdapter detectó un spawner duplicado en '{road.RoadName}'.", road);
            if (road.SpawnPoint != null && !seenSpawnPoints.Add(road.SpawnPoint))
                Debug.LogWarning($"GraphTrafficTelemetryAdapter detectó un punto de aparición duplicado en '{road.RoadName}'.", road);
            if (road.DespawnPoint == null || road.SpawnerRoot == null || road.RouteTemplate == null)
                Debug.LogWarning($"GraphTrafficTelemetryAdapter no puede observar la carretera incompleta '{road.RoadName}'.", road);
        }
    }

    private void ClampConfiguration()
    {
        refreshInterval = Mathf.Max(refreshInterval, MinimumPositiveValue);
        spawnObservationRadius = Mathf.Max(spawnObservationRadius, MinimumPositiveValue);
        despawnObservationRadius = Mathf.Max(despawnObservationRadius, MinimumPositiveValue);
        attributionTimeout = Mathf.Max(attributionTimeout, refreshInterval);
    }
}

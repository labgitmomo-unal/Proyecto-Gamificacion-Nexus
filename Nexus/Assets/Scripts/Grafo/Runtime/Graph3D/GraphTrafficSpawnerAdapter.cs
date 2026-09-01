using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GraphTrafficSpawnerAdapter : MonoBehaviour
{
    private const float SpawnPositionTolerance = 2.5f;
    private const float MinimumDirectionDot = 0.9f;
    private const float DefaultRefreshInterval = 0.1f;

    [SerializeField] private GraphTrafficRoad road;
    [SerializeField] private Transform runtimeSpawner;
    [SerializeField] private MovementController routeTemplate;
    [SerializeField] private float refreshInterval = DefaultRefreshInterval;

    private readonly HashSet<MovementController> observedVehicles = new HashSet<MovementController>();
    private readonly List<MovementController> _staleBuffer = new List<MovementController>();
    private float refreshTimer;
    private bool? _hasRuntimeSpawnerCache;
    private float _hasRuntimeSpawnerCheckTime;

    private void Awake()
    {
        if (road == null)
            road = GetComponent<GraphTrafficRoad>();
        if (runtimeSpawner == null && road != null)
            runtimeSpawner = road.SpawnerRoot;
        if (routeTemplate == null && road != null)
            routeTemplate = road.RouteTemplate;
        refreshInterval = Mathf.Max(refreshInterval, 0.0001f);
    }

    private void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer < refreshInterval)
            return;

        refreshTimer = 0f;
        ReconcileVehicles();
    }

    private void OnDestroy()
    {
        var trafficManager = TrafficManager.Instance;
        foreach (var vehicle in observedVehicles)
        {
            if (vehicle != null)
                trafficManager?.DesregistrarClon(vehicle);
        }
        observedVehicles.Clear();
    }

    private void ReconcileVehicles()
    {
        if (road == null || runtimeSpawner == null || routeTemplate == null || !road.IsConfigured)
            return;
        if (!HasRuntimeSpawnerCached())
            return;

        var trafficManager = TrafficManager.Instance;
        if (trafficManager == null)
            return;

        var registeredVehicles = trafficManager.ObtenerClones();
        _staleBuffer.Clear();
        foreach (var vehicle in observedVehicles)
        {
            if (vehicle == null || !vehicle.gameObject.activeInHierarchy)
            {
                if (vehicle != null)
                    trafficManager.DesregistrarClon(vehicle);
                _staleBuffer.Add(vehicle);
            }
        }

        foreach (var vehicle in _staleBuffer)
            observedVehicles.Remove(vehicle);
    }

    private bool IsPotentialVehicle(MovementController candidate)
    {
        if (candidate == null || !candidate.gameObject.activeInHierarchy)
            return false;

        if (Vector3.Distance(candidate.transform.position, runtimeSpawner.position) > SpawnPositionTolerance)
            return false;

        var templateVelocity = routeTemplate.initialVelocity;
        var candidateVelocity = candidate.initialVelocity;
        if (templateVelocity.sqrMagnitude < 0.0001f || candidateVelocity.sqrMagnitude < 0.0001f)
            return false;

        return Vector3.Dot(templateVelocity.normalized, candidateVelocity.normalized) >= MinimumDirectionDot;
    }

    private bool HasRuntimeSpawnerCached()
    {
        if (_hasRuntimeSpawnerCache.HasValue && Time.time - _hasRuntimeSpawnerCheckTime < 2f)
            return _hasRuntimeSpawnerCache.Value;

        _hasRuntimeSpawnerCheckTime = Time.time;
        _hasRuntimeSpawnerCache = HasRuntimeSpawner();
        return _hasRuntimeSpawnerCache.Value;
    }

    private bool HasRuntimeSpawner()
    {
        if (runtimeSpawner == null)
            return false;

        var behaviours = runtimeSpawner.GetComponents<MonoBehaviour>();
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

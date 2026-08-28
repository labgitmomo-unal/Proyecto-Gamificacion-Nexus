using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Publishes read-only traffic counts from the vehicles registered by TrafficManager.</summary>
public sealed class TrafficTelemetryReader : MonoBehaviour
{
    private const float DefaultRefreshInterval = 0.10f;
    private const float MinimumPositiveValue = 0.0001f;

    [SerializeField] private float refreshInterval = DefaultRefreshInterval;

    private readonly HashSet<MovementController> activeVehicles = new HashSet<MovementController>();
    private TrafficManager subscribedManager;
    private float refreshTimer;
    private int activeVehicleCount;
    private int spawnedVehicleCount;

    public int ActiveVehicleCount => activeVehicleCount;
    public int SpawnedVehicleCount => spawnedVehicleCount;
    public event Action<int, int> TrafficCountChanged;

    private void Start()
    {
        ClampConfiguration();
        TrySubscribeToManager();
        RefreshSnapshot();
    }

    private void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer < refreshInterval)
            return;

        refreshTimer = 0f;
        TrySubscribeToManager();
        RefreshSnapshot();
    }

    private void OnDestroy()
    {
        UnsubscribeFromManager();
    }

    /// <summary>Resets the accumulated spawn count while retaining the current active snapshot.</summary>
    public void ResetSessionCounters()
    {
        spawnedVehicleCount = 0;
        RefreshSnapshot();
        PublishCounts(true);
    }

    /// <summary>Reconciles active vehicles with TrafficManager without changing traffic behaviour.</summary>
    public void RefreshSnapshot()
    {
        ClampConfiguration();
        var manager = TrafficManager.Instance;
        if (manager == null)
        {
            if (activeVehicles.Count == 0 && activeVehicleCount == 0)
                return;

            activeVehicles.Clear();
            PublishCounts(false);
            return;
        }

        var snapshot = new HashSet<MovementController>();
        var registeredVehicles = manager.ObtenerClones();
        if (registeredVehicles != null)
        {
            foreach (var vehicle in registeredVehicles)
            {
                if (vehicle != null && vehicle.gameObject.activeInHierarchy)
                    snapshot.Add(vehicle);
            }
        }

        activeVehicles.Clear();
        foreach (var vehicle in snapshot)
            activeVehicles.Add(vehicle);

        activeVehicleCount = activeVehicles.Count;
        PublishCounts(false);
    }

    private void TrySubscribeToManager()
    {
        var manager = TrafficManager.Instance;
        if (manager == subscribedManager)
            return;

        UnsubscribeFromManager();
        if (manager == null)
            return;

        subscribedManager = manager;
        subscribedManager.VehicleRegistered += HandleVehicleRegistered;
        subscribedManager.VehicleUnregistered += HandleVehicleUnregistered;
    }

    private void UnsubscribeFromManager()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.VehicleRegistered -= HandleVehicleRegistered;
        subscribedManager.VehicleUnregistered -= HandleVehicleUnregistered;
        subscribedManager = null;
    }

    private void HandleVehicleRegistered(MovementController vehicle)
    {
        spawnedVehicleCount++;
        if (vehicle != null && vehicle.gameObject.activeInHierarchy)
            activeVehicles.Add(vehicle);
        activeVehicleCount = activeVehicles.Count;
        PublishCounts(true);
    }

    private void HandleVehicleUnregistered(MovementController vehicle)
    {
        if (vehicle != null)
            activeVehicles.Remove(vehicle);
        PublishCounts(true);
    }

    private void PublishCounts(bool force)
    {
        var currentActiveCount = activeVehicles.Count;
        if (!force && currentActiveCount == activeVehicleCount)
            return;

        activeVehicleCount = currentActiveCount;
        TrafficCountChanged?.Invoke(activeVehicleCount, spawnedVehicleCount);
    }

    private void ClampConfiguration()
    {
        refreshInterval = Mathf.Max(refreshInterval, MinimumPositiveValue);
    }
}

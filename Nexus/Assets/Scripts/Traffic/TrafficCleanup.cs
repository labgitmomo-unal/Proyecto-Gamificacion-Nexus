using UnityEngine;
using System.Collections.Generic;

public class TrafficCleanup : MonoBehaviour
{
    public static TrafficCleanup Instance { get; private set; }

    public int maxTrafficCars = 80;
    public float maxDistanceFromCamera = 300f;
    public float minSpawnTimeBeforeCleanup = 5f; // Minimum time (seconds) a vehicle must exist before it can be cleaned up

    private List<MovementController> toRemove = new List<MovementController>();
    private RandomObjectSpawner[] spawners;
    private bool authMode;
    // Track spawn time for each cloned vehicle
    private Dictionary<MovementController, float> spawnTimes = new Dictionary<MovementController, float>();

    // Cached comparison delegate to avoid GC allocations in Sort
    private static Vector3 _sortCamPos;
    private static readonly System.Comparison<MovementController> _distanceCompare = (a, b) =>
    {
        if (a == null || b == null) return 0;
        float da = (a.transform.position - _sortCamPos).sqrMagnitude;
        float db = (b.transform.position - _sortCamPos).sqrMagnitude;
        return db.CompareTo(da);
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        spawners = FindObjectsByType<RandomObjectSpawner>(FindObjectsSortMode.None);
        InvokeRepeating(nameof(Cleanup), 2f, 0.5f);
    }

    public void SetAuthMode(bool active)
    {
        authMode = active;
        if (active)
        {
            maxTrafficCars = 40;
            maxDistanceFromCamera = 200f;
        }
    }

    // Register a vehicle's spawn time
    public void RegisterSpawnTime(MovementController mc)
    {
        if (mc != null && !spawnTimes.ContainsKey(mc))
            spawnTimes[mc] = Time.time;
    }

    // Called when a vehicle is despawned/recycled
    public void UnregisterSpawnTime(MovementController mc)
    {
        spawnTimes.Remove(mc);
    }

    public void Cleanup()
    {
        if (TrafficManager.Instance == null) return;
        if (authMode) return; // Skip cleanup in auth mode

        var clones = TrafficManager.Instance.ObtenerClones();
        if (clones == null) return;

        Camera cam = Camera.main;
        Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
        float maxDistSqr = maxDistanceFromCamera * maxDistanceFromCamera;

        toRemove.Clear();

        for (int i = 0; i < clones.Count; i++)
        {
            MovementController mc = clones[i];
            if (mc == null) continue;

            // Skip cleanup for vehicles that haven't been alive long enough
            if (spawnTimes.TryGetValue(mc, out float spawnTime))
            {
                if (Time.time - spawnTime < minSpawnTimeBeforeCleanup)
                    continue; // Vehicle too new, skip cleanup
            }

            float distSqr = cam != null
                ? (mc.transform.position - camPos).sqrMagnitude
                : 0f;

            if (distSqr > maxDistSqr || mc.transform.position.y < -100f)
                toRemove.Add(mc);
            // If we skipped due to min time, we don't add to remove
        }

        int exceso = clones.Count - maxTrafficCars;
        if (exceso > 0)
        {
            // Sort by distance (farthest first) using cached delegate
            _sortCamPos = camPos;
            clones.Sort(_distanceCompare);
            for (int i = 0; i < clones.Count && toRemove.Count < exceso; i++)
            {
                MovementController mc = clones[i];
                if (mc == null) continue;
                // Only remove if it's past the minimum spawn time
                if (spawnTimes.TryGetValue(mc, out float spawnTime))
                {
                    if (Time.time - spawnTime >= minSpawnTimeBeforeCleanup)
                        toRemove.Add(mc);
                }
                else
                {
                    // No spawn time tracked, allow cleanup
                    toRemove.Add(mc);
                }
            }
        }

        foreach (var mc in toRemove)
        {
            if (mc == null) continue;
            GameObject obj = mc.gameObject;

            bool returned = false;
            foreach (var s in spawners)
            {
                if (s != null && s.usePooling)
                {
                    s.ReturnToPool(obj);
                    returned = true;
                    break;
                }
            }
            if (!returned)
                Destroy(obj);
        }
    }
}

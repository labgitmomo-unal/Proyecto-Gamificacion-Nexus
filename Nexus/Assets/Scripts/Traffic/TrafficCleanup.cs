using UnityEngine;
using System.Collections.Generic;

public class TrafficCleanup : MonoBehaviour
{
    public static TrafficCleanup Instance { get; private set; }

    public int maxTrafficCars = 80;
    public float maxDistanceFromCamera = 300f;

    private List<MovementController> toRemove = new List<MovementController>();
    private RandomObjectSpawner[] spawners;
    private bool authMode;

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

    void Cleanup()
    {
        if (TrafficManager.Instance == null) return;

        var clones = TrafficManager.Instance.ObtenerClones();
        if (clones == null) return;

        Camera cam = Camera.main;

        toRemove.Clear();

        for (int i = 0; i < clones.Count; i++)
        {
            MovementController mc = clones[i];
            if (mc == null) continue;

            float dist = cam != null
                ? Vector3.Distance(cam.transform.position, mc.transform.position)
                : 0f;

            if (dist > maxDistanceFromCamera || mc.transform.position.y < -100f)
                toRemove.Add(mc);
        }

        int exceso = clones.Count - maxTrafficCars;
        if (exceso > 0)
        {
            clones.Sort((a, b) =>
            {
                if (a == null || b == null) return 0;
                float da = cam != null ? Vector3.Distance(cam.transform.position, a.transform.position) : 0f;
                float db = cam != null ? Vector3.Distance(cam.transform.position, b.transform.position) : 0f;
                return db.CompareTo(da);
            });
            for (int i = 0; i < clones.Count && toRemove.Count < exceso; i++)
            {
                if (clones[i] != null && !toRemove.Contains(clones[i]))
                    toRemove.Add(clones[i]);
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

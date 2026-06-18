using UnityEngine;
using System.Collections.Generic;

public class TrafficCleanup : MonoBehaviour
{
    public int maxTrafficCars = 80;
    public float maxDistanceFromCamera = 300f;

    private List<MovementController> toRemove = new List<MovementController>();
    private RandomObjectSpawner[] spawners;

    void Start()
    {
        spawners = FindObjectsByType<RandomObjectSpawner>(FindObjectsSortMode.None);
        InvokeRepeating(nameof(Cleanup), 1f, 0.5f);
    }

    void Cleanup()
    {
        if (TrafficManager.Instance == null) return;

        var clones = TrafficManager.Instance.ObtenerClones();
        if (clones == null) return;

        int exceso = clones.Count - maxTrafficCars;
        if (exceso <= 0) return;

        Camera cam = Camera.main;
        toRemove.Clear();

        for (int i = 0; i < clones.Count && toRemove.Count < exceso + 5; i++)
        {
            MovementController mc = clones[i];
            if (mc == null) continue;

            float dist = cam != null
                ? Vector3.Distance(cam.transform.position, mc.transform.position)
                : 0f;

            if (dist > maxDistanceFromCamera || mc.transform.position.y < -100f)
                toRemove.Add(mc);
        }

        // Si aun hay exceso, remover los mas lejanos
        if (toRemove.Count < exceso)
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

            // Try to return to pool first
            bool returned = false;
            if (spawners != null)
            {
                foreach (var s in spawners)
                {
                    if (s != null && s.usePooling)
                    {
                        s.ReturnToPool(obj);
                        returned = true;
                        break;
                    }
                }
            }
            if (!returned)
                Destroy(obj);
        }
    }
}

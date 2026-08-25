using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RandomObjectSpawner : MonoBehaviour
{
    public List<GameObject> objectList = new List<GameObject>();
    public float minSpawnInterval = 0.5f;
    public float maxSpawnInterval = 4f;
    public MonoBehaviour[] optionalScripts;
    public Vector3 minScale = Vector3.one;
    public Vector3 maxScale = Vector3.one;
    public float keepAspectRatio = 1f;
    public bool usePooling = false;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private Dictionary<GameObject, MovementController> cloneToTemplate = new Dictionary<GameObject, MovementController>();
    private bool _spawningEnabled = true;
    private float _intervalOverrideMin = -1f;
    private float _intervalOverrideMax = -1f;

    // Public properties to access override values
    public float IntervalOverrideMin => _intervalOverrideMin;
    public float IntervalOverrideMax => _intervalOverrideMax;

    private void Awake()
    {
        ConfigureFixedInterval();
    }

    private void ConfigureFixedInterval()
    {
        string name = gameObject.name;
        
        if (name.Contains("Car Line Spawner (2)"))
        {
            minSpawnInterval = 0.3f;
            maxSpawnInterval = 1.6f;
        }
        else if (name.Contains("Spawner (0)") || name.Contains("Spawner (4)") || name.Contains("Spawner (6)"))
        {
            minSpawnInterval = 0.5f;
            maxSpawnInterval = 2.0f;
        }
        else if (name.Contains("Spawner (1)") || name.Contains("Spawner (3)") || name.Contains("Spawner (5)") || name.Contains("Spawner (8)") || name.Contains("Spawner (10)"))
        {
            minSpawnInterval = 0.1f;
            maxSpawnInterval = 0.5f;
        }
        else if (name.Contains("Spawner (2)") && !name.Contains("Car Line"))
        {
            minSpawnInterval = 0.1f;
            maxSpawnInterval = 0.4f;
        }
    }

    void OnEnable()
    {
        StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    public void SetSpawningEnabled(bool enabled)
    {
        _spawningEnabled = enabled;
    }

    public void SetSpawnIntervalOverride(float min, float max)
    {
        _intervalOverrideMin = min;
        _intervalOverrideMax = max;
    }

    public void ClearSpawnIntervalOverride()
    {
        _intervalOverrideMin = -1f;
        _intervalOverrideMax = -1f;
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            while (!_spawningEnabled) yield return null;

            float min = _intervalOverrideMin >= 0f ? _intervalOverrideMin : minSpawnInterval;
            float max = _intervalOverrideMax >= 0f ? _intervalOverrideMax : maxSpawnInterval;
            float delay = Random.Range(min, max);
            yield return new WaitForSeconds(delay);
            SpawnOne();
        }
    }

    void SpawnOne()
    {
        if (objectList == null || objectList.Count == 0) return;

        GameObject prefab = objectList[Random.Range(0, objectList.Count)];
        if (prefab == null) return;

        GameObject clone;
        if (usePooling && pool.Count > 0)
        {
            clone = pool.Dequeue();
            clone.SetActive(true);
            clone.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }
        else
        {
            clone = Instantiate(prefab, transform.position, transform.rotation);
        }

        ApplyScale(clone.transform);
        CopyOptionalScripts(clone);

        MovementController mc = clone.GetComponent<MovementController>();
        if (mc != null)
        {
            if (TrafficManager.Instance != null)
            {
                TrafficManager.Instance.RegistrarClon(mc);
                TrafficManager.Instance.RegisterSpawnTime(mc);
            }
        }
    }

    void ApplyScale(Transform t)
    {
        if (keepAspectRatio > 0.01f)
        {
            float s = Random.Range(minScale.x, maxScale.x);
            t.localScale = new Vector3(s, s, s);
        }
        else
        {
            float sx = Random.Range(minScale.x, maxScale.x);
            float sy = Random.Range(minScale.y, maxScale.y);
            float sz = Random.Range(minScale.z, maxScale.z);
            t.localScale = new Vector3(sx, sy, sz);
        }
    }

    void CopyOptionalScripts(GameObject target)
    {
        if (optionalScripts == null) return;
        foreach (var template in optionalScripts)
        {
            if (template == null) continue;
            if (template is not MovementController mcTemplate) continue;

            System.Type t = template.GetType();
            Component existing = target.GetComponent(t);
            if (existing == null)
                existing = target.AddComponent(t);

            if (existing is MovementController mcTarget && mcTarget != null)
            {
                mcTarget.useVelocity = mcTemplate.useVelocity;
                mcTarget.useRotation = mcTemplate.useRotation;
                mcTarget.useAcceleration = mcTemplate.useAcceleration;
                mcTarget.initialVelocity = mcTemplate.initialVelocity;
                mcTarget.acceleration = mcTemplate.acceleration;
                mcTarget.rotationSpeed = mcTemplate.rotationSpeed;
                mcTarget.offsetRange = mcTemplate.offsetRange;
            }
        }
    }

    public void ReturnToPool(GameObject obj)
    {
        MovementController mc = obj.GetComponent<MovementController>();
        if (mc != null && TrafficManager.Instance != null)
            TrafficManager.Instance.DesregistrarClon(mc);

        if (usePooling)
        {
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
        else
        {
            Destroy(obj);
        }
    }
}

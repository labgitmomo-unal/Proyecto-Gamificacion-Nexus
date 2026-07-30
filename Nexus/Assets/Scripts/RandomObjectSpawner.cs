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

    void OnEnable()
    {
        StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            float delay = Random.Range(minSpawnInterval, maxSpawnInterval);
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
                TrafficManager.Instance.RegistrarClon(mc);
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
            System.Type t = template.GetType();
            Component existing = target.GetComponent(t);
            if (existing == null)
                existing = target.AddComponent(t);
            if (existing is MovementController mcTemplate)
            {
                MovementController mcTarget = existing as MovementController;
                if (mcTarget != null)
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

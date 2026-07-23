using UnityEngine;
using System.Collections.Generic;

public class BridgeControlManager : MonoBehaviour
{
    [Header("Plantilla del Spawner (1) - arrastrar Script Move")]
    public MovementController spawnerTemplate;

    public bool IsComplete { get; private set; }
    public bool IsActive { get; private set; }
    public int ReleaseCount { get; private set; }

    public static event System.Action OnAllZonesComplete;

    private float _releaseTimer = 0f;
    private List<BridgeCarFreezer> _freezers = new List<BridgeCarFreezer>();
    void Start()
    {
        ReleaseCount = 0;
        IsComplete = false;
        IsActive = false;
        if (spawnerTemplate == null)
        {
            Debug.LogError("[BridgeControl] No hay spawnerTemplate asignado.");
            return;
        }
        TrafficManager.Instance.RegistrarPlantilla(spawnerTemplate);
    }

    private List<MovementController> ObtenerTodosLosMovementControllers()
    {
        return new List<MovementController>(
            FindObjectsByType<MovementController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    private void CongelarNuevos()
    {
        foreach (var mc in ObtenerTodosLosMovementControllers())
        {
            if (mc == null || mc == spawnerTemplate) continue;
            if (mc.GetComponent<BridgeCarFreezer>() != null) continue;
            var freezer = mc.gameObject.AddComponent<BridgeCarFreezer>();
            freezer.manager = this;
            freezer.originalVelocity = mc.initialVelocity;
            freezer.UpdateFreezePosition();
            _freezers.Add(freezer);
        }
    }

    public void FreezeBridge()
    {
        if (spawnerTemplate == null) return;

        gameObject.SetActive(true);
        IsActive = true;
        IsComplete = false;
        ReleaseCount = 0;
        _releaseTimer = 0f;
        _freezers.Clear();

        var tc = FindFirstObjectByType<TrafficCleanup>();
        if (tc != null) tc.CancelInvoke("Cleanup");

        foreach (var mc in ObtenerTodosLosMovementControllers())
        {
            if (mc == null || mc == spawnerTemplate) continue;
            var freezer = mc.GetComponent<BridgeCarFreezer>();
            if (freezer == null)
                freezer = mc.gameObject.AddComponent<BridgeCarFreezer>();
            freezer.manager = this;
            freezer.originalVelocity = mc.initialVelocity;
            freezer.UpdateFreezePosition();
            _freezers.Add(freezer);
        }

        Debug.Log($"[BridgeControl] FreezeBridge: {_freezers.Count} autos congelados, Cleanup detenido.", this);
    }

    void Update()
    {
        if (!IsActive || IsComplete) return;

        if (_releaseTimer > 0f)
        {
            _releaseTimer -= Time.deltaTime;

            if (Time.frameCount % 3 == 0 && spawnerTemplate.initialVelocity.sqrMagnitude > 0.1f)
            {
                foreach (var mc in ObtenerTodosLosMovementControllers())
                {
                    if (mc == null || mc == spawnerTemplate) continue;
                    if (mc.initialVelocity.sqrMagnitude < 0.1f)
                        mc.initialVelocity = spawnerTemplate.initialVelocity;
                }
            }

            if (_releaseTimer <= 0f)
            {
                foreach (var f in _freezers)
                {
                    if (f != null && !f.IsFrozen)
                        f.Refreeze();
                }
            }
        }
        else
        {
            CongelarNuevos();
        }
    }

    public void ReleaseStep()
    {
        if (!IsActive || IsComplete || spawnerTemplate == null) return;

        ReleaseCount++;
        Debug.Log($"[BridgeControl] ReleaseStep {ReleaseCount}/4");

        if (ReleaseCount < 4)
        {
            foreach (var f in _freezers)
            {
                if (f != null && f.IsFrozen)
                    f.TemporalRelease();
            }

            if (spawnerTemplate.initialVelocity.sqrMagnitude > 0.1f)
            {
                foreach (var mc in ObtenerTodosLosMovementControllers())
                {
                    if (mc == null || mc == spawnerTemplate) continue;
                    if (mc.initialVelocity.sqrMagnitude < 0.1f)
                        mc.initialVelocity = spawnerTemplate.initialVelocity;
                }
            }

            _releaseTimer = 8f;
        }
        else
        {
            CompleteChallenge();
        }
    }

    private void CompleteChallenge()
    {
        _releaseTimer = 0f;
        IsComplete = true;
        IsActive = false;

        var tc = FindFirstObjectByType<TrafficCleanup>();
        if (tc != null)
        {
            tc.CancelInvoke("Cleanup");
            tc.InvokeRepeating("Cleanup", 2f, 0.5f);
        }

        foreach (var f in _freezers)
        {
            if (f == null) continue;
            if (f.IsFrozen) f.Release();
            Destroy(f);
        }
        _freezers.Clear();

        Debug.Log("[BridgeControl] CompleteChallenge: tráfico normal.", this);
        OnAllZonesComplete?.Invoke();
    }

    public void Reiniciar()
    {
        _freezers.Clear();
        _releaseTimer = 0f;
        ReleaseCount = 0;
        IsComplete = false;
        IsActive = false;
    }

    public void OnFreezerDestroyed(BridgeCarFreezer freezer)
    {
        _freezers.Remove(freezer);
    }
}

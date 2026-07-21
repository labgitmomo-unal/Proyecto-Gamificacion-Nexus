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

    private List<RandomObjectSpawner> _allSpawners = new List<RandomObjectSpawner>();
    private List<Transform> _releasedCars = new List<Transform>();
    private float _releaseTimer = 0f;
    private const float RELEASE_SPEED = 25f;
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
        BuscarTodosLosSpawners();
    }

    private void BuscarTodosLosSpawners()
    {
        _allSpawners.Clear();
        var all = FindObjectsByType<RandomObjectSpawner>(FindObjectsSortMode.None);
        foreach (var s in all)
            if (s != null) _allSpawners.Add(s);
        Debug.Log($"[BridgeControl] {_allSpawners.Count} spawners encontrados.");
    }

    private void DesactivarTodosLosSpawners()
    {
        foreach (var s in _allSpawners)
            if (s != null) s.enabled = false;
    }

    private void ActivarTodosLosSpawners()
    {
        foreach (var s in _allSpawners)
            if (s != null) s.enabled = true;
    }

    private List<MovementController> ObtenerTodosLosMovementControllers()
    {
        return new List<MovementController>(
            FindObjectsByType<MovementController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    public void FreezeBridge()
    {
        if (spawnerTemplate == null) return;

        gameObject.SetActive(true);
        IsActive = true;
        IsComplete = false;
        ReleaseCount = 0;
        _releaseTimer = 0f;
        _releasedCars.Clear();
        _freezers.Clear();

        BuscarTodosLosSpawners();
        DesactivarTodosLosSpawners();

        Vector3 plantillaVel = TrafficManager.Instance.GetBaseVelocityForPlantilla(spawnerTemplate);

        int contador = 0;
        foreach (var mc in ObtenerTodosLosMovementControllers())
        {
            if (mc == null) continue;
            var freezer = mc.GetComponent<BridgeCarFreezer>();
            if (freezer == null)
                freezer = mc.gameObject.AddComponent<BridgeCarFreezer>();
            freezer.manager = this;
            freezer.UpdateFreezePosition();
            if (mc.initialVelocity.sqrMagnitude > 0.001f)
                freezer.movementDirection = -mc.initialVelocity.normalized;
            else if (plantillaVel.sqrMagnitude > 0.001f)
                freezer.movementDirection = -plantillaVel.normalized;
            _freezers.Add(freezer);
            contador++;
        }

        TrafficManager.Instance.SetMultiplicadorPorPlantilla(spawnerTemplate, 0f);

        Debug.Log($"[BridgeControl] FreezeBridge: {contador} autos con BridgeCarFreezer.", this);
    }

    void Update()
    {
        if (!IsActive || IsComplete) return;

        if (_releaseTimer > 0f)
        {
            _releaseTimer -= Time.deltaTime;

            for (int i = _releasedCars.Count - 1; i >= 0; i--)
            {
                var t = _releasedCars[i];
                if (t == null) { _releasedCars.RemoveAt(i); continue; }
                var dir = t.GetComponent<BridgeCarFreezer>();
                t.position += (dir != null ? dir.movementDirection : t.forward) * RELEASE_SPEED * Time.deltaTime;
            }

            if (_releaseTimer <= 0f)
                RefreezarTodos();
        }

        if (Time.frameCount % 10 == 0)
        {
            DesactivarTodosLosSpawners();
            Vector3 plantillaDir = -TrafficManager.Instance.GetBaseVelocityForPlantilla(spawnerTemplate).normalized;
            foreach (var mc in ObtenerTodosLosMovementControllers())
            {
                if (mc == null) continue;
                if (mc.GetComponent<BridgeCarFreezer>() != null) continue;
                var freezer = mc.gameObject.AddComponent<BridgeCarFreezer>();
                freezer.manager = this;
                freezer.movementDirection = plantillaDir;
                freezer.UpdateFreezePosition();
                _freezers.Add(freezer);
                Debug.Log($"[BridgeControl] Catch-up: nuevo freezer en {mc.name}");
            }
        }
    }

    private void RefreezarTodos()
    {
        Debug.Log($"[BridgeControl] Refreeze: {_releasedCars.Count} autos.");
        foreach (var t in _releasedCars)
        {
            if (t == null) continue;
            var freezer = t.GetComponent<BridgeCarFreezer>();
            if (freezer != null)
                freezer.Refreeze();
        }
        _releasedCars.Clear();
    }

    public void ReleaseStep()
    {
        if (!IsActive || IsComplete || spawnerTemplate == null) return;

        ReleaseCount++;
        Debug.Log($"[BridgeControl] ReleaseStep {ReleaseCount}/4");

        if (ReleaseCount < 4)
        {
            _releasedCars.Clear();
            int liberados = 0;
            foreach (var f in _freezers)
            {
                if (f == null || !f.IsFrozen) continue;
                f.TemporalRelease();
                _releasedCars.Add(f.transform);
                liberados++;
            }
            _releaseTimer = 8f;
            Debug.Log($"[BridgeControl] {liberados} autos liberados por 8s (freezers totales: {_freezers.Count}).");
        }
        else
        {
            CompleteChallenge();
        }
    }

    private void CompleteChallenge()
    {
        _releasedCars.Clear();
        _releaseTimer = 0f;

        IsComplete = true;
        IsActive = false;

        ActivarTodosLosSpawners();
        TrafficManager.Instance.SetMultiplicadorPorPlantilla(spawnerTemplate, 1f);

        if (spawnerTemplate != null)
            spawnerTemplate.initialVelocity = TrafficManager.Instance.GetBaseVelocityForPlantilla(spawnerTemplate);

        int liberados = 0;
        foreach (var f in _freezers)
        {
            if (f == null || !f.IsFrozen) continue;
            f.Release();
            liberados++;
        }
        Debug.Log($"[BridgeControl] CompleteChallenge: {liberados} autos liberados permanentemente.", this);
        OnAllZonesComplete?.Invoke();
    }

    public void Reiniciar()
    {
        _releasedCars.Clear();
        _freezers.Clear();
        _releaseTimer = 0f;

        ActivarTodosLosSpawners();
        TrafficManager.Instance.SetMultiplicadorPorPlantilla(spawnerTemplate, 1f);

        if (spawnerTemplate != null)
            spawnerTemplate.initialVelocity = TrafficManager.Instance.GetBaseVelocityForPlantilla(spawnerTemplate);

        ReleaseCount = 0;
        IsComplete = false;
        IsActive = false;
        Debug.Log("[BridgeControl] Reiniciado.");
    }

    public void OnFreezerDestroyed(BridgeCarFreezer freezer)
    {
        _freezers.Remove(freezer);
        _releasedCars.Remove(freezer.transform);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class BridgeControlManager : MonoBehaviour
{
    [Header("Plantilla del Spawner (1) - arrastrar Script Move")]
    public MovementController spawnerTemplate;

    [Header("Velocidad de avance lenta mientras suelta el flujo")]
    [Range(0.1f, 1f)]
    public float velocidadTrancon = 1f;

    [Header("Tiempo que el tráfico avanza tras cada toque (toques 1-3)")]
    [Range(1f, 30f)]
    public float tiempoAvance = 15f;

    [Header("Velocidad final en el 4º toque (flujo permanente y fluido)")]
    [Range(0.1f, 3f)]
    public float velocidadAvance = 1f;

    [Header("Trancón: intervalos de spawn al avanzar (jam)")]
    [Range(0.01f, 2f)]
    public float tranconSpawnMin = 0.15f;
    [Range(0.05f, 3f)]
    public float tranconSpawnMax = 0.5f;

    public bool IsComplete { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsReleased => _releaseTimer > 0f;
    public int ReleaseCount { get; private set; }

    private static BridgeControlManager _instance;
    public static BridgeControlManager Instance
    {
        get
        {
            if (_instance != null)
                return _instance;
            
            _instance = FindFirstObjectByType<BridgeControlManager>();
            
            if (_instance == null)
            {
                Debug.LogWarning("[BridgeControl] No hay BridgeControlManager en la escena. Funcionalidad limitada.");
                return null;
            }
            return _instance;
        }
    }

    // Rest of the class...

    public static event System.Action OnAllZonesComplete;

    private float _releaseTimer = 0f;
    private List<BridgeCarFreezer> _freezers = new List<BridgeCarFreezer>();
    private List<RandomObjectSpawner> _spawners = new List<RandomObjectSpawner>();

    // Cache de TODAS las plantillas "Script Move" de los RandomObjectSpawner:
    // las naves NUEVAS que se instancien durante el challenge deben nacer ya
    // a la velocidad controlada, sin importar de qué spawner vengan.
    private List<MovementController> _allTemplates = new List<MovementController>();
    private List<MovementController> _cachedAllMovementControllers = new List<MovementController>();
    private float _lastMovementControllerCacheTime = 0f;
    private const float MovementControllerCacheInterval = 1f;

    private void RebuildTemplateCache()
    {
        _allTemplates.Clear();
        foreach (var mc in ObtenerTodosLosMovementControllers())
        {
            if (mc == null || mc.gameObject.activeInHierarchy) continue;
            if (mc.gameObject.name == "Script Move" && !_allTemplates.Contains(mc))
                _allTemplates.Add(mc);
        }
    }

    private void SetAllTemplatesSpeed(Vector3 speed)
    {
        foreach (var mc in _allTemplates)
        {
            if (mc == null) continue;
            mc.useAcceleration = false;
            mc.initialVelocity = speed;
        }
    }

    // Aplica velocidad a un AUTO ya instanciado. Además de SetVelocity (que escribe
    // initialVelocity + currentVelocity), forzamos useAcceleration=false: si el clon
    // tiene useAcceleration=true, MovementController suma acceleration*dt cada frame y
    // "pisa" la velocidad fijada aquí. Al desactivar la aceleración el auto se mueve
    // exactamente a la velocidad objetivo.
    private void AplicarVelocidadAuto(MovementController mc, Vector3 speed)
    {
        if (mc == null) return;
        mc.useAcceleration = false;
        mc.SetVelocity(speed);
    }

    void Start()
    {
        ReleaseCount = 0;
        IsComplete = false;
        IsActive = false;

        if (spawnerTemplate == null)
            AutoFindTemplate();

        if (spawnerTemplate == null)
        {
            Debug.LogError("[BridgeControl] No hay spawnerTemplate asignado.");
            return;
        }

        CacheSpawners();

        TrafficManager.Instance.RegistrarPlantilla(spawnerTemplate);
    }

    private void CacheSpawners()
    {
        _spawners.Clear();
        foreach (var sp in FindObjectsByType<RandomObjectSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sp != null && !_spawners.Contains(sp))
                _spawners.Add(sp);
        }
    }

    /// <summary>
    /// Congestión DESDE EL INICIO del juego: el tráfico nace ya lento y apretado
    /// (velocidad de trancón + spawns densos), sin pausar la instanciación.
    /// Usado por la cinemática en lugar de velocidad completa.
    /// </summary>
    public void AplicarCongestionInicial()
    {
        if (spawnerTemplate == null)
            AutoFindTemplate();
        if (spawnerTemplate == null) return;

        CacheSpawners();
        RebuildTemplateCache();

        Vector3 lentoVel = VelocidadDeTrancon();
        SetAllTemplatesSpeed(lentoVel);

        foreach (var mc in ObtenerTodosLosMovementControllers())
        {
            if (mc == null || mc == spawnerTemplate) continue;
            AplicarVelocidadAuto(mc, lentoVel);
        }

        // Spawns densos para el look "pegados", pero SEGUIMOS instanciando
        // para que el tráfico fluya lento y no quede vacío.
        AplicarSpawnTrancon(true);
        Debug.Log($"[BridgeControl] Congestión inicial aplicada: velocidad {lentoVel.x:F1}, spawns densos.", this);
    }

    // Pausar/reanudar la instanciación de naves. En ROJO no debe aparecer
    // ninguna nave nueva.
    private void PausarSpawners(bool on)
    {
        foreach (var sp in _spawners)
        {
            if (sp != null)
                sp.SetSpawningEnabled(!on);
        }
    }

    // Al avanzar (VERDE) aumentamos la densidad de spawns para simular un trancón:
    // reducimos el intervalo a tranconSpawnMin..tranconSpawnMax.
    private void AplicarSpawnTrancon(bool on)
    {
        foreach (var sp in _spawners)
        {
            if (sp == null) continue;
            if (on)
                sp.SetSpawnIntervalOverride(tranconSpawnMin, tranconSpawnMax);
            else
                sp.ClearSpawnIntervalOverride();
        }
    }

    private List<MovementController> ObtenerTodosLosMovementControllers()
    {
        // Cache for 1 second to avoid FindObjectsByType every call
        if (Time.time - _lastMovementControllerCacheTime > MovementControllerCacheInterval)
        {
            _cachedAllMovementControllers.Clear();
            _cachedAllMovementControllers.AddRange(
                FindObjectsByType<MovementController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            _lastMovementControllerCacheTime = Time.time;
        }
        return _cachedAllMovementControllers;
    }

    private BridgeCarFreezer ObtenerOCongelar(MovementController mc)
    {
        var freezer = mc.GetComponent<BridgeCarFreezer>();
        if (freezer == null)
            freezer = mc.gameObject.AddComponent<BridgeCarFreezer>();
        freezer.manager = this;
        freezer.originalVelocity = mc.initialVelocity;
        freezer.UpdateFreezePosition();
        // Garantiza que el auto quede DETENIDO aunque el freezer ya existiera
        // (AddComponent dispara OnEnable->frozen=true, pero un freezer previo
        // liberado con TemporalRelease/Release quedaria frozen=false).
        freezer.Refreeze();
        return freezer;
    }

    // Inicia el challenge: semáforo en ROJO, el tráfico se detiene.
    public void FreezeBridge()
    {
        if (spawnerTemplate == null) return;

        gameObject.SetActive(true);
        IsActive = true;
        IsComplete = false;
        ReleaseCount = 0;
        _releaseTimer = 0f;
        _freezers.Clear();
        RebuildTemplateCache();

        var tc = FindFirstObjectByType<TrafficCleanup>();
        if (tc != null) tc.CancelInvoke("Cleanup");

        // ROJO: no se instancia ninguna nave nueva durante la detención.
        CacheSpawners();
        PausarSpawners(true);

        // Plantillas lentas/detenidas para que las naves nuevas nazcan controladas.
        Vector3 lentoVel = VelocidadDeTrancon();
        SetAllTemplatesSpeed(lentoVel);

        // Congela todos los autos que hay en escena (semáforo rojo).
        foreach (var mc in ObtenerTodosLosMovementControllers())
        {
            if (mc == null || mc == spawnerTemplate) continue;
            _freezers.Add(ObtenerOCongelar(mc));
        }

        Debug.Log($"[BridgeControl] FreezeBridge: SEMÁFORO ROJO, {_freezers.Count} autos detenidos, Cleanup detenido.", this);
    }

    private void AutoFindTemplate()
    {
        var allMC = FindObjectsByType<MovementController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mc in allMC)
        {
            if (mc.gameObject.activeSelf) continue;
            if (mc.gameObject.name == "Script Move")
            {
                spawnerTemplate = mc;
                var parent = mc.transform.parent;
                Debug.Log($"[BridgeControl] Template auto-asignado: {(parent != null ? parent.name : "?")}/{mc.gameObject.name}", mc);
                return;
            }
        }

        if (allMC.Length > 0)
        {
            spawnerTemplate = allMC[allMC.Length - 1];
            Debug.LogWarning($"[BridgeControl] Template asignado por fallback: {spawnerTemplate.gameObject.name}", spawnerTemplate);
        }
    }

    void Update()
    {
        if (!IsActive || IsComplete) return;

        if (_releaseTimer > 0f)
        {
            _releaseTimer -= Time.deltaTime;

            // Mientras el flujo avanza (tras un toque), mantener velocidad lenta
            // también en los autos nuevos que aparezcan.
            if (Time.frameCount % 5 == 0)
            {
                Vector3 lentoVel = VelocidadDeTrancon();
                foreach (var mc in ObtenerTodosLosMovementControllers())
                {
                    if (mc == null || mc == spawnerTemplate) continue;
                    if (mc.initialVelocity.sqrMagnitude > lentoVel.sqrMagnitude + 0.01f)
                        AplicarVelocidadAuto(mc, lentoVel);
                }
            }

            // Se acabó el tiempo de avance: vuelve a ROJO y se detiene todo.
            if (_releaseTimer <= 0f)
            {
                PausarSpawners(true);
                AplicarSpawnTrancon(false);
                foreach (var f in _freezers)
                {
                    if (f != null && !f.IsFrozen)
                        f.Refreeze();
                }
                Debug.Log("[BridgeControl] Tiempo agotado: SEMÁFORO ROJO de nuevo, tráfico detenido.", this);
            }
        }
        else
        {
            // En rojo: congelar también los autos nuevos que aparezcan.
            foreach (var mc in ObtenerTodosLosMovementControllers())
            {
                if (mc == null || mc == spawnerTemplate) continue;
                if (mc.GetComponent<BridgeCarFreezer>() == null)
                    _freezers.Add(ObtenerOCongelar(mc));
            }
        }
    }

    // Llamado por el reto cuando se completa (vía RetoTraficoLinker).
    // Equivale a pulsar el botón una vez: avanza el tráfico con el semáforo en VERDE.
    public void RetoCompletado()
    {
        ReleaseStep();
    }

    // Cada toque del botón: avanza el flujo un tiempo determinado (toques 1-3)
    // o lo deja fluyendo para siempre (toque 4).
    public void ReleaseStep()
    {
        if (!IsActive || IsComplete || spawnerTemplate == null) return;

        ReleaseCount++;
        Debug.Log($"[BridgeControl] ReleaseStep {ReleaseCount}/4");

        if (ReleaseCount < 4)
        {
            Debug.Log($"[Bridge] Toque {ReleaseCount}/4 -> VERDE temporal: avanza {tiempoAvance}s a velocidad lenta ({velocidadTrancon}x).");

            foreach (var f in _freezers)
            {
                if (f != null && f.IsFrozen)
                    f.TemporalRelease();
            }

            Vector3 lentoVel = VelocidadDeTrancon();
            foreach (var mc in ObtenerTodosLosMovementControllers())
            {
                if (mc == null || mc == spawnerTemplate) continue;
                AplicarVelocidadAuto(mc, lentoVel);
            }

            _releaseTimer = tiempoAvance;

            // VERDE: se reanuda la instanciación y con mayor densidad (trancón).
            PausarSpawners(false);
            AplicarSpawnTrancon(true);
        }
        else
        {
            CompleteChallenge();
        }
    }

    private void CompleteChallenge()
    {
        Debug.Log($"[Bridge] Toque {ReleaseCount}/4 -> COMPLETE: el tráfico ya NO se detiene más (velocidad {velocidadAvance}x).", this);
        _releaseTimer = 0f;
        IsComplete = true;
        IsActive = false;

        // Flujo permanente: densidad normal de spawns (sin trancón).
        PausarSpawners(false);
        AplicarSpawnTrancon(false);

        var tc = FindFirstObjectByType<TrafficCleanup>();
        if (tc != null)
        {
            tc.CancelInvoke("Cleanup");
            tc.InvokeRepeating("Cleanup", 2f, 0.5f);
            tc.Cleanup();
        }

        // Suelta todos los autos de forma PERMANENTE y mantiene el flujo
        // a una velocidad más alta (velocidadAvance) para que no se vea lento.
        Vector3 finalVel = VelocidadDeAvance();
        AplicarVelocidadAuto(spawnerTemplate, finalVel);
        SetAllTemplatesSpeed(finalVel);

        foreach (var f in _freezers)
        {
            if (f == null) continue;
            f.Release();
            var mc = f.GetComponent<MovementController>();
            if (mc != null && mc != spawnerTemplate)
                AplicarVelocidadAuto(mc, finalVel);
        }

        foreach (var f in _freezers)
        {
            if (f == null) continue;
            Destroy(f);
        }
        _freezers.Clear();

        Debug.Log("[BridgeControl] CompleteChallenge: flujo permanente a velocidad alta.", this);
        OnAllZonesComplete?.Invoke();
    }

    public void Reiniciar()
    {
        _freezers.Clear();
        _releaseTimer = 0f;
        ReleaseCount = 0;
        IsComplete = false;
        IsActive = false;
        PausarSpawners(false);
        AplicarSpawnTrancon(false);
    }

    public void OnFreezerDestroyed(BridgeCarFreezer freezer)
    {
        _freezers.Remove(freezer);
    }

    private Vector3 VelocidadDeTrancon()
    {
        if (spawnerTemplate == null) return Vector3.zero;

        // Usar SIEMPRE la velocidad base ESTABLE registrada en TrafficManager,
        // nunca spawnerTemplate.initialVelocity directamente: ese valor puede
        // haber sido mutado por TrafficManager.SetVelocidad o RegistrarClon.
        Vector3 baseVel = spawnerTemplate.initialVelocity;
        if (TrafficManager.Instance != null)
        {
            Vector3 b = TrafficManager.Instance.GetBaseVelocityForPlantilla(spawnerTemplate);
            if (b.sqrMagnitude > 0.001f) baseVel = b;
        }

        return baseVel * velocidadTrancon;
    }

    // Velocidad para el 4º toque (flujo permanente): base * velocidadAvance,
    // más alta que el trancón para que no se vea lento.
    private Vector3 VelocidadDeAvance()
    {
        if (spawnerTemplate == null) return Vector3.zero;

        Vector3 baseVel = spawnerTemplate.initialVelocity;
        if (TrafficManager.Instance != null)
        {
            Vector3 b = TrafficManager.Instance.GetBaseVelocityForPlantilla(spawnerTemplate);
            if (b.sqrMagnitude > 0.001f) baseVel = b;
        }

        return baseVel * velocidadAvance;
    }
    /// <summary>
    /// Pausa o reanuda la instanciación de un spawner específico por nombre.
    /// Si on es true, pausa el spawner; si es false, lo reanuda.
    /// </summary>
    /// <param name="spawnerName">Nombre exacto del GameObject del spawner</param>
    /// <param name="on">true para pausar, false para reanudar</param>
    public void PauseSpawnerByName(string spawnerName, bool on)
    {
        foreach (var sp in FindObjectsByType<RandomObjectSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sp != null && sp.gameObject.name == spawnerName)
            {
                sp.SetSpawningEnabled(!on);
                Debug.Log($"[BridgeControl] Spawner '{spawnerName}' pausado={!on} reanudado={on}", sp);
                return;
            }
        }
        Debug.LogWarning($"[BridgeControl] No se encontró spawner con nombre '{spawnerName}'");
    }
}

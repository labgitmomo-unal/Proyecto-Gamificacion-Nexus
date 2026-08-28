using System.Collections.Generic;
using UnityEngine;

/// <summary>Controls traffic speed and keeps the authoritative registry of active vehicles.</summary>
public class TrafficManager : MonoBehaviour
{
    private static TrafficManager _instance;
    public static TrafficManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<TrafficManager>(FindObjectsInactive.Include);
                if (_instance != null)
                    _instance.TryInitialize();
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [Header("Plantillas - arrastra aquí los 'Script Move' de cada Car Line Spawner")]
    public List<MovementController> plantillas = new List<MovementController>();

    [Header("Velocidad")]
    [Range(0f, 2f)]
    public float multiplicador = 1f;

    private readonly Dictionary<MovementController, Vector3> velocidadesOriginales
        = new Dictionary<MovementController, Vector3>();
    private readonly Dictionary<MovementController, float> multiplicadoresPorPlantilla
        = new Dictionary<MovementController, float>();
    private readonly Dictionary<MovementController, GraphTrafficRoad> roadByVehicle
        = new Dictionary<MovementController, GraphTrafficRoad>();
    private readonly List<MovementController> clonesActivos = new List<MovementController>();

    public event System.Action<MovementController> VehicleRegistered;
    public event System.Action<MovementController> VehicleUnregistered;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        InitializeDictionaries();
    }

    /// <summary>Guarantees that the singleton dictionaries are initialized.</summary>
    public void TryInitialize()
    {
        if (_instance == null)
            _instance = this;
        if (velocidadesOriginales.Count > 0)
            return;
        InitializeDictionaries();
    }

    private void InitializeDictionaries()
    {
        velocidadesOriginales.Clear();
        multiplicadoresPorPlantilla.Clear();
        foreach (var template in plantillas)
        {
            if (template == null)
                continue;
            velocidadesOriginales[template] = template.initialVelocity;
            multiplicadoresPorPlantilla[template] = 1f;
        }
    }

    /// <summary>Registers a spawned vehicle without assigning a logical road.</summary>
    public void RegistrarClon(MovementController movementController)
    {
        RegistrarClon(movementController, null);
    }

    /// <summary>Registers a spawned vehicle and preserves its logical road source.</summary>
    public void RegistrarClon(MovementController movementController, GraphTrafficRoad sourceRoad)
    {
        if (movementController == null || clonesActivos.Contains(movementController))
            return;

        clonesActivos.Add(movementController);
        var spawnerMultiplier = ObtenerMultiplicadorPorDireccion(movementController.initialVelocity);
        movementController.initialVelocity *= multiplicador * spawnerMultiplier;
        roadByVehicle[movementController] = sourceRoad;
        VehicleRegistered?.Invoke(movementController);
    }

    /// <summary>Unregisters a vehicle when it is destroyed or returned to a pool.</summary>
    public void DesregistrarClon(MovementController movementController)
    {
        if (movementController == null)
            return;

        if (clonesActivos.Remove(movementController))
        {
            roadByVehicle.Remove(movementController);
            VehicleUnregistered?.Invoke(movementController);
        }
    }

    /// <summary>Counts active registered vehicles assigned to a logical road.</summary>
    public int CountActiveVehicles(GraphTrafficRoad road)
    {
        if (road == null)
            return 0;

        var count = 0;
        foreach (var vehicle in clonesActivos)
        {
            if (vehicle != null && vehicle.gameObject.activeInHierarchy
                && roadByVehicle.TryGetValue(vehicle, out var assignedRoad)
                && assignedRoad == road)
                count++;
        }
        return count;
    }

    /// <summary>Applies a global speed multiplier to templates and registered vehicles.</summary>
    public void SetVelocidad(float nuevoMultiplicador)
    {
        multiplicador = Mathf.Clamp(nuevoMultiplicador, 0f, 2f);

        foreach (var template in plantillas)
        {
            if (template == null || !velocidadesOriginales.ContainsKey(template))
                continue;
            var spawnerMultiplier = multiplicadoresPorPlantilla.TryGetValue(template, out var value) ? value : 1f;
            template.initialVelocity = velocidadesOriginales[template] * multiplicador * spawnerMultiplier;
        }

        clonesActivos.RemoveAll(movementController => movementController == null);
        foreach (var movementController in clonesActivos)
        {
            var baseVelocity = ObtenerVelocidadBasePorDireccion(movementController.initialVelocity);
            var spawnerMultiplier = ObtenerMultiplicadorPorDireccion(movementController.initialVelocity);
            movementController.initialVelocity = baseVelocity * multiplicador * spawnerMultiplier;
        }
    }

    /// <summary>Applies an independent speed multiplier to one template.</summary>
    public void SetMultiplicadorPorPlantilla(MovementController plantilla, float multiplier)
    {
        if (plantilla == null || !velocidadesOriginales.ContainsKey(plantilla))
            return;

        multiplicadoresPorPlantilla[plantilla] = Mathf.Clamp(multiplier, 0f, 2f);
        var globalMultiplier = multiplicador;
        var spawnerMultiplier = multiplicadoresPorPlantilla[plantilla];
        plantilla.initialVelocity = velocidadesOriginales[plantilla] * globalMultiplier * Mathf.Max(spawnerMultiplier, 0.01f);

        clonesActivos.RemoveAll(movementController => movementController == null);
        foreach (var movementController in clonesActivos)
        {
            if (!DireccionesCoinciden(movementController.initialVelocity, velocidadesOriginales[plantilla]))
                continue;

            var baseVelocity = ObtenerVelocidadBasePorDireccion(movementController.initialVelocity);
            movementController.initialVelocity = baseVelocity * globalMultiplier * spawnerMultiplier;
        }
    }

    /// <summary>Restores normal traffic speed.</summary>
    public void RestaurarVelocidad() => SetVelocidad(1f);

    /// <summary>Reduces traffic speed to the requested multiplier.</summary>
    public void RalentizarTrafico(float porcentaje = 0.1f) => SetVelocidad(porcentaje);

    /// <summary>Returns the base velocity of a registered template.</summary>
    public Vector3 GetBaseVelocityForPlantilla(MovementController plantilla)
    {
        if (plantilla == null || !velocidadesOriginales.ContainsKey(plantilla))
            return Vector3.zero;
        return velocidadesOriginales[plantilla] * multiplicador;
    }

    /// <summary>Registers a template dynamically when it was not assigned in the Inspector.</summary>
    public void RegistrarPlantilla(MovementController template)
    {
        if (template == null || plantillas.Contains(template))
            return;

        plantillas.Add(template);
        velocidadesOriginales[template] = template.initialVelocity;
        multiplicadoresPorPlantilla[template] = 1f;
    }

    /// <summary>Returns the current registry of spawned vehicles.</summary>
    public List<MovementController> ObtenerClones()
    {
        clonesActivos.RemoveAll(movementController => movementController == null);
        var staleMappings = new List<MovementController>();
        foreach (var pair in roadByVehicle)
        {
            if (pair.Key == null)
                staleMappings.Add(pair.Key);
        }
        foreach (var staleMapping in staleMappings)
            roadByVehicle.Remove(staleMapping);
        return clonesActivos;
    }

    /// <summary>Registers the time of a vehicle spawn for cleanup integrations.</summary>
    public void RegisterSpawnTime(MovementController movementController)
    {
    }

    /// <summary>Removes the time of a vehicle spawn for cleanup integrations.</summary>
    public void UnregisterSpawnTime(MovementController movementController)
    {
    }

    private Vector3 ObtenerVelocidadBasePorDireccion(Vector3 velocidadActual)
    {
        foreach (var pair in velocidadesOriginales)
        {
            if (DireccionesCoinciden(pair.Value, velocidadActual))
                return pair.Value;
        }

        return velocidadActual.normalized * 50f;
    }

    private static bool DireccionesCoinciden(Vector3 first, Vector3 second)
    {
        if (first.sqrMagnitude < 0.0001f || second.sqrMagnitude < 0.0001f)
            return false;
        return Vector3.Dot(first.normalized, second.normalized) > 0.9f;
    }

    private float ObtenerMultiplicadorPorDireccion(Vector3 velocidadActual)
    {
        foreach (var pair in multiplicadoresPorPlantilla)
        {
            if (velocidadesOriginales.TryGetValue(pair.Key, out var baseVelocity)
                && DireccionesCoinciden(baseVelocity, velocidadActual))
                return pair.Value;
        }

        return 1f;
    }
}

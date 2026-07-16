using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controla la velocidad del tráfico volador modificando los MovementController
/// de los Car Line Spawner y todos sus clones activos en escena.
/// </summary>
public class TrafficManager : MonoBehaviour
{
    public static TrafficManager Instance { get; private set; }

    [Header("Plantillas - arrastra aquí los 'Script Move' de cada Car Line Spawner")]
    public List<MovementController> plantillas = new List<MovementController>();

    [Header("Velocidad")]
    [Range(0f, 2f)]
    public float multiplicador = 1f;

    private Dictionary<MovementController, Vector3> velocidadesOriginales
        = new Dictionary<MovementController, Vector3>();

    private Dictionary<MovementController, float> multiplicadoresPorPlantilla
        = new Dictionary<MovementController, float>();

    private List<MovementController> clonesActivos = new List<MovementController>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Guardar velocidades originales de las plantillas
        foreach (var p in plantillas)
        {
            if (p == null) continue;
            velocidadesOriginales[p] = p.initialVelocity;
            multiplicadoresPorPlantilla[p] = 1f;
        }
    }

    /// <summary>
    /// Llamar desde RandomObjectSpawner cuando se instancia un clon.
    /// </summary>
    public void RegistrarClon(MovementController mc)
    {
        if (mc == null || clonesActivos.Contains(mc)) return;
        clonesActivos.Add(mc);
        float spawnerMult = ObtenerMultiplicadorPorDireccion(mc.initialVelocity);
        mc.initialVelocity *= multiplicador * spawnerMult;
    }

    /// <summary>
    /// Aplica un multiplicador a plantillas y clones activos (0=detenido, 1=normal).
    /// </summary>
    public void SetVelocidad(float nuevoMultiplicador)
    {
        multiplicador = Mathf.Clamp(nuevoMultiplicador, 0f, 2f);

        // Actualizar plantillas (afecta clones futuros)
        foreach (var p in plantillas)
        {
            if (p == null || !velocidadesOriginales.ContainsKey(p)) continue;
            float spawnerMult = multiplicadoresPorPlantilla.TryGetValue(p, out float sm) ? sm : 1f;
            p.initialVelocity = velocidadesOriginales[p] * multiplicador * spawnerMult;
        }

        // Actualizar clones ya existentes
        clonesActivos.RemoveAll(mc => mc == null);
        foreach (var mc in clonesActivos)
        {
            Vector3 velocidadBase = ObtenerVelocidadBasePorDireccion(mc.initialVelocity);
            float spawnerMult = ObtenerMultiplicadorPorDireccion(mc.initialVelocity);
            mc.initialVelocity = velocidadBase * multiplicador * spawnerMult;
        }
    }

    /// <summary>
    /// Multiplicador INDEPENDIENTE para una plantilla específica.
    /// 0 = detenido, 1 = normal. No afecta otras plantillas.
    /// </summary>
    public void SetMultiplicadorPorPlantilla(MovementController plantilla, float multiplier)
    {
        if (plantilla == null || !velocidadesOriginales.ContainsKey(plantilla)) return;

        multiplicadoresPorPlantilla[plantilla] = Mathf.Clamp(multiplier, 0f, 2f);

        float globalMult = multiplicador;
        float spawnerMult = multiplicadoresPorPlantilla[plantilla];

        // Aplicar a la plantilla
        plantilla.initialVelocity = velocidadesOriginales[plantilla] * globalMult * spawnerMult;

        // Aplicar a clones que coincidan con esta plantilla (por dirección)
        clonesActivos.RemoveAll(mc => mc == null);
        foreach (var mc in clonesActivos)
        {
            if (!DireccionesCoinciden(mc.initialVelocity, velocidadesOriginales[plantilla])) continue;

            Vector3 baseVel = ObtenerVelocidadBasePorDireccion(mc.initialVelocity);
            mc.initialVelocity = baseVel * globalMult * spawnerMult;
        }
    }

    /// <summary>Restaura la velocidad normal (multiplicador = 1).</summary>
    public void RestaurarVelocidad() => SetVelocidad(1f);

    /// <summary>Ralentiza el tráfico al porcentaje indicado (ej: 0.1 = 10%).</summary>
    public void RalentizarTrafico(float porcentaje = 0.1f) => SetVelocidad(porcentaje);

    public void DesregistrarClon(MovementController mc)
    {
        if (mc != null) clonesActivos.Remove(mc);
    }

    public List<MovementController> ObtenerClones()
    {
        clonesActivos.RemoveAll(mc => mc == null);
        return clonesActivos;
    }

    private Vector3 ObtenerVelocidadBasePorDireccion(Vector3 velocidadActual)
    {
        foreach (var kvp in velocidadesOriginales)
            if (DireccionesCoinciden(kvp.Value, velocidadActual))
                return kvp.Value;

        return velocidadActual.normalized * 50f;
    }

    private bool DireccionesCoinciden(Vector3 a, Vector3 b)
    {
        return Vector3.Dot(a.normalized, b.normalized) > 0.9f;
    }

    private float ObtenerMultiplicadorPorDireccion(Vector3 velocidadActual)
    {
        foreach (var kvp in multiplicadoresPorPlantilla)
        {
            if (velocidadesOriginales.TryGetValue(kvp.Key, out var velBase) &&
                DireccionesCoinciden(velBase, velocidadActual))
            {
                return kvp.Value;
            }
        }
        return 1f;
    }
}

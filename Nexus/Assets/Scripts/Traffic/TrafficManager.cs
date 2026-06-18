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

    private List<MovementController> clonesActivos = new List<MovementController>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Guardar velocidades originales de las plantillas
        foreach (var p in plantillas)
            if (p != null) velocidadesOriginales[p] = p.initialVelocity;
    }

    /// <summary>
    /// Llamar desde RandomObjectSpawner cuando se instancia un clon.
    /// </summary>
    public void RegistrarClon(MovementController mc)
    {
        if (mc == null || clonesActivos.Contains(mc)) return;
        clonesActivos.Add(mc);
        mc.initialVelocity *= multiplicador;
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
            p.initialVelocity = velocidadesOriginales[p] * multiplicador;
        }

        // Actualizar clones ya existentes
        clonesActivos.RemoveAll(mc => mc == null);
        foreach (var mc in clonesActivos)
        {
            // Reconstruir desde la plantilla más cercana por dirección
            Vector3 velocidadBase = ObtenerVelocidadBasePorDireccion(mc.initialVelocity);
            mc.initialVelocity = velocidadBase * multiplicador;
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
            if (Vector3.Dot(kvp.Value.normalized, velocidadActual.normalized) > 0.9f)
                return kvp.Value;

        return velocidadActual.normalized * 50f;
    }
}

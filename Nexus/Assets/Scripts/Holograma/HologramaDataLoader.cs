using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recibe los datos del JSON descargado por DriveDataLoader y los aplica
/// al GraphManager (rutas de movilidad) y al TrafficManager (velocidad del tráfico).
/// Coloca este componente en el mismo GameObject que GraphManager.
/// </summary>
public class HologramaDataLoader : MonoBehaviour
{
    // ─── Tipos de datos del JSON ──────────────────────────────────────────────

    [Serializable]
    private class RutaData
    {
        /// <summary>
        /// Nombre de la ruta — debe coincidir con el nombre del LineRenderer
        /// asignado en la lista 'rutas' del GraphManager.
        /// </summary>
        public string nombre = "";

        /// <summary>Volumen de pasajeros en millones.</summary>
        public float volumenPasajerosMillon = 5f;

        /// <summary>Densidad vehicular en veh/km³.</summary>
        public float densidadVehicular = 0f;
    }

    [Serializable]
    private class HologramaJSON
    {
        public List<RutaData> rutas = new List<RutaData>();

        /// <summary>
        /// Multiplicador global de velocidad del tráfico (0 = detenido, 1 = normal, 2 = doble).
        /// </summary>
        public float velocidadTrafico = 1f;
    }

    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Dependencias")]
    [Tooltip("Si está vacío se busca automáticamente la instancia en escena.")]
    public GraphManager graphManager;

    [Tooltip("Si está vacío se busca automáticamente la instancia en escena.")]
    public TrafficManager trafficManager;

    [Header("Configuración")]
    [Tooltip("Si es true, aplica los datos tan pronto como estén disponibles al inicio.")]
    public bool aplicarAlIniciar = true;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (graphManager  == null) graphManager  = GraphManager.Instance;
        if (trafficManager == null) trafficManager = TrafficManager.Instance;
    }

    private void OnEnable()
    {
        DriveDataLoader.OnDataLoaded += OnDatosDescargados;

        if (aplicarAlIniciar && (DriveDataLoader.DataReady || DriveDataLoader.HasLocalData()))
            AplicarDatosLocales();
    }

    private void OnDisable()
    {
        DriveDataLoader.OnDataLoaded -= OnDatosDescargados;
    }

    // ─── Handlers ─────────────────────────────────────────────────────────────

    private void OnDatosDescargados() => AplicarDatosLocales();

    // ─── API Pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Lee el JSON guardado por DriveDataLoader y aplica los valores al grafo
    /// y al sistema de tráfico. Se puede llamar manualmente desde otros scripts.
    /// </summary>
    public void AplicarDatosLocales()
    {
        string json = DriveDataLoader.ReadLocalJson();
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[HologramaDataLoader] No hay JSON local disponible.");
            return;
        }

        HologramaJSON datos;
        try
        {
            datos = JsonUtility.FromJson<HologramaJSON>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[HologramaDataLoader] Error al parsear JSON: {e.Message}");
            return;
        }

        if (datos == null)
        {
            Debug.LogWarning("[HologramaDataLoader] El JSON no contiene datos válidos.");
            return;
        }

        AplicarRutas(datos.rutas);
        AplicarTrafico(datos.velocidadTrafico);
    }

    // ─── Implementación Interna ───────────────────────────────────────────────

    private void AplicarRutas(List<RutaData> rutasJSON)
    {
        if (graphManager == null)
        {
            Debug.LogWarning("[HologramaDataLoader] GraphManager no encontrado.");
            return;
        }

        if (rutasJSON == null || rutasJSON.Count == 0) return;

        int actualizadas = 0;
        foreach (RutaData datosRuta in rutasJSON)
        {
            RutaMovilidad ruta = EncontrarRutaPorNombre(datosRuta.nombre);
            if (ruta == null)
            {
                Debug.LogWarning($"[HologramaDataLoader] Ruta '{datosRuta.nombre}' no encontrada en el GraphManager.");
                continue;
            }

            ruta.volumenPasajerosMillon = datosRuta.volumenPasajerosMillon;
            ruta.densidadVehicular      = datosRuta.densidadVehicular;

            // Actualizar también el snapshot base para que las reversiones sean correctas
            ruta.volumenBase  = datosRuta.volumenPasajerosMillon;
            ruta.densidadBase = datosRuta.densidadVehicular;

            actualizadas++;
        }

        // Redibujar el grafo con los nuevos valores
        graphManager.RefrescarGrafo();
        Debug.Log($"[HologramaDataLoader] {actualizadas}/{rutasJSON.Count} rutas actualizadas.");
    }

    private void AplicarTrafico(float multiplicador)
    {
        if (trafficManager == null)
        {
            Debug.LogWarning("[HologramaDataLoader] TrafficManager no encontrado.");
            return;
        }

        trafficManager.SetVelocidad(multiplicador);
        Debug.Log($"[HologramaDataLoader] Velocidad de tráfico aplicada: {multiplicador}x");
    }

    /// <summary>
    /// Busca una RutaMovilidad cuyo LineRenderer tenga el nombre indicado.
    /// </summary>
    private RutaMovilidad EncontrarRutaPorNombre(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;

        foreach (RutaMovilidad ruta in graphManager.rutas)
        {
            if (ruta?.lineRenderer == null) continue;
            if (ruta.lineRenderer.name.Equals(nombre, StringComparison.OrdinalIgnoreCase))
                return ruta;
        }

        return null;
    }
}

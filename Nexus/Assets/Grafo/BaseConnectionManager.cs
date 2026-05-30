using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.XR.Interaction.Toolkit.Interactors; // not needed here

/// <summary>
/// Gestiona las conexiones adyacentes del grid de bases holográficas.
///
/// Se auto-configura en Awake:
///   - Descubre todos los BaseSlots hijos por nombre (Base1 … Base9).
///   - Descubre los LineRenderers en el hijo "Conexiones" parseando el nombre
///     con el formato "Conexion_BX_BY".
///
/// Cuando ambas bases extremo están ocupadas por una bola, la conexión
/// cambia a _gradienteActivo y aumenta su ancho.
/// </summary>
public class BaseConnectionManager : MonoBehaviour
{
    // ─── Tipos internos ───────────────────────────────────────────────────────

    private struct Conexion
    {
        public BaseSlot     slotA;
        public BaseSlot     slotB;
        public LineRenderer lineRenderer;
        public bool         eraActiva;   // Estado previo para detectar cambios.
    }

    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Visual")]
    [Tooltip("Desplazamiento en Y (metros) para que la línea quede sobre la superficie de la base.")]
    [SerializeField] private float _alturaOffset = 0.28f;

    [Tooltip("Ancho de línea (metros) en estado inactivo.")]
    [SerializeField] private float _anchoInactivo = 0.005f;

    [Tooltip("Ancho de línea (metros) cuando la conexión está activa.")]
    [SerializeField] private float _anchoActivo = 0.018f;

    [Tooltip("Gradiente cuando la conexión está inactiva (siempre visible).")]
    [SerializeField] private Gradient _gradienteInactivo;

    [Tooltip("Gradiente cuando ambas bases están ocupadas.")]
    [SerializeField] private Gradient _gradienteActivo;

    // ─── Estado interno ───────────────────────────────────────────────────────

    private readonly List<BaseSlot> _slots      = new();
    private readonly List<Conexion> _conexiones = new();
    private readonly Dictionary<string, BaseSlot> _slotPorNombre = new();

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        InicializarGradientes();
        AutoDescubrirSlots();
        AutoDescubrirConexiones();
    }

    private void Start()
    {
        PreposicionarTodasLasLineas();
        ActualizarConexiones(forzar: true);
    }

    private void Update() => ActualizarConexiones(forzar: false);
    // Note: BaseSlot intentionally exposes only polling (IsOccupied).
    // This manager polls slots each frame in Update(), so there is no
    // subscription to placement/removal events here.

    // ─── Auto-discovery ───────────────────────────────────────────────────────

    /// <summary>Busca todos los BaseSlots en los hijos e indexa por nombre de GO.</summary>
    private void AutoDescubrirSlots()
    {
        _slots.Clear();
        _slotPorNombre.Clear();

        foreach (var slot in GetComponentsInChildren<BaseSlot>(true))
        {
            _slots.Add(slot);
            _slotPorNombre[slot.gameObject.name] = slot;
        }

        if (_slots.Count == 0)
            Debug.LogWarning("[BaseConnectionManager] No se encontraron BaseSlots en los hijos.");
        else
            Debug.Log($"[BaseConnectionManager] {_slots.Count} BaseSlots descubiertos.");
    }

    /// <summary>
    /// Busca el hijo "Conexiones" y parsea cada LineRenderer por su nombre
    /// (formato "Conexion_BX_BY" → BaseX, BaseY).
    /// </summary>
    private void AutoDescubrirConexiones()
    {
        _conexiones.Clear();

        Transform contenedor = transform.Find("Conexiones");
        if (contenedor == null)
        {
            Debug.LogError("[BaseConnectionManager] No se encontró el hijo 'Conexiones'.");
            return;
        }

        int encontradas = 0;
        foreach (Transform hijo in contenedor)
        {
            var lr = hijo.GetComponent<LineRenderer>();
            if (lr == null) continue;

            // Formato: "Conexion_BX_BY"  →  partes[1]="B1"  partes[2]="B2"
            string[] partes = hijo.name.Split('_');
            if (partes.Length < 3) continue;

            string nombreA = "Base" + partes[1].TrimStart('B');
            string nombreB = "Base" + partes[2].TrimStart('B');

            if (_slotPorNombre.TryGetValue(nombreA, out BaseSlot slotA) &&
                _slotPorNombre.TryGetValue(nombreB, out BaseSlot slotB))
            {
                _conexiones.Add(new Conexion
                {
                    slotA        = slotA,
                    slotB        = slotB,
                    lineRenderer = lr,
                    eraActiva    = false
                });
                encontradas++;
            }
            else
            {
                Debug.LogWarning($"[BaseConnectionManager] No se encontraron slots para '{hijo.name}' " +
                                 $"(buscando '{nombreA}' y '{nombreB}').");
            }
        }

        Debug.Log($"[BaseConnectionManager] {encontradas} conexiones descubiertas.");
    }

    // ─── Lógica de líneas ─────────────────────────────────────────────────────

    /// <summary>Posiciona todas las líneas en estado inactivo al iniciar.</summary>
    private void PreposicionarTodasLasLineas()
    {
        Vector3 offset = Vector3.up * _alturaOffset;

        foreach (var c in _conexiones)
        {
            if (c.lineRenderer == null || c.slotA == null || c.slotB == null) continue;

            c.lineRenderer.positionCount = 2;
            c.lineRenderer.SetPosition(0, c.slotA.transform.position + offset);
            c.lineRenderer.SetPosition(1, c.slotB.transform.position + offset);
            c.lineRenderer.colorGradient = _gradienteInactivo;
            c.lineRenderer.startWidth    = _anchoInactivo;
            c.lineRenderer.endWidth      = _anchoInactivo;
            c.lineRenderer.enabled       = true;
        }
    }

    /// <summary>
    /// Evalúa todas las conexiones consultando el estado de ocupación en tiempo real.
    /// Solo modifica el LineRenderer cuando el estado cambia.
    /// </summary>
    private void ActualizarConexiones(bool forzar)
    {
        Vector3 offset = Vector3.up * _alturaOffset;

        for (int i = 0; i < _conexiones.Count; i++)
        {
            Conexion c = _conexiones[i];
            if (c.lineRenderer == null || c.slotA == null || c.slotB == null) continue;

            bool activa = c.slotA.IsOccupied && c.slotB.IsOccupied;
            if (!forzar && activa == c.eraActiva) continue;

            c.lineRenderer.colorGradient = activa ? _gradienteActivo  : _gradienteInactivo;
            c.lineRenderer.startWidth    = activa ? _anchoActivo       : _anchoInactivo;
            c.lineRenderer.endWidth      = activa ? _anchoActivo       : _anchoInactivo;

            c.lineRenderer.SetPosition(0, c.slotA.transform.position + offset);
            c.lineRenderer.SetPosition(1, c.slotB.transform.position + offset);

            c.eraActiva    = activa;
            _conexiones[i] = c;   // Structs son valor — hay que escribir de vuelta.
        }
    }

    // ─── Gradientes por defecto ───────────────────────────────────────────────

    /// <summary>
    /// Inicializa los gradientes con valores útiles si el Inspector los dejó
    /// en el blanco por defecto de Unity (dos GradientColorKey blancos).
    /// </summary>
    private void InicializarGradientes()
    {
        if (EsGradienteBlancoPorDefecto(_gradienteInactivo))
            _gradienteInactivo = CrearGradienteUniforme(new Color(0f, 0.4f, 0.9f, 0.35f));

        if (EsGradienteBlancoPorDefecto(_gradienteActivo))
            _gradienteActivo = CrearGradienteActivo();
    }

    /// <summary>Detecta el Gradient blanco que Unity asigna por defecto en el Inspector.</summary>
    private static bool EsGradienteBlancoPorDefecto(Gradient g)
    {
        if (g == null || g.colorKeys.Length != 2) return false;
        return g.colorKeys[0].color == Color.white && g.colorKeys[1].color == Color.white;
    }

    /// <summary>Gradient uniforme de color y alpha constantes.</summary>
    private static Gradient CrearGradienteUniforme(Color color)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a, 1f) }
        );
        return g;
    }

    /// <summary>Gradient activo: azul intenso en extremos → cian brillante en el centro.</summary>
    private static Gradient CrearGradienteActivo()
    {
        var colorExtremo = new Color(0f, 0.4f, 1f);
        var colorCentro  = new Color(0.3f, 1f, 1f);

        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(colorExtremo, 0f),
                new GradientColorKey(colorCentro,  0.5f),
                new GradientColorKey(colorExtremo, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(1f,   0.5f),
                new GradientAlphaKey(0.8f, 1f)
            }
        );
        return g;
    }
}

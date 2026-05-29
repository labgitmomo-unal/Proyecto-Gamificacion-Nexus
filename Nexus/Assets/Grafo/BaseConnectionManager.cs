using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona las 12 conexiones adyacentes del grid 3×3 de bases holográficas.
///
/// Comportamiento visual:
///   - Desde Start(), todas las conexiones son visibles en estado inactivo (_gradienteInactivo),
///     dibujadas entre las posiciones reales de las bases en escena.
///   - Cuando ambas bases extremo están ocupadas por una bola, la conexión cambia a
///     _gradienteActivo (más brillante / color distinto).
///
/// _gradienteInactivo y _gradienteActivo son configurables en el Inspector.
/// El ancho y el material de cada línea se configura directamente en su LineRenderer.
/// </summary>
public class BaseConnectionManager : MonoBehaviour
{
    // ─── Tipos de Datos ───────────────────────────────────────────────────────

    [Serializable]
    public struct DefinicionConexion
    {
        [Tooltip("Primera base extremo de la conexión.")]
        public BaseSlot slotA;

        [Tooltip("Segunda base extremo de la conexión.")]
        public BaseSlot slotB;

        [Tooltip("LineRenderer que dibuja la arista entre las dos bases.")]
        public LineRenderer lineRenderer;
    }

    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Conexiones del Grid")]
    [Tooltip("Las 12 conexiones adyacentes del grid 3×3.")]
    [SerializeField] private List<DefinicionConexion> _conexiones = new();

    [Header("Slots del Grid")]
    [Tooltip("Los 9 BaseSlots de las bases. Necesarios para suscribirse a eventos de colocación.")]
    [SerializeField] private List<BaseSlot> _slots = new();

    [Header("Visual")]
    [Tooltip("Desplazamiento en Y (unidades mundo) sobre el centro de la base. " +
             "Para este grid la semialtura mundo de cada base es 0.20, por lo que " +
             "usar ≥ 0.25 garantiza que la línea quede por encima de la superficie.")]
    [SerializeField] private float _alturaOffset = 0.28f;

    [Tooltip("Color del grid cuando la conexión está inactiva (siempre visible).")]
    [SerializeField] private Gradient _gradienteInactivo;

    [Tooltip("Color cuando ambas bases están ocupadas y la conexión se activa.")]
    [SerializeField] private Gradient _gradienteActivo;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        InicializarGradientesDefecto();
    }

    private void Start()
    {
        // Posiciona todas las líneas y las deja visibles en estado inactivo.
        PreposicionarTodasLasLineas();
    }

    private void OnEnable()
    {
        foreach (var slot in _slots)
        {
            if (slot == null) continue;
            slot.OnBallPlaced  += HandleBallPlaced;
            slot.OnBallRemoved += HandleBallRemoved;
        }
    }

    private void OnDisable()
    {
        foreach (var slot in _slots)
        {
            if (slot == null) continue;
            slot.OnBallPlaced  -= HandleBallPlaced;
            slot.OnBallRemoved -= HandleBallRemoved;
        }
    }

    // ─── API Pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Registra un BaseSlot en tiempo de ejecución.
    /// Útil si los slots se generan proceduralmente.
    /// </summary>
    public void RegistrarSlot(BaseSlot slot)
    {
        if (slot == null || _slots.Contains(slot)) return;
        _slots.Add(slot);
        slot.OnBallPlaced  += HandleBallPlaced;
        slot.OnBallRemoved += HandleBallRemoved;
    }

    // ─── Handlers ─────────────────────────────────────────────────────────────

    private void HandleBallPlaced(BaseSlot _)  => RefrescarConexiones();
    private void HandleBallRemoved(BaseSlot _) => RefrescarConexiones();

    // ─── Lógica de Conexiones ─────────────────────────────────────────────────

    /// <summary>
    /// Inicializa todos los LineRenderers con sus posiciones correctas en el mundo
    /// y los muestra en estado inactivo. Llamado una sola vez en Start().
    /// </summary>
    private void PreposicionarTodasLasLineas()
    {
        var offset = Vector3.up * _alturaOffset;

        foreach (var c in _conexiones)
        {
            if (c.lineRenderer == null || c.slotA == null || c.slotB == null) continue;

            c.lineRenderer.positionCount  = 2;
            c.lineRenderer.SetPosition(0, c.slotA.transform.position + offset);
            c.lineRenderer.SetPosition(1, c.slotB.transform.position + offset);
            c.lineRenderer.colorGradient  = _gradienteInactivo;
            c.lineRenderer.enabled        = true;
        }
    }

    /// <summary>
    /// Evalúa todas las conexiones y cambia su gradiente entre activo e inactivo.
    /// Al activarse, actualiza también las posiciones extremas del LineRenderer.
    /// </summary>
    private void RefrescarConexiones()
    {
        var offset = Vector3.up * _alturaOffset;

        foreach (var c in _conexiones)
        {
            if (c.lineRenderer == null || c.slotA == null || c.slotB == null) continue;

            bool activa = c.slotA.IsOccupied && c.slotB.IsOccupied;
            c.lineRenderer.colorGradient = activa ? _gradienteActivo : _gradienteInactivo;

            if (activa)
            {
                c.lineRenderer.SetPosition(0, c.slotA.transform.position + offset);
                c.lineRenderer.SetPosition(1, c.slotB.transform.position + offset);
            }
        }
    }

    // ─── Utilidades ───────────────────────────────────────────────────────────

    /// <summary>
    /// Asigna gradientes por defecto si el usuario no los configuró en el Inspector.
    /// </summary>
    private void InicializarGradientesDefecto()
    {
        if (_gradienteInactivo == null || _gradienteInactivo.colorKeys.Length == 0)
            _gradienteInactivo = CrearGradienteUniforme(new Color(0f, 0.55f, 1f, 0.35f));

        if (_gradienteActivo == null || _gradienteActivo.colorKeys.Length == 0)
            _gradienteActivo = CrearGradienteUniforme(new Color(0f, 1f, 0.45f, 1f));
    }

    /// <summary>Crea un Gradient de color y alpha uniformes de extremo a extremo.</summary>
    private static Gradient CrearGradienteUniforme(Color color)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a, 1f) }
        );
        return g;
    }
}

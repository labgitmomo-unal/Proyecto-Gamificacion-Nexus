using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona las conexiones adyacentes del grid de bases holográficas.
///
/// Se auto-configura en Awake:
///   - Descubre todos los BaseSlots hijos por nombre (Base1 … Base9).
///   - Descubre los LineRenderers en el hijo "Conexiones" parseando el nombre
///     con el formato "Conexion_BX_BY".
///
/// Cuando ambas bases extremo están ocupadas por una bola, la conexión
/// aplica la configuración manual de la línea.
/// </summary>
public class BaseConnectionManager : MonoBehaviour
{
    // ─── Tipos internos ───────────────────────────────────────────────────────

    private struct Conexion
    {
        public BaseSlot     slotA;
        public BaseSlot     slotB;
        public LineRenderer lineRenderer;
        public BaseConnectionLine lineaConfig;
        public bool         eraActiva;   // Estado previo para detectar cambios.
    }

    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Visual")]
    [Tooltip("Desplazamiento en Y (metros) para que la línea quede sobre la superficie de la base.")]
    [SerializeField] private float _alturaOffset = 0.28f;

    [Tooltip("Ancho base de la línea en metros.")]
    [SerializeField, Min(0.0001f)] private float _anchoBase = 0.005f;

    [Tooltip("Ancho máximo de la línea cuando el tráfico está al 100%.")]
    [SerializeField, Min(0.0001f)] private float _anchoMaximo = 0.02f;

    [Tooltip("Nivel de tráfico de la línea. 0 = azul, 1 = rojo y más gruesa.")]
    [SerializeField, Range(0f, 1f)] private float _trafico = 0f;

    [Tooltip("Color de la línea cuando el tráfico está en 0.")]
    [SerializeField] private Color _colorBajoTrafico = new Color(0f, 0.4f, 1f, 0.35f);

    [Tooltip("Color de la línea cuando el tráfico está en 1.")]
    [SerializeField] private Color _colorAltoTrafico = new Color(1f, 0.15f, 0.1f, 0.9f);

    // ─── Estado interno ───────────────────────────────────────────────────────

    private readonly List<BaseSlot> _slots      = new();
    private readonly List<Conexion> _conexiones = new();
    private readonly Dictionary<string, BaseSlot> _slotPorNombre = new();
    private float _traficoAplicado = -1f;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        AutoDescubrirSlots();
        AutoDescubrirConexiones();
    }

    private void Start()
    {
        PreposicionarTodasLasLineas();
        ActualizarConexiones(forzar: true);
    }

    private void Update() => ActualizarConexiones(forzar: false);

    private void OnValidate()
    {
        if (Application.isPlaying)
            ActualizarConexiones(forzar: true);
    }

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

            BaseConnectionLine lineaConfig = hijo.GetComponent<BaseConnectionLine>();
            if (lineaConfig == null)
                lineaConfig = hijo.gameObject.AddComponent<BaseConnectionLine>();

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
                    lineaConfig  = lineaConfig,
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
        foreach (var c in _conexiones)
        {
            if (c.lineRenderer == null || c.slotA == null || c.slotB == null) continue;

            c.lineRenderer.positionCount = 2;
            PosicionarLinea(c);
            if (c.lineaConfig != null)
            {
                c.lineaConfig.Activa = false;
                c.lineaConfig.Aplicar();
            }
        }
    }

    /// <summary>
    /// Evalúa todas las conexiones consultando el estado de ocupación en tiempo real.
    /// Solo modifica el LineRenderer cuando el estado cambia.
    /// </summary>
    private void ActualizarConexiones(bool forzar)
    {
        for (int i = 0; i < _conexiones.Count; i++)
        {
            Conexion c = _conexiones[i];
            if (c.lineRenderer == null || c.slotA == null || c.slotB == null || c.lineaConfig == null) continue;

            bool activa = c.slotA.IsOccupied && c.slotB.IsOccupied;
            if (forzar || activa != c.eraActiva)
            {
                c.lineaConfig.Activa = activa;
                c.lineaConfig.Aplicar();
            }

            PosicionarLinea(c);

            c.eraActiva    = activa;
            _conexiones[i] = c;   // Structs son valor — hay que escribir de vuelta.
        }
    }

    /// <summary>
    /// Posiciona la línea en el espacio local del LineRenderer para que siga
    /// correctamente al grafo cuando su raíz se mueva.
    /// </summary>
    private void PosicionarLinea(Conexion c)
    {
        if (c.lineRenderer == null || c.slotA == null || c.slotB == null) return;

        // Si la base tiene una bola tocando, usar la posición de la bola para
        // igualar la altura; si no, caer a la posición de la base con offset.
        Vector3 puntoA;
        if (c.slotA.TryGetContactPosition(out Vector3 contactoA))
            puntoA = contactoA;
        else
            puntoA = c.slotA.transform.position + c.slotA.transform.up * _alturaOffset;

        Vector3 puntoB;
        if (c.slotB.TryGetContactPosition(out Vector3 contactoB))
            puntoB = contactoB;
        else
            puntoB = c.slotB.transform.position + c.slotB.transform.up * _alturaOffset;

        c.lineRenderer.SetPosition(0, puntoA);
        c.lineRenderer.SetPosition(1, puntoB);
    }

}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tipos de pivote de mejora que el jugador puede colocar en un nodo.
/// </summary>
public enum TipoPivote
{
    SemaforoInteligente,
    BusRapido,
    PeajeElectronico
}

/// <summary>
/// Representa una arista del grafo: vincula un LineRenderer con sus datos de
/// flujo de pasajeros y estado de congestión.
/// </summary>
[System.Serializable]
public class RutaMovilidad
{
    [Tooltip("LineRenderer que dibuja la conexión entre dos nodos.")]
    public LineRenderer lineRenderer;

    [Tooltip("Volumen de pasajeros en millones (ej. 12.5 para el Centro).")]
    public float volumenPasajerosMillon = 5f;

    [Tooltip("Densidad vehicular actual en veh/km³.")]
    public float densidadVehicular = 0f;

    /// <summary>Snapshot del volumen base antes de aplicar mejoras. Usado para revertir exactamente.</summary>
    [HideInInspector] public float volumenBase;

    /// <summary>Snapshot de la densidad base antes de aplicar mejoras. Usado para revertir exactamente.</summary>
    [HideInInspector] public float densidadBase;

    /// <summary>Nodo origen de la arista.</summary>
    public HologramNodeFeedback nodoOrigen;

    /// <summary>Nodo destino de la arista.</summary>
    public HologramNodeFeedback nodoDestino;

    /// <summary>
    /// Devuelve true cuando la densidad vehicular supera el máximo de referencia.
    /// </summary>
    public bool EstaCongestionada => densidadVehicular >= ObtenerDensidadMaxReferencia();

    /// <summary>
    /// Nivel de congestión normalizado [0, 1] basado en densidadMaxReferencia.
    /// 0 = sin tráfico, 1 = densidad máxima → color más rojo del gradiente.
    /// </summary>
    public float NivelCongestionNormalizado =>
        ObtenerDensidadMaxReferencia() > 0f
            ? Mathf.Clamp01(densidadVehicular / ObtenerDensidadMaxReferencia())
            : 0f;

    private float ObtenerDensidadMaxReferencia()
    {
        return GraphManager.Instance != null
            ? GraphManager.Instance.densidadMaxReferencia
            : 15f;
    }
}

/// <summary>
/// Guarda los deltas de un impacto activo para poder revertirlo exactamente.
/// </summary>
public class ImpactoMejora
{
    public HologramNodeFeedback Nodo;
    public MejoraMovilidad MejoraRef;
    public Dictionary<RutaMovilidad, (float deltaVolumen, float deltaDensidad)> Deltas
        = new Dictionary<RutaMovilidad, (float, float)>();
}

/// <summary>
/// Vincula nodos (HologramNodeFeedback) con aristas (LineRenderers / RutaMovilidad).
/// Maneja el resaltado visual del grafo al hover y la aplicación de mejoras.
/// </summary>
public class GraphManager : MonoBehaviour, IGraphManager
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static GraphManager Instance { get; private set; }

    // ─── Evento de presupuesto ────────────────────────────────────────────────
    /// <summary>
    /// Se dispara cada vez que cambia el número de mejoras activas.
    /// El parámetro bool indica si el presupuesto ALOM está agotado.
    /// NodoSnapZone se suscribe para habilitar/deshabilitar sus sockets.
    /// </summary>
    public static event System.Action<bool> OnPresupuestoCambiado;

    // ─── Configuración Inspector ──────────────────────────────────────────────
    [Header("Rutas de Movilidad")]
    [Tooltip("Lista de aristas que conforman el grafo de flujo.")]
    public List<RutaMovilidad> rutas = new List<RutaMovilidad>();

    [Header("Ancho de Línea")]
    [Tooltip("Volumen máximo de referencia (en millones) para mapear al ancho máximo.")]
    public float volumenMaxReferencia = 15f;

    [Tooltip("Ancho mínimo del LineRenderer (en unidades de mundo) cuando el volumen es cercano a cero.")]
    public float anchoMinimo = 0.05f;

    [Tooltip("Ancho máximo del LineRenderer (en unidades de mundo) para el 70 % del flujo al Centro.")]
    public float anchoMaximo = 15.0f;

    [Header("Gradiente de Congestión")]
    [Tooltip("Gradiente aplicado al LineRenderer según el nivel de congestión (0=Verde, 1=Rojo HDR).")]
    public Gradient gradienteCongestión = new Gradient();

    [Tooltip("Densidad vehicular máxima de referencia (veh/km³). Este valor se usa para todas las rutas.")]
    public float densidadMaxReferencia = 15f;

    [Header("Feedback al Hover")]
    [Tooltip("Intensidad de brillo adicional aplicada a las líneas al hacer hover sobre un nodo conectado.")]
    public float intensidadHoverLinea = 3f;

    [Tooltip("Color HDR que adquiere la línea durante el hover.")]
    public Color colorHoverLinea = new Color(0f, 1f, 1f, 1f);

    [Header("Mejoras aplicadas")]
    [Tooltip("Factor de reducción del ancho al aplicar un Semáforo Inteligente (0–1).")]
    [Range(0f, 1f)]
    public float factorReduccionSemaforo = 0.5f;

    [Tooltip("Factor de reducción al aplicar un Peaje Electrónico (0–1).")]
    [Range(0f, 1f)]
    public float factorReduccionPeaje = 0.45f;

    [Header("Presupuesto ALOM")]
    [Tooltip("Número máximo de mejoras que el jugador puede colocar simultáneamente.")]
    public int maxMedidas = 3;

    [Header("UI de Nodos (opcional)")]
    public List<GameObject> nodosUI = new List<GameObject>();

    // ─── Estado Interno ───────────────────────────────────────────────────────
    /// <summary>Índice rápido: nodo → rutas conectadas.</summary>
    private readonly Dictionary<HologramNodeFeedback, List<RutaMovilidad>>
        _rutasPorNodo = new Dictionary<HologramNodeFeedback, List<RutaMovilidad>>();

    /// <summary>Materiales instanciados por LineRenderer para no afectar activos compartidos.</summary>
    private readonly Dictionary<LineRenderer, Material> _materialesInstancia
        = new Dictionary<LineRenderer, Material>();

    /// <summary>Impactos activos por nodo, para poder revertirlos exactamente.</summary>
    private readonly Dictionary<HologramNodeFeedback, ImpactoMejora>
        _impactosActivos = new Dictionary<HologramNodeFeedback, ImpactoMejora>();

    /// <summary>True una vez que Awake completó la inicialización completa.</summary>
    private bool _inicializado;

    // ─── Colores constantes para mejoras ─────────────────────────────────────
    private static readonly Color ColorSemaforo  = new Color(0f, 0.8f, 1f, 1f);
    private static readonly Color ColorBus       = new Color(1f, 0.6f, 0f, 1f);
    private static readonly Color ColorPeaje     = new Color(0.8f, 0f, 1f, 1f);

    // ─── Propiedades de presupuesto ───────────────────────────────────────────
    /// <summary>Número de mejoras actualmente colocadas.</summary>
    public int MedidasActivas => _impactosActivos.Count;

    /// <summary>True cuando se alcanzó el límite ALOM y no se pueden añadir más mejoras.</summary>
    public bool PresupuestoAgotado => _impactosActivos.Count >= maxMedidas;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InicializarGradientePorDefecto();
        ConstruirIndice();
        InstanciarMateriales();
        GuardarSnapshotsBase();
        AplicarEstadoVisualInicial();
        _inicializado = true;
    }

    /// <summary>
    /// Unity invoca este método automáticamente cada vez que un campo del componente
    /// cambia desde el Inspector, tanto en Edit Mode como en Play Mode.
    /// </summary>
    private void OnValidate()
    {
        if (!_inicializado) return;

        ConstruirIndice();
        AplicarEstadoVisualInicial();
    }

    // ─── Inicialización ───────────────────────────────────────────────────────

    /// <summary>
    /// Rellena el gradiente de congestión si no fue configurado desde el Inspector.
    /// CAMBIO: Rango Verde HDR → Amarillo → Rojo HDR (antes era Cian en t=0).
    /// </summary>
    private void InicializarGradientePorDefecto()
    {
        if (gradienteCongestión.colorKeys.Length > 2) return;

        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(new Color(0f, 1f, 0f), 0f);      // Verde  ← CAMBIO
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.85f, 0f), 0.5f); // Amarillo
        colorKeys[2] = new GradientColorKey(new Color(1f, 0.05f, 0f), 1f);   // Rojo HDR

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);

        gradienteCongestión.SetKeys(colorKeys, alphaKeys);
    }

    /// <summary>
    /// Construye el diccionario nodo → rutas conectadas para búsqueda O(1).
    /// </summary>
    private void ConstruirIndice()
    {
        _rutasPorNodo.Clear();

        foreach (RutaMovilidad ruta in rutas)
        {
            if (ruta == null || ruta.lineRenderer == null) continue;
            RegistrarEnIndice(ruta.nodoOrigen, ruta);
            RegistrarEnIndice(ruta.nodoDestino, ruta);
        }
    }

    private void RegistrarEnIndice(HologramNodeFeedback nodo, RutaMovilidad ruta)
    {
        if (nodo == null) return;
        if (!_rutasPorNodo.ContainsKey(nodo))
            _rutasPorNodo[nodo] = new List<RutaMovilidad>();
        _rutasPorNodo[nodo].Add(ruta);
    }

    /// <summary>
    /// Crea instancias de material por cada LineRenderer para permitir cambios
    /// de color sin afectar el asset compartido M_Holograma_Base.mat.
    /// </summary>
    private void InstanciarMateriales()
    {
        foreach (RutaMovilidad ruta in rutas)
        {
            if (ruta?.lineRenderer == null) continue;
            LineRenderer lr = ruta.lineRenderer;
            if (!_materialesInstancia.ContainsKey(lr))
            {
                Material instancia = new Material(lr.sharedMaterial);
                lr.material = instancia;
                _materialesInstancia[lr] = instancia;
            }
        }
    }

    /// <summary>
    /// Guarda el volumen y densidad originales de cada ruta para poder revertir mejoras exactamente.
    /// </summary>
    private void GuardarSnapshotsBase()
    {
        foreach (RutaMovilidad ruta in rutas)
        {
            if (ruta == null) continue;
            ruta.volumenBase  = ruta.volumenPasajerosMillon;
            ruta.densidadBase = ruta.densidadVehicular;
        }
    }

    // ─── API Pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Llamado por HologramNodeFeedback cuando el jugador inicia hover sobre un nodo.
    /// Resalta todas las líneas conectadas a ese nodo.
    /// </summary>
    public void OnNodoHoverEnter(HologramNodeFeedback nodo)
    {
        if (!_rutasPorNodo.TryGetValue(nodo, out List<RutaMovilidad> conectadas)) return;
        foreach (RutaMovilidad ruta in conectadas)
            AplicarColorLinea(ruta.lineRenderer, colorHoverLinea * intensidadHoverLinea);
    }

    /// <summary>
    /// Llamado por HologramNodeFeedback cuando el jugador termina el hover.
    /// Restaura las líneas al estado visual basado en congestión.
    /// </summary>
    public void OnNodoHoverExit(HologramNodeFeedback nodo)
    {
        if (!_rutasPorNodo.TryGetValue(nodo, out List<RutaMovilidad> conectadas)) return;
        foreach (RutaMovilidad ruta in conectadas)
            AplicarColorCongestión(ruta);
    }

    /// <summary>
    /// Aplica el impacto de una MejoraMovilidad sobre las rutas conectadas al nodo.
    /// Reduce volumenPasajerosMillon y densidadVehicular según los factores definidos
    /// en el componente MejoraMovilidad, amplificados por el factorPrioridad del nodo.
    /// Llamado automáticamente por NodoSnapZone.
    /// </summary>
    public void AplicarPivoteEnNodo(HologramNodeFeedback nodo, MejoraMovilidad mejora)
    {
        if (nodo == null || mejora == null) return;
        if (_impactosActivos.ContainsKey(nodo)) return;
        if (!_rutasPorNodo.TryGetValue(nodo, out List<RutaMovilidad> conectadas)) return;

        ImpactoMejora impacto = new ImpactoMejora { Nodo = nodo, MejoraRef = mejora };
        Color colorMejora     = ObtenerColorMejora(mejora.tipoPivote);

        foreach (RutaMovilidad ruta in conectadas)
        {
            if (ruta.lineRenderer == null) continue;

            float deltaVolumen  = ruta.volumenPasajerosMillon * mejora.factorReduccionVolumen  * nodo.factorPrioridad;
            float deltaDensidad = ruta.densidadVehicular      * mejora.factorReduccionDensidad * nodo.factorPrioridad;

            ruta.volumenPasajerosMillon = Mathf.Max(0f, ruta.volumenPasajerosMillon - deltaVolumen);
            ruta.densidadVehicular      = Mathf.Max(0f, ruta.densidadVehicular      - deltaDensidad);

            impacto.Deltas[ruta] = (deltaVolumen, deltaDensidad);

            AplicarAnchoLinea(ruta.lineRenderer, CalcularAnchoPorVolumen(ruta.volumenPasajerosMillon));
            AplicarColorLinea(ruta.lineRenderer, colorMejora * 2f);
        }

        _impactosActivos[nodo] = impacto;
        OnPresupuestoCambiado?.Invoke(PresupuestoAgotado);

        Debug.Log($"[GraphManager] '{mejora.tipoPivote}' aplicado en '{nodo.name}' " +
                  $"(prioridad x{nodo.factorPrioridad}). Medidas: {MedidasActivas}/{maxMedidas}");
    }

    /// <summary>
    /// Revierte el impacto de una MejoraMovilidad retirada, restaurando los valores exactos
    /// previos y actualizando los visuales de las rutas.
    /// Llamado automáticamente por NodoSnapZone.
    /// </summary>
    public void RevertirPivoteDeNodo(HologramNodeFeedback nodo, MejoraMovilidad mejora)
    {
        if (nodo == null || mejora == null) return;
        if (!_impactosActivos.TryGetValue(nodo, out ImpactoMejora impacto)) return;
        if (!ReferenceEquals(impacto.MejoraRef, mejora)) return;

        foreach (KeyValuePair<RutaMovilidad, (float deltaVolumen, float deltaDensidad)> par in impacto.Deltas)
        {
            RutaMovilidad ruta = par.Key;
            ruta.volumenPasajerosMillon += par.Value.deltaVolumen;
            ruta.densidadVehicular      += par.Value.deltaDensidad;

            if (ruta.lineRenderer != null)
            {
                AplicarAnchoLinea(ruta.lineRenderer, CalcularAnchoPorVolumen(ruta.volumenPasajerosMillon));
                AplicarColorCongestión(ruta);
            }
        }

        _impactosActivos.Remove(nodo);
        OnPresupuestoCambiado?.Invoke(PresupuestoAgotado);

        Debug.Log($"[GraphManager] Mejora revertida en '{nodo.name}'. " +
                  $"Medidas: {MedidasActivas}/{maxMedidas}");
    }

    /// <summary>
    /// Recalcula y aplica el estado visual completo de todas las rutas.
    /// </summary>
    public void RefrescarGrafo()
    {
        AplicarEstadoVisualInicial();
    }

    // ─── Lógica Visual Interna ────────────────────────────────────────────────

    private void AplicarEstadoVisualInicial()
    {
        foreach (RutaMovilidad ruta in rutas)
        {
            if (ruta?.lineRenderer == null) continue;
            AplicarAnchoLinea(ruta.lineRenderer, CalcularAnchoPorVolumen(ruta.volumenPasajerosMillon));
            AplicarColorCongestión(ruta);
        }
    }

    private float CalcularAnchoPorVolumen(float volumenMillon)
    {
        if (volumenMaxReferencia <= 0f) return anchoMinimo;
        // Sin Clamp01: volúmenes por encima del máximo de referencia
        // producen anchos mayores que anchoMaximo, reflejando rutas de alto flujo.
        float t = volumenMillon / volumenMaxReferencia;
        return Mathf.Max(anchoMinimo, Mathf.Lerp(anchoMinimo, anchoMaximo, t));
    }

    private static void AplicarAnchoLinea(LineRenderer lr, float ancho)
    {
        lr.widthCurve      = AnimationCurve.Constant(0f, 1f, 1f);
        lr.widthMultiplier = ancho;
    }

    private void AplicarColorCongestión(RutaMovilidad ruta)
    {
        Color colorCongestión = gradienteCongestión.Evaluate(ruta.NivelCongestionNormalizado);
        AplicarColorLinea(ruta.lineRenderer, colorCongestión);
    }

    /// <summary>
    /// Aplica el color al LineRenderer instanciado.
    /// En URP se escribe tanto _BaseColor como _EmissionColor para que el cambio
    /// sea visible tanto en la emisión HDR como en el color base del material.
    /// </summary>
    private void AplicarColorLinea(LineRenderer lr, Color color)
    {
        if (lr == null) return;
        if (!_materialesInstancia.TryGetValue(lr, out Material mat)) return;

        mat.SetColor("_EmissionColor", color);
        mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, color.a));
    }

    private static Color ObtenerColorMejora(TipoPivote tipo) => tipo switch
    {
        TipoPivote.SemaforoInteligente => ColorSemaforo,
        TipoPivote.BusRapido           => ColorBus,
        TipoPivote.PeajeElectronico    => ColorPeaje,
        _                              => ColorSemaforo
    };
}
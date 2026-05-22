using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tipos de pivote de mejora que el jugador puede colocar en un nodo.
/// </summary>
public enum TipoPivote
{
    SemaforoInteligente,
    SistemaPrioridad
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

    /// <summary>
    /// Umbral de densidad crítica según la especificación del dominio.
    /// Por encima de este valor la ruta se considera congestionada.
    /// </summary>
    public const float DensidadCritica = 0.12f;

    /// <summary>Nodo origen de la arista.</summary>
    public HologramNodeFeedback nodoOrigen;

    /// <summary>Nodo destino de la arista.</summary>
    public HologramNodeFeedback nodoDestino;

    /// <summary>
    /// Devuelve true cuando la densidad vehicular supera el umbral crítico.
    /// </summary>
    public bool EstaCongestionada => densidadVehicular >= DensidadCritica;

    /// <summary>
    /// Nivel de congestión normalizado [0, 1], donde 1 = saturación máxima.
    /// </summary>
    public float NivelCongestionNormalizado =>
        Mathf.Clamp01(densidadVehicular / DensidadCritica);
}

/// <summary>
/// Vincula nodos (HologramNodeFeedback) con aristas (LineRenderers / RutaMovilidad).
/// Maneja el resaltado visual del grafo al hover y la aplicación de mejoras.
/// </summary>
public class GraphManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static GraphManager Instance { get; private set; }

    // ─── Configuración Inspector ──────────────────────────────────────────────
    [Header("Rutas de Movilidad")]
    [Tooltip("Lista de aristas que conforman el grafo de flujo.")]
    public List<RutaMovilidad> rutas = new List<RutaMovilidad>();

    [Header("Ancho de Línea")]
    [Tooltip("Volumen máximo de referencia (en millones) para mapear al ancho máximo.")]
    public float volumenMaxReferencia = 15f;

    [Tooltip("Ancho mínimo del LineRenderer (en unidades de mundo) cuando el volumen es cercano a cero.")]
    public float anchoMinimo = 0.05f;

    // CAMBIO: aumentado de 0.8f a 2.0f para líneas más gruesas
    [Tooltip("Ancho máximo del LineRenderer (en unidades de mundo) para el 70 % del flujo al Centro.")]
    public float anchoMaximo = 2.0f;

    [Header("Gradiente de Congestión")]
    // CAMBIO: tooltip actualizado para reflejar Verde en vez de Cian
    [Tooltip("Gradiente aplicado al LineRenderer según el nivel de congestión (0=Verde, 1=Rojo HDR).")]
    public Gradient gradienteCongestión = new Gradient();

    [Header("Feedback al Hover")]
    [Tooltip("Intensidad de brillo adicional aplicada a las líneas al hacer hover sobre un nodo conectado.")]
    public float intensidadHoverLinea = 3f;

    [Tooltip("Color HDR que adquiere la línea durante el hover.")]
    public Color colorHoverLinea = new Color(0f, 1f, 1f, 1f);

    [Header("Mejoras aplicadas")]
    [Tooltip("Factor de reducción del ancho al aplicar un Semáforo Inteligente (0–1).")]
    [Range(0f, 1f)]
    public float factorReduccionSemaforo = 0.5f;

    [Tooltip("Factor de reducción del ancho al aplicar un Sistema de Prioridad (0–1).")]
    [Range(0f, 1f)]
    public float factorReduccionPrioridad = 0.35f;

    // ─── Estado Interno ───────────────────────────────────────────────────────
    /// <summary>Índice rápido: nodo → rutas conectadas.</summary>
    private readonly Dictionary<HologramNodeFeedback, List<RutaMovilidad>>
        _rutasPorNodo = new Dictionary<HologramNodeFeedback, List<RutaMovilidad>>();

    /// <summary>Materiales instanciados por LineRenderer para no afectar activos compartidos.</summary>
    private readonly Dictionary<LineRenderer, Material> _materialesInstancia
        = new Dictionary<LineRenderer, Material>();

    /// <summary>True una vez que Awake completó la inicialización completa.</summary>
    private bool _inicializado;

    // ─── Colores constantes para mejoras ─────────────────────────────────────
    private static readonly Color ColorSemaforo = new Color(0f, 0.8f, 1f, 1f);   // Azul cian
    private static readonly Color ColorPrioridad = new Color(0.1f, 1f, 0.2f, 1f); // Verde

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
    /// Aplica la mejora de un pivote al nodo indicado: reduce el grosor de
    /// sus rutas conectadas y cambia su color a azul/verde según el tipo.
    /// </summary>
    public void AplicarMejora(TipoPivote tipo, HologramNodeFeedback nodo)
    {
        if (!_rutasPorNodo.TryGetValue(nodo, out List<RutaMovilidad> conectadas)) return;

        float factor = tipo == TipoPivote.SemaforoInteligente
            ? factorReduccionSemaforo
            : factorReduccionPrioridad;

        Color colorMejora = tipo == TipoPivote.SemaforoInteligente
            ? ColorSemaforo
            : ColorPrioridad;

        foreach (RutaMovilidad ruta in conectadas)
        {
            if (ruta.lineRenderer == null) continue;

            float anchoBase = CalcularAnchoPorVolumen(ruta.volumenPasajerosMillon);
            AplicarAnchoLinea(ruta.lineRenderer, anchoBase * factor);
            AplicarColorLinea(ruta.lineRenderer, colorMejora * 2f);
            ruta.densidadVehicular *= factor;
        }
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
        float t = Mathf.Clamp01(volumenMillon / volumenMaxReferencia);
        return Mathf.Lerp(anchoMinimo, anchoMaximo, t);
    }

    private static void AplicarAnchoLinea(LineRenderer lr, float ancho)
    {
        lr.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        lr.widthMultiplier = ancho;
    }

    private void AplicarColorCongestión(RutaMovilidad ruta)
    {
        Color colorCongestión = gradienteCongestión.Evaluate(ruta.NivelCongestionNormalizado);
        AplicarColorLinea(ruta.lineRenderer, colorCongestión);
    }

    private void AplicarColorLinea(LineRenderer lr, Color color)
    {
        if (lr == null) return;
        if (_materialesInstancia.TryGetValue(lr, out Material mat))
            mat.SetColor("_EmissionColor", color);
    }
}
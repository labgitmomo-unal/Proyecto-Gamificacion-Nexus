using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Panel de categorias mezcladas (ponderacion == 0).
///
/// Mecanica: dos toques selectivos XR.
///   1. Tocar/Seleccionar un boton → BotonDato se resalta AMARILLO
///   2. Tocar/Seleccionar una categoria (header) → se evalua el match:
///        Coincide   → boton VERDE → destruye → notifica progreso
///        No coincide → boton ROJO  → deselecciona
///
/// La sincronizacion entre pasos se hace a traves de BotonSeleccionado (estatico).
///
/// Layout (igual que aebb882):
///   - Zona superior (57%): titulos de categoria + CategoriaHeader interactivos
///   - Zona inferior (43%): botones mezclados + ScrollRect horizontal
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PanelPonderacionCero : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject buttonPrefab;

    [Header("Colores")]
    public Color colorBoton    = new Color(0f, 0.85f, 1f, 1f);
    public Color colorHeader   = new Color(0f, 1f, 1f, 1f);
    public Color colorFondoCol = new Color(0f, 0.08f, 0.15f, 0.9f);

    [Header("Tipografia")]
    public float fontSizeMaxBoton = 36f;
    public float fontSizeMinBoton = 14f;

    private CanvasGroup _cg;
    private bool _inicializado = false;

    /// <summary>Boton actualmente seleccionado por el jugador (estatico, compartido).</summary>
    public static BotonDato BotonSeleccionado { get; private set; }

    /// <summary>Evento estatico: disparado cuando un boton es tocado y presionado por XR.</summary>
    public static event Action<BotonDato> OnBotonTocado;

    void Awake()
    {
        _cg            = GetComponent<CanvasGroup>();
        _cg.alpha      = 0f;
        _cg.interactable= false;
        _cg.blocksRaycasts = false;
        _inicializado  = false;
        BotonSeleccionado = null;
    }

    void Start()
    {
        ProgresoAbstraccion.OnFaseCompletada += Inicializar;
        if (ProgresoAbstraccion.FaseCompletada)
            Inicializar();
    }

    void OnDestroy() { ProgresoAbstraccion.OnFaseCompletada -= Inicializar; }

    private void Inicializar()
    {
        if (_inicializado) return;
        _inicializado = true;

        // Limpiar hijos existentes
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        string json = DriveDataLoader.ReadLocalJson();
        if (json == null) { Debug.LogWarning("[PanelPonderacionCero] Sin JSON."); return; }

        BotonDataList lista;
        try { lista = JsonUtility.FromJson<BotonDataList>(json); }
        catch (Exception e) { Debug.LogError($"[Panel] JSON error: {e.Message}"); return; }

        // Filtrar solo ponderacion == 0 Y con categoria
        var items = new List<BotonData>();
        var categorias = new List<string>();
        foreach (var b in lista.botones)
        {
            if (!Mathf.Approximately(b.ponderacion, 0f)) continue;
            if (string.IsNullOrEmpty(b.categoria)) continue;
            items.Add(b);
            if (!categorias.Contains(b.categoria)) categorias.Add(b.categoria);
        }

        if (items.Count == 0) { Debug.LogWarning("[Panel] Sin items."); return; }

        // Mezclar (Fisher-Yates)
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var t = items[i]; items[i] = items[j]; items[j] = t;
        }

        Debug.Log($"[PanelPonderacionCero] {items.Count} items, {categorias.Count} categorias: {string.Join(",", categorias)}");

        ConstruirUI(items, categorias);

        _cg.alpha         = 1f;
        _cg.interactable  = true;
        _cg.blocksRaycasts= true;

        Debug.Log("[PanelPonderacionCero] Panel visible.");
    }

    /// <summary>Resetea el panel (borra hijos, oculta, deselecciona boton activo).</summary>
    public void Resetear()
    {
        // Deseleccionar primero
        if (BotonSeleccionado != null)
        {
            BotonSeleccionado.Deseleccionar();
            BotonSeleccionado = null;
        }

        if (_inicializado)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            _inicializado = false;
        }

        _cg.alpha         = 0f;
        _cg.interactable  = false;
        _cg.blocksRaycasts= false;

        Debug.Log("[PanelPonderacionCero] Panel reseteado.");
    }

    private void ConstruirUI(List<BotonData> items, List<string> categorias)
    {
        // === FONDO ===
        CrearImagen("Fondo", transform, new Color(0f,0.03f,0.08f,0.97f),
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // === ZONA SUPERIOR: titulos de categoria + CategoriaHeader ===
        var zonaTitulos = CrearNodo("ZonaTitulos", transform,
                              new Vector2(0f,0.57f), new Vector2(1f,1f),
                              new Vector2(6f,6f), new Vector2(-6f,-6f));
        {
            var hlg = zonaTitulos.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing     = 6;
            hlg.padding     = new RectOffset(6, 6, 6, 6);
            hlg.childControlWidth  = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
        }

        foreach (var cat in categorias)
            CrearTituloCategoria(cat, zonaTitulos.transform);

        // === ZONA INFERIOR: botones mezclados ===
        var zonaMix = CrearNodo("ZonaMezclados", transform,
                                new Vector2(0f,0f), new Vector2(1f,0.57f),
                                new Vector2(6f,6f), new Vector2(-6f,-6f));
        CrearImagen("FondoMix", zonaMix.transform, new Color(0f,0.05f,0.12f,0.9f),
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Instruccion
        var tituloGO = CrearNodo("Titulo", zonaMix.transform,
                                 new Vector2(0f,0.92f), new Vector2(1f,1f),
                                 new Vector2(8f,2f), new Vector2(-8f,-2f));
        var tTMP = tituloGO.AddComponent<TextMeshProUGUI>();
        tTMP.text = "Toca un dato, luego toca su categoria";
        tTMP.alignment = TextAlignmentOptions.Center;
        tTMP.enableAutoSizing = true;
        tTMP.fontSizeMin = 8;
        tTMP.fontSizeMax = 40;
        tTMP.color = new Color(0.7f, 0.9f, 1f, 0.8f);
        tTMP.fontStyle = FontStyles.Italic;
        tTMP.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // ScrollRect horizontal
        var mixScrollGO = CrearNodo("MixScroll", zonaMix.transform,
                                     new Vector2(0f,0f), new Vector2(1f,0.88f),
                                     new Vector2(4f,4f), new Vector2(-4f,-4f));
        {
            var bg = mixScrollGO.AddComponent<Image>();
            bg.color = new Color(0f,0f,0f,0.01f);

            var sr = mixScrollGO.AddComponent<ScrollRect>();
            var vp = CrearNodo("Viewport", mixScrollGO.transform,
                               Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var vpImg = vp.AddComponent<Image>();
            vpImg.color = new Color(0f,0f,0f,0.01f);
            var mask = vp.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = CrearNodo("Content", vp.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f,0f);
            contentRT.anchorMax = new Vector2(0f,1f);
            contentRT.pivot     = new Vector2(0f,0.5f);
            contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
            contentRT.sizeDelta = Vector2.zero;

            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing              = 10;
            hlg.padding              = new RectOffset(10, 10, 6, 6);
            hlg.childControlWidth    = false;
            hlg.childControlHeight   = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight= false;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vp.GetComponent<RectTransform>();
            sr.content  = contentRT;
            sr.horizontal = true;
            sr.vertical   = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 30f;
        }

        // Instanciar botones mezclados
        foreach (var item in items)
            CrearBoton(item, mixScrollGO.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>());
    }

    /// <summary>
    /// Crea el header de una categoria con CategoriaHeader para interaccion XR.
    /// El BoxCollider se calcula desde el RectTransform en world space.
    /// </summary>
    private CategoriaHeader CrearTituloCategoria(string categoria, Transform parent)
    {
        // Header visual
        var headerGO = new GameObject($"Header_{categoria}");
        headerGO.transform.SetParent(parent, false);
        headerGO.AddComponent<RectTransform>();
        headerGO.AddComponent<Image>().color = new Color(0f, 0.15f, 0.22f, 1f);
        var hLE = headerGO.AddComponent<LayoutElement>();
        hLE.minHeight = 90;
        hLE.preferredHeight = 90;

        // Texto en hijo separado
        var hTextoGO = new GameObject("HeaderTexto");
        hTextoGO.transform.SetParent(headerGO.transform, false);
        {
            var hTextoRT = hTextoGO.AddComponent<RectTransform>();
            hTextoRT.anchorMin = Vector2.zero;
            hTextoRT.anchorMax = Vector2.one;
            hTextoRT.offsetMin = hTextoRT.offsetMax = Vector2.zero;

            var hTMP = hTextoGO.AddComponent<TextMeshProUGUI>();
            hTMP.text = categoria;
            hTMP.alignment = TextAlignmentOptions.Center;
            hTMP.enableAutoSizing = true;
            hTMP.fontSizeMin = 10;
            hTMP.fontSizeMax = 52;
            hTMP.color = colorHeader;
            hTMP.fontStyle = FontStyles.Bold;
            hTMP.textWrappingMode = TMPro.TextWrappingModes.Normal;
        }

        // XR interaction: select cuando el controlador toca el header
        var xri = headerGO.AddComponent<XRBaseInteractable>();
        xri.selectMode = InteractableSelectMode.Multiple;

        // Logica de categoria
        var cata = headerGO.AddComponent<CategoriaHeader>();
        cata.categoria      = categoria;
        cata.textoCategoria = hTextoGO.GetComponentInChildren<TextMeshProUGUI>();
        cata.colorNormal    = new Color(0f, 0.15f, 0.22f, 1f);
        cata.colorHover     = new Color(0f, 0.30f, 0.38f, 1f);
        cata.colorAcierto   = new Color(0f, 0.55f, 0.12f, 1f);
        cata.colorError     = new Color(0.75f, 0.10f, 0.05f, 1f);

        // Ajustar BoxCollider al tamano visual en world space
        AjustarColliderHeader(headerGO, cata);

        return cata;
    }

    /// <summary>
    /// Calcula el box collider en world space a partir del RectTransform.
    /// </summary>
    private void AjustarColliderHeader(GameObject headerGO, CategoriaHeader cata)
    {
        var rt  = headerGO.GetComponent<RectTransform>();
        var box = headerGO.GetComponent<BoxCollider>();
        Camera cam = Camera.main ?? FindObjectOfType<Camera>();

        if (cam != null)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Vector3 localMin = headerGO.transform.InverseTransformPoint(corners[0]);
            Vector3 localMax = headerGO.transform.InverseTransformPoint(corners[2]);

            box.center = (localMin + localMax) * 0.5f;
            box.size   = localMax - localMin;
        }
        else
        {
            box.size   = new Vector3(2.5f, 0.45f, 0.01f);
            box.center = Vector3.forward * 0.005f;
        }
    }

    /// <summary>
    /// Crea un boton de dato en la zona inferior con BotonDato + XRBaseInteractable.
    /// Sin XRGrabInteractable ni Rigidbody (el boton NO se mueve al tocarlo).
    /// </summary>
    private void CrearBoton(BotonData datos, RectTransform parent)
    {
        GameObject go;
        if (buttonPrefab != null)
            go = Instantiate(buttonPrefab, parent);
        else
        {
            go = new GameObject($"Btn|{datos.categoria}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0.12f, 0.20f, 1f);
        }
        go.transform.localScale = Vector3.one;
        go.name = $"Btn|{datos.categoria}";

        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280f, 110f);

        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredWidth  = 280f;
        le.preferredHeight = 110f;
        le.minHeight       = 70f;

        // Eliminar Button (usamos XR select events en su lugar)
        var btn = go.GetComponent<Button>();
        if (btn != null) Destroy(btn);

        // ── XR interaction ─────────────────────────────────────────────
        var xr = go.GetComponent<XRBaseInteractable>();
        if (xr == null) xr = go.AddComponent<XRBaseInteractable>();
        xr.selectMode           = InteractableSelectMode.Multiple;
        xr.hoverToSelect        = true;
        xr.hoverToActivate      = false;

        // ── BotonDato: logica de seleccion y feedback visual ───────────
        var botonDato = go.GetComponent<BotonDato>();
        if (botonDato == null) botonDato = go.AddComponent<BotonDato>();
        botonDato.categoria       = datos.categoria;
        botonDato.colorNormal     = new Color(0f, 0.12f, 0.22f, 1f);
        botonDato.colorTextoNormal= colorBoton;
        botonDato.colorSeleccionado= new Color(1f, 0.82f, 0f, 1f);  // amarillo
        botonDato.colorCorrecto   = new Color(0f, 1f, 0.30f, 1f);    // verde
        botonDato.colorIncorrecto = new Color(0.9f, 0.1f, 0.1f, 1f);  // rojo

        // ── Texto ────────────────────────────────────────────────────────
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null)
        {
            var tGO = new GameObject("Text");
            tGO.transform.SetParent(go.transform, false);
            var tRT = tGO.AddComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0.04f, 0.04f);
            tRT.anchorMax = new Vector2(0.96f, 0.96f);
            tRT.offsetMin = tRT.offsetMax = Vector2.zero;
            tmp = tGO.AddComponent<TextMeshProUGUI>();
        }
        tmp.text               = datos.texto;
        tmp.color              = colorBoton;
        tmp.textWrappingMode   = TMPro.TextWrappingModes.Normal;
        tmp.overflowMode       = TextOverflowModes.Truncate;
        tmp.enableAutoSizing   = true;
        tmp.fontSizeMin        = fontSizeMinBoton;
        tmp.fontSizeMax        = fontSizeMaxBoton;
        tmp.alignment          = TextAlignmentOptions.Center;

        // Asegurar Image para raycast/graficos
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private GameObject CrearNodo(string nombre, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go   = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        var rt   = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        return go;
    }

    private void CrearImagen(string nombre, Transform parent, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = CrearNodo(nombre, parent, anchorMin, anchorMax, offsetMin, offsetMax);
        go.AddComponent<Image>().color = color;
    }
}

// ================================================================
// Data model
// ================================================================
[Serializable]
public class BotonData
{
    public string texto;
    public float  ponderacion;
    public string categoria;
}

[Serializable]
public class BotonDataList
{
    public List<BotonData> botones;
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class PanelPonderacionCero : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject buttonPrefab;

    [Header("Colores")]
    public Color colorBoton       = new Color(0f, 0.85f, 1f, 1f);
    public Color colorHeader      = new Color(0f, 1f, 1f, 1f);
    public Color colorFondoCol    = new Color(0f, 0.08f, 0.15f, 0.9f);

    [Header("Tipografia")]
    public float fontSizeMaxBoton = 28f;
    public float fontSizeMinBoton = 8f;

    private CanvasGroup _cg;
    private bool _inicializado = false;

	void Awake()
	{
		_cg = GetComponent<CanvasGroup>();
		_cg.alpha = 0f;
		_cg.interactable = false;
		_cg.blocksRaycasts = false;
		_inicializado = false;
	}

    void Start()
    {
        // Suscribirse al evento
        ProgresoAbstraccion.OnFaseCompletada += Inicializar;

        // Si la fase ya se completo antes de que este panel arrancara, inicializar ya
        if (ProgresoAbstraccion.FaseCompletada)
            Inicializar();
    }

    void OnDestroy() { ProgresoAbstraccion.OnFaseCompletada -= Inicializar; }

    private void Inicializar()
    {
        if (_inicializado) return;
        _inicializado = true;

        // Limpiar hijos duplicados por si acaso
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        string json = DriveDataLoader.ReadLocalJson();
        if (json == null) { Debug.LogWarning("[PanelPonderacionCero] Sin JSON."); return; }

        BotonDataList lista;
        try { lista = JsonUtility.FromJson<BotonDataList>(json); }
        catch (Exception e) { Debug.LogError($"[Panel] JSON error: {e.Message}"); return; }

        // Filtrar ponderacion==0 CON categoria
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

        // Mezclar Fisher-Yates
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var t = items[i]; items[i] = items[j]; items[j] = t;
        }

        Debug.Log($"[PanelPonderacionCero] {items.Count} items, {categorias.Count} categorias: {string.Join(",", categorias)}");

        ConstruirUI(items, categorias);

		_cg.alpha = 1f;
		_cg.interactable = true;
		_cg.blocksRaycasts = true;

		Debug.Log("[PanelPonderacionCero] Panel visible.");
	}

	/// <summary>
	/// Resetea el panel para una nueva sesion de juego.
	/// Llamado por ProgresoAbstraccion en OnEnable para limpiar el estado anterior.
	/// </summary>
	public void Resetear()
	{
		if (_inicializado)
		{
			// Limpiar hijos (botones, columnas, etc.)
			for (int i = transform.childCount - 1; i >= 0; i--)
				Destroy(transform.GetChild(i).gameObject);

			_inicializado = false;
		}

		// Ocultar el panel mientras no se complete la fase
		_cg.alpha = 0f;
		_cg.interactable = false;
		_cg.blocksRaycasts = false;

		Debug.Log("[PanelPonderacionCero] Panel reseteado.");
	}

    private void ConstruirUI(List<BotonData> items, List<string> categorias)
    {
        // === FONDO ===
        CrearImagen("Fondo", transform, new Color(0f,0.03f,0.08f,0.97f),
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // === ZONA SUPERIOR: botones mezclados (scroll horizontal) ===
        var zonaMix = CrearNodo("ZonaMezclados", transform,
                                new Vector2(0f,0.55f), new Vector2(1f,1f),
                                new Vector2(8f,8f), new Vector2(-8f,-6f));
        CrearImagen("FondoMix", zonaMix.transform, new Color(0f,0.05f,0.12f,0.9f),
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Titulo
        var tituloGO = CrearNodo("Titulo", zonaMix.transform,
                                 new Vector2(0f,0.82f), new Vector2(1f,1f),
                                 new Vector2(8f,2f), new Vector2(-8f,-2f));
        var tTMP = tituloGO.AddComponent<TextMeshProUGUI>();
        tTMP.text = "Arrastra cada dato a su categoria";
        tTMP.alignment = TextAlignmentOptions.Center;
        tTMP.enableAutoSizing = true; tTMP.fontSizeMin=6; tTMP.fontSizeMax=40;
        tTMP.color = new Color(0.7f,0.9f,1f,0.8f);
        tTMP.fontStyle = FontStyles.Italic;
        tTMP.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // ScrollRect horizontal para los botones mezclados
        var mixScrollGO = CrearNodo("MixScroll", zonaMix.transform,
                                     new Vector2(0f,0f), new Vector2(1f,0.80f),
                                     new Vector2(4f,4f), new Vector2(-4f,-4f));
        var mixBg = mixScrollGO.AddComponent<Image>();
        mixBg.color = new Color(0,0,0,0.01f);
        var mixScroll = mixScrollGO.AddComponent<ScrollRect>();

        var mixVP = CrearNodo("Viewport", mixScrollGO.transform,
                               Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var mixVPImg = mixVP.AddComponent<Image>(); mixVPImg.color = new Color(0,0,0,0.01f);
        var mixMask = mixVP.AddComponent<Mask>(); mixMask.showMaskGraphic = false;

        var mixContent = CrearNodo("Content", mixVP.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        var mixContentRT = mixContent.GetComponent<RectTransform>();
        mixContentRT.anchorMin = new Vector2(0,0); mixContentRT.anchorMax = new Vector2(0,1);
        mixContentRT.pivot     = new Vector2(0,0.5f);
        mixContentRT.offsetMin = mixContentRT.offsetMax = Vector2.zero;
        mixContentRT.sizeDelta = Vector2.zero;
        var hlg = mixContent.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing=10; hlg.padding=new RectOffset(10,10,6,6);
        hlg.childControlWidth=false; hlg.childControlHeight=false;
        hlg.childForceExpandWidth=false; hlg.childForceExpandHeight=false;
        var mixCSF = mixContent.AddComponent<ContentSizeFitter>();
        mixCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        mixScroll.viewport=mixVP.GetComponent<RectTransform>();
        mixScroll.content=mixContentRT;
        mixScroll.horizontal=true; mixScroll.vertical=false;
        mixScroll.movementType=ScrollRect.MovementType.Elastic;
        mixScroll.scrollSensitivity=30f;

        // === ZONA INFERIOR: columnas ===
        var zonaCols = CrearNodo("ZonaColumnas", transform,
                                  new Vector2(0f,0f), new Vector2(1f,0.53f),
                                  new Vector2(8f,8f), new Vector2(-8f,-6f));
        var zlg = zonaCols.AddComponent<HorizontalLayoutGroup>();
        zlg.spacing=8; zlg.padding=new RectOffset(6,6,6,6);
        zlg.childControlWidth=true; zlg.childControlHeight=true;
        zlg.childForceExpandWidth=true; zlg.childForceExpandHeight=true;

        // Crear columnas
        var dropZones = new Dictionary<string, RectTransform>();
        foreach (var cat in categorias)
        {
            var contentRT = CrearColumna(cat, zonaCols.transform);
            dropZones[cat] = contentRT;
        }

        // Instanciar botones mezclados
        foreach (var item in items)
            CrearBoton(item, mixContentRT, dropZones);
    }

    private RectTransform CrearColumna(string categoria, Transform parent)
    {
        // Contenedor
        var col = new GameObject($"Col_{categoria}");
        col.transform.SetParent(parent, false);
        col.AddComponent<RectTransform>();
        col.AddComponent<Image>().color = colorFondoCol;
        var vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.spacing=6; vlg.padding=new RectOffset(4,4,4,4);
        vlg.childControlWidth=true; vlg.childControlHeight=false;
        vlg.childForceExpandWidth=true; vlg.childForceExpandHeight=false;

        // Header
        var header = new GameObject("Header");
        header.transform.SetParent(col.transform, false);
        var headerRT = header.AddComponent<RectTransform>();
        var hLE = header.AddComponent<LayoutElement>(); hLE.minHeight=70; hLE.preferredHeight=70;
        header.AddComponent<Image>().color = new Color(0f,0.15f,0.22f,1f);
        // Texto en hijo separado para evitar conflicto Image+TMP en mismo GO
        var hTextoGO = new GameObject("HeaderTexto");
        hTextoGO.transform.SetParent(header.transform, false);
        var hTextoRT = hTextoGO.AddComponent<RectTransform>();
        hTextoRT.anchorMin = Vector2.zero; hTextoRT.anchorMax = Vector2.one;
        hTextoRT.offsetMin = hTextoRT.offsetMax = Vector2.zero;
        var hTMP = hTextoGO.AddComponent<TextMeshProUGUI>();
        hTMP.text = categoria; hTMP.alignment=TextAlignmentOptions.Center;
        hTMP.enableAutoSizing=true; hTMP.fontSizeMin=6; hTMP.fontSizeMax=52;
        hTMP.color=colorHeader; hTMP.fontStyle=FontStyles.Bold;
        hTMP.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Drop area con scroll vertical
        var dropGO = new GameObject("DropArea");
        dropGO.transform.SetParent(col.transform, false);
        dropGO.AddComponent<RectTransform>();
        var dropLE = dropGO.AddComponent<LayoutElement>(); dropLE.flexibleHeight=1;
        // Image necesaria para recibir raycasts
        var dropImg = dropGO.AddComponent<Image>(); dropImg.color=new Color(0f,0.04f,0.1f,0.5f);
        var dropScroll = dropGO.AddComponent<ScrollRect>();

        var vp = new GameObject("Viewport");
        vp.transform.SetParent(dropGO.transform, false);
        var vpRT = vp.AddComponent<RectTransform>();
        vpRT.anchorMin=Vector2.zero; vpRT.anchorMax=Vector2.one;
        vpRT.offsetMin=vpRT.offsetMax=Vector2.zero;
        var vpImg = vp.AddComponent<Image>(); vpImg.color=new Color(0,0,0,0.01f);
        var vpMask = vp.AddComponent<Mask>(); vpMask.showMaskGraphic=false;

        var content = new GameObject("Content");
        content.transform.SetParent(vp.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin=new Vector2(0,1); contentRT.anchorMax=new Vector2(1,1);
        contentRT.pivot=new Vector2(0.5f,1);
        contentRT.offsetMin=contentRT.offsetMax=Vector2.zero;
        var cvlg = content.AddComponent<VerticalLayoutGroup>();
        cvlg.spacing=6; cvlg.padding=new RectOffset(4,4,4,4);
        cvlg.childControlWidth=true; cvlg.childControlHeight=false;
        cvlg.childForceExpandWidth=true; cvlg.childForceExpandHeight=false;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit=ContentSizeFitter.FitMode.PreferredSize;

        dropScroll.viewport=vpRT; dropScroll.content=contentRT;
        dropScroll.horizontal=false; dropScroll.vertical=true;
        dropScroll.movementType=ScrollRect.MovementType.Elastic;

        // DropHandler en el DropArea (tiene Image = recibe raycast)
        var dh = dropGO.AddComponent<ColumnaDropHandler>();
        dh.categoria=categoria; dh.contentRT=contentRT;

        return contentRT;
    }

    private void CrearBoton(BotonData datos, RectTransform parent, Dictionary<string, RectTransform> dropZones)
    {
        GameObject go;
        if (buttonPrefab != null)
            go = Instantiate(buttonPrefab, parent);
        else
        {
            go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0f,0.12f,0.2f,1f);
        }
        go.transform.localScale = Vector3.one;
        go.name = $"Btn|{datos.categoria}";

        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(240f, 90f);

        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredWidth=240f; le.preferredHeight=90f; le.minHeight=60f;

        // Quitar Button
        var btn = go.GetComponent<Button>();
        if (btn != null) Destroy(btn);

        // Texto
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null)
        {
            var tGO = new GameObject("Text");
            tGO.transform.SetParent(go.transform, false);
            var tRT = tGO.AddComponent<RectTransform>();
            tRT.anchorMin=new Vector2(0.04f,0.04f); tRT.anchorMax=new Vector2(0.96f,0.96f);
            tRT.offsetMin=tRT.offsetMax=Vector2.zero;
            tmp = tGO.AddComponent<TextMeshProUGUI>();
        }
        tmp.text=datos.texto; tmp.color=colorBoton;
        tmp.textWrappingMode=TMPro.TextWrappingModes.Normal; tmp.overflowMode=TextOverflowModes.Truncate;
        tmp.enableAutoSizing=true; tmp.fontSizeMin=fontSizeMinBoton; tmp.fontSizeMax=fontSizeMaxBoton;
        tmp.alignment=TextAlignmentOptions.Center;

        // Asegurar que el boton tenga Image para raycast
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();

        // Arrastre
        var drag = go.AddComponent<BotonArrastrable>();
        drag.categoria=datos.categoria;
        drag.dropZones=dropZones;
        drag.colorNormal      = colorBoton;
        drag.colorArrastrando = new Color(0f,1f,0.5f,0.85f);
        drag.colorCorrecto    = new Color(0f,1f,0.3f,1f);
        drag.colorIncorrecto  = new Color(1f,0.3f,0.2f,1f);
    }

    // Helpers
    private GameObject CrearNodo(string nombre, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin=anchorMin; rt.anchorMax=anchorMax;
        rt.offsetMin=offsetMin; rt.offsetMax=offsetMax;
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
// Drag & Drop
// ================================================================
public class BotonArrastrable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string categoria;
    public Dictionary<string, RectTransform> dropZones;
    public Color colorNormal, colorArrastrando, colorCorrecto, colorIncorrecto;

    private RectTransform _rt;
    private Canvas        _canvas;
    private CanvasGroup   _cg;
    private Transform     _parentOriginal;
    private int           _siblingOriginal;
    private Vector2       _posOriginal;
    private Image         _img;

    void Awake()
    {
        _rt  = GetComponent<RectTransform>();
        _img = GetComponent<Image>();
        _cg  = gameObject.AddComponent<CanvasGroup>();
        // Subir hasta el Canvas raiz
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
            while (!_canvas.isRootCanvas && _canvas.transform.parent != null)
            {
                var parent = _canvas.transform.parent.GetComponentInParent<Canvas>();
                if (parent == null) break;
                _canvas = parent;
            }
    }

    public void OnBeginDrag(PointerEventData e)
    {
        _parentOriginal  = _rt.parent;
        _siblingOriginal = _rt.GetSiblingIndex();
        _posOriginal     = _rt.position;

        _rt.SetParent(_canvas.transform, true);
        _rt.SetAsLastSibling();
        _cg.blocksRaycasts = false;
        if (_img != null) _img.color = colorArrastrando;
    }

    public void OnDrag(PointerEventData e)
    {
        _rt.position += (Vector3)(e.delta);
    }

    public void OnEndDrag(PointerEventData e)
    {
        _cg.blocksRaycasts = true;

        // Buscar ColumnaDropHandler bajo el cursor
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(e, hits);
        ColumnaDropHandler destino = null;
        foreach (var h in hits)
        {
            destino = h.gameObject.GetComponent<ColumnaDropHandler>()
                   ?? h.gameObject.GetComponentInParent<ColumnaDropHandler>();
            if (destino != null) break;
        }

        if (destino != null)
        {
            bool correcto = destino.categoria == categoria;
            _rt.SetParent(destino.contentRT, false);
            _rt.localScale = Vector3.one;

            // En la columna el tamaño lo controla el layout
            var le = GetComponent<LayoutElement>();
            if (le != null) { le.preferredWidth=-1; le.preferredHeight=80; le.minHeight=60; }

            if (_img != null) _img.color = correcto ? colorCorrecto : colorIncorrecto;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(destino.contentRT);
        }
        else
        {
            // Volver al origen
            _rt.SetParent(_parentOriginal, true);
            _rt.SetSiblingIndex(_siblingOriginal);
            _rt.position = _posOriginal;
            if (_img != null) _img.color = colorNormal;
        }
    }
}

// ================================================================
// Receptor de drops
// ================================================================
public class ColumnaDropHandler : MonoBehaviour
{
    public string        categoria;
    public RectTransform contentRT;
}

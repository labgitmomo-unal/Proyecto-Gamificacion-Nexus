using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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
    public float fontSizeMaxBoton = 36f;
    public float fontSizeMinBoton = 14f;

    private CanvasGroup _cg;
    private bool _inicializado = false;
    private Dictionary<string, CategoriaDropHandler> _destinosCategoria;

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
			// Limpiar hijos (botones, titulos, etc.)
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

        // === ZONA SUPERIOR (arriba): titulos de categoria ===
        // Los titulos ocupan la mitad superior del panel (anchorMin.y=0.57)
        var zonaTitulos = CrearNodo("ZonaTitulos", transform,
                              new Vector2(0f,0.57f), new Vector2(1f,1f),
                              new Vector2(6f,6f), new Vector2(-6f,-6f));
        var ztl = zonaTitulos.AddComponent<HorizontalLayoutGroup>();
        ztl.spacing=6; ztl.padding=new RectOffset(6,6,6,6);
        ztl.childControlWidth=true; ztl.childControlHeight=true;
        ztl.childForceExpandWidth=true; ztl.childForceExpandHeight=true;

        _destinosCategoria = new Dictionary<string, CategoriaDropHandler>();
        foreach (var cat in categorias)
        {
            var dh = CrearTituloCategoria(cat, zonaTitulos.transform);
            _destinosCategoria[cat] = dh;
        }

        // === ZONA INFERIOR (abajo): botones mezclados en scroll horizontal ===
        // anchorMin=(0,0), anchorMax=(1,0.57) — ocupa el 57% inferior
        var zonaMix = CrearNodo("ZonaMezclados", transform,
                                new Vector2(0f,0f), new Vector2(1f,0.57f),
                                new Vector2(6f,6f), new Vector2(-6f,-6f));
        CrearImagen("FondoMix", zonaMix.transform, new Color(0f,0.05f,0.12f,0.9f),
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Instruccion
        var tituloGO = CrearNodo("Titulo", zonaMix.transform,
                                 new Vector2(0f,0.90f), new Vector2(1f,1f),
                                 new Vector2(8f,2f), new Vector2(-8f,-2f));
        var tTMP = tituloGO.AddComponent<TextMeshProUGUI>();
        tTMP.text = "Arrastra cada dato a su categoria";
        tTMP.alignment = TextAlignmentOptions.Center;
        tTMP.enableAutoSizing = true; tTMP.fontSizeMin=8; tTMP.fontSizeMax=40;
        tTMP.color = new Color(0.7f,0.9f,1f,0.8f);
        tTMP.fontStyle = FontStyles.Italic;
        tTMP.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // ScrollRect horizontal para los botones mezclados
        var mixScrollGO = CrearNodo("MixScroll", zonaMix.transform,
                                     new Vector2(0f,0f), new Vector2(1f,0.88f),
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

        // Instanciar botones mezclados
        foreach (var item in items)
            CrearBoton(item, mixContentRT);
    }

     private CategoriaDropHandler CrearTituloCategoria(string categoria, Transform parent)
     {
         // ── Header visual ──────────────────────────────────────────────
         var headerGO = new GameObject($"Header_{categoria}");
         headerGO.transform.SetParent(parent, false);
         headerGO.AddComponent<RectTransform>();
         headerGO.AddComponent<Image>().color = new Color(0f,0.15f,0.22f,1f);
         var hLE = headerGO.AddComponent<LayoutElement>(); hLE.minHeight=90; hLE.preferredHeight=90;
 
         // Texto en hijo separado para evitar conflicto Image+TMP en mismo GO
         var hTextoGO = new GameObject("HeaderTexto");
         hTextoGO.transform.SetParent(headerGO.transform, false);
         var hTextoRT = hTextoGO.AddComponent<RectTransform>();
         hTextoRT.anchorMin = Vector2.zero; hTextoRT.anchorMax = Vector2.one;
         hTextoRT.offsetMin = hTextoRT.offsetMax = Vector2.zero;
         var hTMP = hTextoGO.AddComponent<TextMeshProUGUI>();
         hTMP.text = categoria; hTMP.alignment=TextAlignmentOptions.Center;
         hTMP.enableAutoSizing=true; hTMP.fontSizeMin=10; hTMP.fontSizeMax=52;
         hTMP.color=colorHeader; hTMP.fontStyle=FontStyles.Bold;
         hTMP.textWrappingMode = TMPro.TextWrappingModes.Normal;
 
         // DropHandler en el propio header
         var dh = headerGO.AddComponent<CategoriaDropHandler>();
         dh.categoria = categoria;
 
         // ── Sphere trigger para deteccion de zona al soltar ─────────────
         // Hijo aparte para no interferir con el RectTransform del header
         var triggerGO = new GameObject($"Trigger_{categoria}");
         triggerGO.transform.SetParent(headerGO.transform, false);
         var tr = triggerGO.AddComponent<SphereCollider>();
         tr.isTrigger  = true;
         tr.radius     = 0.35f;           // radio en unidades de mundo
         tr.center     = new Vector3(0f, -0.30f, 0.15f);
 
         triggerGO.AddComponent<CategoriaGrabbableZone>();
 
         return dh;
     }

     private void CrearBoton(BotonData datos, RectTransform parent)
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
         rt.sizeDelta = new Vector2(280f, 110f);
 
         var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
         le.preferredWidth=280f; le.preferredHeight=110f; le.minHeight=70f;
 
         // Quitar Button (usaremos XR grab en su lugar)
         var btn = go.GetComponent<Button>();
         if (btn != null) Destroy(btn);
 
         // ── Physics components para XR grab ────────────────────────────
         // Puedes desactivarlos si usas un prefab prefabricado con rig
         var rb = go.GetComponent<Rigidbody>();
         if (rb == null) rb = go.AddComponent<Rigidbody>();
         rb.isKinematic = true;
         rb.useGravity  = false;
 
         var box = go.GetComponent<BoxCollider>();
         if (box == null)
         {
             box = go.AddComponent<BoxCollider>();
             // El tamaño en mundo esta pensado para canvas WorldSpace a escala 0.001
             box.size = new Vector3(0.28f, 0.11f, 0.01f);
             box.center = Vector3.forward * 0.005f;
         }
 
          var xrg = go.GetComponent<XRGrabInteractable>();
          if (xrg == null)
          {
              xrg = go.AddComponent<XRGrabInteractable>();
              xrg.throwVelocityScale        = 1.2f;
              xrg.throwAngularVelocityScale = 0.8f;
              xrg.trackPosition             = true;
              xrg.trackRotation             = false;
              xrg.snapToColliderVolume      = true;
              xrg.matchAttachRotation       = false;
          }
 
          // ── AbductionGrabbable: control de follow + drop zone logic ──────
          var grab = go.GetComponent<AbductionGrabbable>();
          if (grab == null) grab = go.AddComponent<AbductionGrabbable>();
          grab.categoria  = datos.categoria;
          grab.normalColor = colorBoton;
          grab.ponderacionEsUno = Mathf.Approximately(datos.ponderacion, 1f);
 
         // ── Texto ────────────────────────────────────────────────────────
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
    public Dictionary<string, CategoriaDropHandler> destinosCategoria;
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

        // Buscar CategoriaDropHandler bajo el cursor
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(e, hits);
        CategoriaDropHandler destino = null;
        foreach (var h in hits)
        {
            destino = h.gameObject.GetComponent<CategoriaDropHandler>()
                   ?? h.gameObject.GetComponentInParent<CategoriaDropHandler>();
            if (destino != null) break;
        }

        if (destino != null)
        {
            bool correcto = destino.categoria == categoria;
            if (correcto)
            {
                // Categoria correcta: destruir el boton
                Destroy(gameObject);
            }
            else
            {
                // Categoria incorrecta: volver a la zona de botones mezclados
                _rt.SetParent(_parentOriginal, true);
                _rt.SetSiblingIndex(_siblingOriginal);
                _rt.position = _posOriginal;
                if (_img != null) _img.color = colorIncorrecto;

                // Volver al color normal despues de un breve feedback
                StartCoroutine(VolverColorNormal());
            }
        }
        else
        {
            // No se solto sobre ningun titulo: volver al origen
            _rt.SetParent(_parentOriginal, true);
            _rt.SetSiblingIndex(_siblingOriginal);
            _rt.position = _posOriginal;
            if (_img != null) _img.color = colorNormal;
        }
    }

    private System.Collections.IEnumerator VolverColorNormal()
    {
        yield return new WaitForSeconds(0.3f);
        if (_img != null) _img.color = colorNormal;
    }
}

// ================================================================
// Receptor de drops: titulo de categoria
// ================================================================
public class CategoriaDropHandler : MonoBehaviour
{
    public string categoria;
}

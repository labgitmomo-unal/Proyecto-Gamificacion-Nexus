using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class GraphVerticalPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private bool _autoBuild = true;
    [SerializeField] private string _panelTitle = "Nexus";
    [SerializeField] private Vector2 _panelSize = new Vector2(560f, 980f);
    [SerializeField] private Vector3 _localOffset = new Vector3(1.2f, 0.15f, 0f);

    [Header("Mapa")]
    [SerializeField] private Sprite _cityMapSprite;
    [SerializeField] private bool _preserveMapAspect = true;

    [Header("Colores")]
    [SerializeField] private Color _panelBackground   = new Color(0.02f, 0.05f, 0.09f, 0.94f);
    [SerializeField] private Color _mapFallback       = new Color(0.07f, 0.11f, 0.18f, 0.96f);
    [SerializeField] private Color _nodeColor         = new Color(0.25f, 0.75f, 1f,   1f);
    [SerializeField] private Color _nodeOccupiedColor = new Color(1f,   0.62f, 0.2f,  1f);
    [SerializeField] private Color _edgeColor         = new Color(0.52f, 0.82f, 1f,   0.42f);
    [SerializeField] private Color _edgeOccupiedColor = new Color(1f,   0.86f, 0.22f, 0.8f);

    [Header("Nodos")]
    [SerializeField] private float _nodeSize   = 42f;
    [SerializeField] private float _edgeHeight = 6f;
    [SerializeField] private float _mapPadding = 0.12f;

    // ── datos internos ─────────────────────────────────────────────────────
    private readonly List<BaseSlot>                      _slots       = new();
    private readonly List<ConnectionData>                _connections = new();
    private readonly Dictionary<BaseSlot, RectTransform> _nodeBySlot  = new();

    private RectTransform _rootPanel;
    private RectTransform _mapRoot;
    private Sprite        _circleSprite;
    private bool          _building;
    private float         _computedScale = 0.01f;

    private struct ConnectionData
    {
        public BaseSlot slotA;
        public BaseSlot slotB;
        public Image    edgeImage;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Unity callbacks
    // ══════════════════════════════════════════════════════════════════════

    private void OnEnable()
    {
        if (_autoBuild) BuildPanel();
    }

    private void OnValidate()
    {
        _panelSize.x = Mathf.Max(100f, _panelSize.x);
        _panelSize.y = Mathf.Max(100f, _panelSize.y);
        _mapPadding  = Mathf.Clamp(_mapPadding, 0.01f, 0.4f);

        if (_autoBuild && !_building)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => { if (this) BuildPanel(); };
#endif
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        RefreshPanelState();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  API pública
    // ══════════════════════════════════════════════════════════════════════

    public void BuildPanel()
    {
        if (_building) return;
        _building = true;

        try
        {
            CollectGraphData();
            if (_slots.Count == 0) return;

            AutoFitPanelScale();
            EnsureCircleSprite();
            EnsureRootPanel();
            ClearGeneratedUI();
            BuildVisualStructure();
            RefreshPanelState();
        }
        finally { _building = false; }
    }

    public void RefreshPanelState()
    {
        if (_rootPanel == null) return;

        foreach (BaseSlot slot in _slots)
        {
            if (slot == null) continue;
            if (_nodeBySlot.TryGetValue(slot, out RectTransform rt) && rt != null)
            {
                Image img = rt.GetComponent<Image>();
                if (img) img.color = slot.IsOccupied ? _nodeOccupiedColor : _nodeColor;
            }
        }

        for (int i = 0; i < _connections.Count; i++)
        {
            ConnectionData c = _connections[i];
            if (c.edgeImage == null || c.slotA == null || c.slotB == null) continue;
            bool active = c.slotA.IsOccupied && c.slotB.IsOccupied;
            c.edgeImage.color = active ? _edgeOccupiedColor : _edgeColor;
            _connections[i] = c;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Escala automática según spread real del grafo
    // ══════════════════════════════════════════════════════════════════════

    private void AutoFitPanelScale()
    {
        GetBounds(out float minX, out float maxX, out float minZ, out float maxZ);

        float spreadX = maxX - minX;
        float spreadZ = maxZ - minZ;
        float spread  = Mathf.Max(spreadX, spreadZ);

        if (spread < 0.001f) spread = 1f;

        float panelMax = Mathf.Max(_panelSize.x, _panelSize.y);
        _computedScale = spread / panelMax;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Recolección de datos
    // ══════════════════════════════════════════════════════════════════════

    private void CollectGraphData()
    {
        _slots.Clear();
        _connections.Clear();
        _nodeBySlot.Clear();

        _slots.AddRange(GetComponentsInChildren<BaseSlot>(true));

        Transform edgesRoot = transform.Find("Conexiones");
        if (edgesRoot == null) return;

        foreach (Transform child in edgesRoot)
        {
            if (child.GetComponent<LineRenderer>() == null) continue;

            string[] parts = child.name.Split('_');
            if (parts.Length < 3) continue;

            BaseSlot a = FindSlotByName("Base" + parts[1].TrimStart('B'));
            BaseSlot b = FindSlotByName("Base" + parts[2].TrimStart('B'));
            if (a == null || b == null) continue;

            _connections.Add(new ConnectionData { slotA = a, slotB = b });
        }
    }

    private BaseSlot FindSlotByName(string name)
    {
        foreach (BaseSlot s in _slots)
            if (s != null && s.gameObject.name == name) return s;
        return null;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Construcción de UI
    // ══════════════════════════════════════════════════════════════════════

    private void EnsureRootPanel()
    {
        Transform existing = transform.Find("PanelVerticalGrafo");
        if (existing != null)
        {
            _rootPanel = existing as RectTransform;
            if (_rootPanel != null)
            {
                _rootPanel.sizeDelta     = _panelSize;
                _rootPanel.localPosition = _localOffset;
                _rootPanel.localScale    = Vector3.one * _computedScale;
                return;
            }
        }

        GameObject go = new GameObject("PanelVerticalGrafo",
            typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler),  typeof(GraphicRaycaster));

        go.transform.SetParent(transform, false);
        go.transform.localPosition = _localOffset;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one * _computedScale;

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode      = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder    = 100;

        go.GetComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ConstantPixelSize;

        _rootPanel           = go.GetComponent<RectTransform>();
        _rootPanel.sizeDelta = _panelSize;
    }

    private void ClearGeneratedUI()
    {
        if (_rootPanel == null) return;
        for (int i = _rootPanel.childCount - 1; i >= 0; i--)
            DestroySmart(_rootPanel.GetChild(i).gameObject);
        _nodeBySlot.Clear();
        // ⚠️ NO limpiar _connections aquí — ya están llenas con datos
    }

    private void BuildVisualStructure()
    {
        BuildBackground();
        BuildTitle();
        _mapRoot = BuildMapArea();
        BuildOverlay();
    }

    private void BuildBackground()
    {
        var go   = MakeUIObject("Fondo", _rootPanel, typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = _panelBackground;
    }

    private void BuildTitle()
    {
        var go   = MakeUIObject("Titulo", _rootPanel, typeof(TextMeshProUGUI));
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.92f);
        rect.anchorMax = new Vector2(0.92f, 0.98f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text          = _panelTitle;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.fontSize      = 44f;
        tmp.color         = Color.white;
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
    }

    private RectTransform BuildMapArea()
    {
        var go   = MakeUIObject("Mapa", _rootPanel, typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.16f);
        rect.anchorMax = new Vector2(0.92f, 0.90f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        if (_cityMapSprite != null)
        {
            img.sprite         = _cityMapSprite;
            img.color          = Color.white;
            img.preserveAspect = _preserveMapAspect;
            img.type           = Image.Type.Simple;
        }
        else
        {
            img.color = _mapFallback;
        }

        return rect;
    }

    // ── Overlay de nodos y aristas ─────────────────────────────────────────

    private void BuildOverlay()
    {
        if (_mapRoot == null) return;

        GetBounds(out float minX, out float maxX, out float minZ, out float maxZ);

        // Calcula el tamaño del mapa desde anchors × panelSize
        // porque rect.size es (0,0) antes de que Unity procese el layout
        Vector2 mapSize = ComputeMapSize();

        // Aristas primero (quedan detrás de los nodos)
        for (int i = 0; i < _connections.Count; i++)
        {
            ConnectionData c = _connections[i];
            c.edgeImage = CreateEdge(_mapRoot, c.slotA, c.slotB,
                                     mapSize, minX, maxX, minZ, maxZ);
            _connections[i] = c;
        }

        foreach (BaseSlot slot in _slots)
        {
            if (slot == null) continue;
            _nodeBySlot[slot] = CreateNode(_mapRoot, slot,
                                            mapSize, minX, maxX, minZ, maxZ);
        }
    }

    /// <summary>
    /// Calcula el tamaño en píxeles del área del mapa a partir de los anchors
    /// definidos en BuildMapArea, sin depender de rect.size.
    /// </summary>
    private Vector2 ComputeMapSize()
    {
        const float anchorMinX = 0.08f, anchorMaxX = 0.92f;
        const float anchorMinY = 0.16f, anchorMaxY = 0.90f;

        return new Vector2(
            _panelSize.x * (anchorMaxX - anchorMinX),
            _panelSize.y * (anchorMaxY - anchorMinY)
        );
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Creación de nodos y aristas
    // ══════════════════════════════════════════════════════════════════════

    private RectTransform CreateNode(RectTransform parent, BaseSlot slot,
        Vector2 mapSize, float minX, float maxX, float minZ, float maxZ)
    {
        // FIX: posición en espacio local de Grafo (no del padre inmediato del slot)
        Vector3 localPos = transform.InverseTransformPoint(slot.transform.position);

        var go   = MakeUIObject(slot.gameObject.name, parent, typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin        = Vector2.zero;
        rect.anchorMax        = Vector2.zero;
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.sizeDelta        = new Vector2(_nodeSize, _nodeSize);
        rect.anchoredPosition = Project(localPos, mapSize, minX, maxX, minZ, maxZ);

        var img = go.GetComponent<Image>();
        img.sprite        = _circleSprite;
        img.color         = slot.IsOccupied ? _nodeOccupiedColor : _nodeColor;
        img.type          = Image.Type.Simple;
        img.raycastTarget = false;

        // Etiqueta debajo del nodo
        var labelGO   = MakeUIObject("Etiqueta", rect, typeof(TextMeshProUGUI));
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin        = new Vector2(0.5f, 0f);
        labelRect.anchorMax        = new Vector2(0.5f, 0f);
        labelRect.pivot            = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -8f);
        labelRect.sizeDelta        = new Vector2(140f, 30f);

        var tmp = labelGO.GetComponent<TextMeshProUGUI>();
        tmp.text          = slot.gameObject.name;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.fontSize      = 18f;
        tmp.color         = Color.white;
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        return rect;
    }

    private Image CreateEdge(RectTransform parent,
        BaseSlot slotA, BaseSlot slotB,
        Vector2 mapSize, float minX, float maxX, float minZ, float maxZ)
    {
        string name = $"Linea_{slotA.gameObject.name}_{slotB.gameObject.name}";
        var go   = MakeUIObject(name, parent, typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot     = new Vector2(0.5f, 0.5f);

        // FIX: posición en espacio local de Grafo para ambos slots
        Vector3 posA = transform.InverseTransformPoint(slotA.transform.position);
        Vector3 posB = transform.InverseTransformPoint(slotB.transform.position);

        Vector2 pA    = Project(posA, mapSize, minX, maxX, minZ, maxZ);
        Vector2 pB    = Project(posB, mapSize, minX, maxX, minZ, maxZ);
        Vector2 delta = pB - pA;

        rect.anchoredPosition = (pA + pB) * 0.5f;
        rect.sizeDelta        = new Vector2(Mathf.Max(_edgeHeight, delta.magnitude), _edgeHeight);
        rect.localRotation    = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        var img = go.GetComponent<Image>();
        img.color         = _edgeColor;
        img.raycastTarget = false;
        return img;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Proyección y bounds
    // ══════════════════════════════════════════════════════════════════════

    private void GetBounds(out float minX, out float maxX,
                            out float minZ, out float maxZ)
    {
        minX = minZ =  float.MaxValue;
        maxX = maxZ = -float.MaxValue;

        foreach (BaseSlot s in _slots)
        {
            if (s == null) continue;
            // FIX: posición relativa al objeto Grafo
            Vector3 p = transform.InverseTransformPoint(s.transform.position);
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
        }

        if (Mathf.Approximately(minX, maxX)) maxX = minX + 0.001f;
        if (Mathf.Approximately(minZ, maxZ)) maxZ = minZ + 0.001f;
    }

    private Vector2 Project(Vector3 local, Vector2 size,
                             float minX, float maxX,
                             float minZ, float maxZ)
    {
        float nx = Mathf.InverseLerp(minX, maxX, local.x);
        float ny = Mathf.InverseLerp(minZ, maxZ, local.z);

        float mx = size.x * _mapPadding;
        float my = size.y * _mapPadding;

        return new Vector2(
            Mathf.Lerp(-size.x * 0.5f + mx, size.x * 0.5f - mx, nx),
            Mathf.Lerp(-size.y * 0.5f + my, size.y * 0.5f - my, ny)
        );
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Utilidades
    // ══════════════════════════════════════════════════════════════════════

    private void EnsureCircleSprite()
    {
        if (_circleSprite != null) return;

        const int size   = 64;
        const int center = size / 2;
        float     radius = center - 1f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dist  = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
            float alpha = Mathf.Clamp01(radius - dist + 0.5f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }

        tex.SetPixels(pixels);
        tex.Apply();

        _circleSprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size);
    }

    private static GameObject MakeUIObject(string name, Transform parent,
                                            params System.Type[] components)
    {
        var go = new GameObject(name, components);
        go.transform.SetParent(parent, false);
        return go;
    }

    private void DestroySmart(GameObject target)
    {
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}
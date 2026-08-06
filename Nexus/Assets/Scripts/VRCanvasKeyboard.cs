using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class VRCanvasKeyboard : MonoBehaviour
{
    private TMP_InputField activeField;
    private GameObject panel;
    private bool shifted;

    private readonly string[] rowTop = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
    private readonly string[] rowMid = { "A", "S", "D", "F", "G", "H", "J", "K", "L", "Ñ" };
    private readonly string[] rowBot = { "Z", "X", "C", "V", "B", "N", "M" };

    [Header("Tamaño del panel (en metros, relativo al canvas)")]
    [SerializeField] private float panelHeight = 0.45f;
    [SerializeField] private float paddingMeters = 0.015f;

    [Header("Tamaño de teclas")]
    [SerializeField] private float keyWidth = 0.075f;
    [SerializeField] private float keyHeight = 0.07f;
    [SerializeField] private float keyFontSize = 0.04f;
    [SerializeField] private float spacing = 0.005f;

    private void Start()
    {
        Canvas parentCanvas = GetComponent<Canvas>();
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) { enabled = false; return; }

        BuildKeyboard(parentCanvas);
        HookInputFields();
    }

    private void BuildKeyboard(Canvas parentCanvas)
    {
        panel = new GameObject("KeyboardPanel");
        panel.transform.SetParent(parentCanvas.transform, false);

        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0, 0);
        prt.anchorMax = new Vector2(1, 0);
        prt.pivot = new Vector2(0.5f, 0);
        prt.offsetMin = new Vector2(paddingMeters, paddingMeters);
        prt.offsetMax = new Vector2(-paddingMeters, panelHeight);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.LowerCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.spacing = spacing * 2f;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        float letterW = keyWidth;

        MakeRow(panel.transform, rowTop, letterW);
        MakeRow(panel.transform, rowMid, letterW);

        GameObject row3 = MakeRow(panel.transform, rowBot, letterW);
        AddKey(row3.transform, letterW * 1.2f, "^", "Shift");
        AddKey(row3.transform, letterW * 1.4f, "<-", "Backspace");

        GameObject row4 = new GameObject("Row4");
        row4.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup hlg = row4.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = spacing;

        AddKey(row4.transform, letterW * 3f, "Espacio", "Space");
        AddKey(row4.transform, letterW, ".");
        AddKey(row4.transform, letterW, "@");
        AddKey(row4.transform, letterW * 1.8f, "Hecho", "Done");

        panel.SetActive(false);
    }

    private GameObject MakeRow(Transform parent, string[] keys, float keyW)
    {
        GameObject row = new GameObject("Row");
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = spacing;

        foreach (string k in keys)
            AddKey(row.transform, keyW, k);

        return row;
    }

    private void AddKey(Transform parent, float width, string label, string command = null)
    {
        GameObject key = new GameObject("Key_" + (command ?? label));
        key.transform.SetParent(parent, false);

        RectTransform rt = key.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, keyHeight);

        Image img = key.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        Button btn = key.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
        cb.pressedColor = new Color(0.12f, 0.12f, 0.12f);
        cb.selectedColor = img.color;
        btn.colors = cb;

        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(key.transform, false);
        RectTransform lrt = lbl.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;

        TMP_Text tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = keyFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        string cmd = command ?? label;
        btn.onClick.AddListener(() => OnKey(cmd));
    }

    private void HookInputFields()
    {
        var fields = FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var f in fields)
        {
            if (f.gameObject.scene != gameObject.scene) continue;
            TMP_InputField captured = f;
            f.onSelect.AddListener(_ => Show(captured));
        }
    }

    private void Show(TMP_InputField field)
    {
        activeField = field;
        panel.SetActive(true);
    }

    private void Hide()
    {
        panel.SetActive(false);
        activeField = null;
    }

    private void OnKey(string cmd)
    {
        if (activeField == null) return;

        if (cmd == "Done") { Hide(); return; }

        switch (cmd)
        {
            case "Backspace":
                if (activeField.text.Length > 0)
                    activeField.text = activeField.text.Substring(0, activeField.text.Length - 1);
                break;
            case "Shift":
                shifted = !shifted;
                UpdateLabels();
                return;
            case "Space":
                activeField.text += " ";
                break;
            default:
                activeField.text += shifted ? cmd : cmd.ToLower();
                if (shifted) { shifted = false; UpdateLabels(); }
                break;
        }

        activeField.caretPosition = activeField.text.Length;
        EventSystem.current.SetSelectedGameObject(activeField.gameObject);
    }

    private void UpdateLabels()
    {
        foreach (Transform row in panel.transform)
        {
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) continue;
            foreach (Transform key in row)
            {
                TMP_Text tmp = key.GetComponentInChildren<TMP_Text>();
                if (tmp == null) continue;
                string t = tmp.text;
                if (t == "Espacio" || t == "Hecho" || t == "." || t == "@") continue;
                if (t == "<-") { tmp.fontStyle = FontStyles.Bold; continue; }
                if (t == "^") { continue; }
                tmp.text = shifted ? t.ToUpper() : t.ToLower();
            }
        }
    }

    private void OnDestroy()
    {
        if (panel != null) Destroy(panel);
    }
}

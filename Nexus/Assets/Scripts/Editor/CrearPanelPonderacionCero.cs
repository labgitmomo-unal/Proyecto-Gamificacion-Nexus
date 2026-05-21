using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class CrearPanelPonderacionCero : EditorWindow
{
    [MenuItem("Tools/Crear Panel Ponderacion Cero")]
    public static void CrearPanel()
    {
        var canvasGO = GameObject.Find("Nivel_Abstraccion/Table001 (1)/Canvas");
        if (canvasGO == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontro el Canvas en Table001 (1).", "OK");
            return;
        }

        var canvasRT = canvasGO.GetComponent<RectTransform>();

        // Limpiar hijos existentes si los hay
        while (canvasRT.childCount > 0)
            Object.DestroyImmediate(canvasRT.GetChild(0).gameObject);

        // Fondo
        var fondo = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
        fondo.transform.SetParent(canvasRT, false);
        var fondoRT = fondo.GetComponent<RectTransform>();
        fondoRT.anchorMin = Vector2.zero;
        fondoRT.anchorMax = Vector2.one;
        fondoRT.offsetMin = Vector2.zero;
        fondoRT.offsetMax = Vector2.zero;
        fondo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

        // Contenedor columnas
        var colContainer = new GameObject("ColumnasContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        colContainer.transform.SetParent(canvasRT, false);
        var ccRT = colContainer.GetComponent<RectTransform>();
        ccRT.anchorMin = new Vector2(0.02f, 0.15f);
        ccRT.anchorMax = new Vector2(0.98f, 0.92f);

        var hlg = colContainer.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.UpperCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        string[] titulos = { "Flujo entre zonas", "Horas pico", "Capacidad del sistema", "Puntos críticos de congestión" };
        string[] nombresCol = { "Columna_FlujoEntreZonas", "Columna_HorasPico", "Columna_Capacidad", "Columna_PuntosCriticos" };

        for (int i = 0; i < 4; i++)
        {
            var col = new GameObject(nombresCol[i], typeof(RectTransform), typeof(VerticalLayoutGroup));
            col.transform.SetParent(ccRT, false);
            var vlg = col.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var tituloGO = new GameObject("Titulo", typeof(RectTransform), typeof(TextMeshProUGUI));
            tituloGO.transform.SetParent(col.transform, false);
            var tituloRT = tituloGO.GetComponent<RectTransform>();
            tituloRT.sizeDelta = new Vector2(0, 40);
            var tmp = tituloGO.GetComponent<TextMeshProUGUI>();
            tmp.text = titulos[i];
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 16;
            tmp.color = new Color(0f, 1f, 1f, 1f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10;
            tmp.fontSizeMax = 18;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(col.transform, false);
            var cvlg = content.GetComponent<VerticalLayoutGroup>();
            cvlg.padding = new RectOffset(3, 3, 3, 3);
            cvlg.spacing = 5;
            cvlg.childAlignment = TextAnchor.UpperCenter;
            cvlg.childControlWidth = true;
            cvlg.childControlHeight = true;
            cvlg.childForceExpandWidth = true;
            cvlg.childForceExpandHeight = false;
            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // Agregar script PanelPonderacionCero al Canvas
        var script = canvasGO.GetComponent<PanelPonderacionCero>();
        if (script == null)
            script = canvasGO.AddComponent<PanelPonderacionCero>();

        var canvasGroup = canvasGO.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            script.panelCanvasGroup = canvasGroup;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        script.columnaFlujo = FindChild(canvasGO.transform, "ColumnasContainer/Columna_FlujoEntreZonas/Content").GetComponent<RectTransform>();
        script.columnaHorasPico = FindChild(canvasGO.transform, "ColumnasContainer/Columna_HorasPico/Content").GetComponent<RectTransform>();
        script.columnaCapacidad = FindChild(canvasGO.transform, "ColumnasContainer/Columna_Capacidad/Content").GetComponent<RectTransform>();
        script.columnaPuntosCriticos = FindChild(canvasGO.transform, "ColumnasContainer/Columna_PuntosCriticos/Content").GetComponent<RectTransform>();

        // Cargar prefab del boton
        string prefabPath = "Assets/Prefabs/Button.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
            script.buttonPrefab = prefab;

        canvasGO.SetActive(false);

        Undo.RegisterCreatedObjectUndo(canvasGO, "Crear Panel Ponderacion Cero");
        Selection.activeGameObject = canvasGO;

        Debug.Log("[CrearPanelPonderacionCero] Estructura creada en Canvas de Table001 (1).");
        EditorUtility.DisplayDialog("Exito", "Columnas creadas en el Canvas de Table001 (1).\nScript PanelPonderacionCero agregado al Canvas.", "OK");
    }

    private static Transform FindChild(Transform parent, string path)
    {
        string[] parts = path.Split('/');
        Transform current = parent;
        foreach (var part in parts)
        {
            current = current.Find(part);
            if (current == null) return null;
        }
        return current;
    }
}

using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class ControlsHints_Builder
{
    private const int RenderLayer = 31;
    private const int ImgSize = 1024;

    private static string[] ButtonKeys = { "Trigger", "Bumper", "ThumbStick", "Button_A", "Button_B", "Button_Home" };
    private static string[] ButtonLabels = { "Gatillo", "Grip", "Stick", "A", "B", "Home" };

    private static Dictionary<string, string[]> FunctionByHand = new Dictionary<string, string[]>
    {
        { "Left", new string[] { "Interactuar", "Agarre", "Mover", "Saltar", "Girar", "Pausa" } },
        { "Right", new string[] { "Activar", "Agarre", "Rotar", "Saltar", "Menu", "Pausa" } }
    };

    [MenuItem("Tools/Controles/Construir Panel de Controles")]
    public static void BuildFromMenu()
    {
        Build();
    }

    public static void Build()
    {
        GameObject xr = GameObject.Find("XR Origin (XR Rig)");
        if (xr == null)
        {
            Debug.LogError("[ControlsHints_Builder] No existe 'XR Origin (XR Rig)' en la escena.");
            return;
        }

RemoveOldHintRig(xr);
        DestroyExistingPanel();
        EnsureTextureFolder();

        Camera vrCamera = null;
        Transform camOffset = null;
        Transform leftVisual = null;
        Transform rightVisual = null;
        foreach (Transform child in xr.transform)
        {
            if (child.name == "Camera Offset")
                camOffset = child;
        }
        if (camOffset != null)
        {
            foreach (Transform child in camOffset)
            {
                if (child.name == "Left Controller") leftVisual = FindVisual(child, "Left Controller Visual");
                else if (child.name == "Right Controller") rightVisual = FindVisual(child, "Right Controller Visual");
                else if (child.name == "Main Camera")
                {
                    Camera c = child.GetComponent<Camera>();
                    if (c != null) vrCamera = c;
                }
            }
        }

        if (vrCamera == null) vrCamera = Camera.main;
        if (leftVisual == null || rightVisual == null)
        {
            Debug.LogError("[ControlsHints_Builder] No se encontraron los Controller Visual de ambas manos.");
            return;
        }

        Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        Dictionary<string, Dictionary<string, Vector2>> uvsByHand = new Dictionary<string, Dictionary<string, Vector2>>();

        RenderAndSave(leftVisual, "LeftController", sprites, uvsByHand, "Left");
        RenderAndSave(rightVisual, "RightController", sprites, uvsByHand, "Right");

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        GameObject panel = BuildPanel(font, sprites, uvsByHand, vrCamera);

        panel.AddComponent<Controls_Panel_UI>();
        var ctrl = panel.GetComponent<Controls_Panel_UI>();
        ctrl.panel = panel;
        ctrl.targetCamera = vrCamera;
        ctrl.debugSimulateKeyB = true;

        panel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(panel.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ControlsHints_Builder] Panel de controles construido. Actívalo con la B del mando derecho (o tecla B en editor).");
    }

    private static Transform FindVisual(Transform hand, string name)
    {
        foreach (Transform child in hand)
            if (child.name == name)
                return child;
        return null;
    }

private static void DestroyExistingPanel()
    {
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t != null && t.name == "ControlsPanel_Hints" && UnityEditor.AssetDatabase.GetAssetPath(t.gameObject) == "")
                Object.DestroyImmediate(t.gameObject);
        }
    }

    private static void RemoveOldHintRig(GameObject xr)
    {
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null || !t.name.StartsWith("HintAnchor_"))
                continue;
            if (AssetDatabase.GetAssetPath(t.gameObject) != "")
                continue;
            Object.DestroyImmediate(t.gameObject);
        }

        foreach (Component c in xr.GetComponents<Component>())
        {
            if (c != null && c.GetType().Name == "Controller_Button_Hints")
                Object.DestroyImmediate(c);
        }
    }

    private static void EnsureTextureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            AssetDatabase.CreateFolder("Assets", "Textures");
        if (!AssetDatabase.IsValidFolder("Assets/Textures/ControllerHints"))
            AssetDatabase.CreateFolder("Assets/Textures", "ControllerHints");
    }

    private static void RenderAndSave(Transform visual, string fileName, Dictionary<string, Sprite> sprites,
        Dictionary<string, Dictionary<string, Vector2>> uvsByHand, string hand)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogError("[ControlsHints_Builder] " + hand + ": sin renderers bajo " + visual.name);
            return;
        }

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

        Vector3 padC = MeshCenter(FindChild(visual, "TouchPad"));
        Vector3 stickC = MeshCenter(FindChild(visual, "ThumbStick"));
        Vector3 trigC = MeshCenter(FindChild(visual, "Trigger"));

        Vector3 normal = Vector3.up;
        Transform tPad = FindChild(visual, "TouchPad");
        if (tPad != null)
        {
            normal = tPad.up;
            if (Vector3.Dot(normal, stickC - padC) < 0f)
                normal = -normal;
        }

        Vector3 front = trigC - padC;
        front = (front - Vector3.Dot(front, normal) * normal).normalized;
        if (front.sqrMagnitude < 0.5f)
            front = -visual.forward;

        float topTilt = 0.25f;
        Vector3 dir = (normal + front * topTilt).normalized;
        float orthoSize = b.size.magnitude * 0.72f;
        Vector3 target = b.center;
        float dist = b.size.magnitude * 2.5f;
        Vector3 camPos = target + dir * dist;

        List<KeyValuePair<GameObject, int>> savedLayers = new List<KeyValuePair<GameObject, int>>();
        foreach (Renderer r in renderers)
        {
            if (r.gameObject.layer != RenderLayer)
            {
                savedLayers.Add(new KeyValuePair<GameObject, int>(r.gameObject, r.gameObject.layer));
                r.gameObject.layer = RenderLayer;
            }
        }

        GameObject camGO = new GameObject("__TempControllerCam");
        camGO.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(target - camPos, front));
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = dist * 2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.06f, 0.1f, 1f);
        cam.cullingMask = 1 << RenderLayer;
        cam.targetTexture = null;

        GameObject keyLight = new GameObject("__TempKeyLight");
        keyLight.transform.SetParent(camGO.transform, false);
        keyLight.transform.localRotation = Quaternion.Euler(-20f, 30f, 0f);
        Light kL = keyLight.AddComponent<Light>();
        kL.type = LightType.Directional;
        kL.intensity = 1.4f;
        kL.color = new Color(1f, 0.98f, 0.9f);

        GameObject fillLight = new GameObject("__TempFillLight");
        fillLight.transform.SetParent(camGO.transform, false);
        fillLight.transform.localRotation = Quaternion.Euler(20f, -60f, 0f);
        Light fL = fillLight.AddComponent<Light>();
        fL.type = LightType.Directional;
        fL.intensity = 0.45f;
        fL.color = new Color(0.7f, 0.85f, 1f);

        RenderTexture rt = new RenderTexture(ImgSize, ImgSize, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;

        Dictionary<string, Vector2> uvs = new Dictionary<string, Vector2>();
        foreach (string key in ButtonKeys)
        {
            Transform mesh = FindChild(visual, key);
            if (mesh == null) continue;
            Renderer m = mesh.GetComponent<Renderer>();
            Vector3 c = m != null ? m.bounds.center : mesh.position;
            Vector3 vp = cam.WorldToViewportPoint(c);
            uvs[key] = new Vector2(vp.x, 1f - vp.y);
        }

        cam.Render();

        Texture2D tex = new Texture2D(ImgSize, ImgSize, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, ImgSize, ImgSize), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        string path = "Assets/Textures/ControllerHints/" + fileName + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());

        AssetDatabase.ImportAsset(path);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.maxTextureSize = ImgSize;
        ti.SaveAndReimport();

        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        sprites[hand] = sp;
        uvsByHand[hand] = uvs;

        Object.DestroyImmediate(camGO);
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);

        foreach (KeyValuePair<GameObject, int> kv in savedLayers)
        {
            if (kv.Key != null) kv.Key.layer = kv.Value;
        }

        Debug.Log("[ControlsHints_Builder] Render " + hand + " -> " + path + " (sprite: " + (sp != null) + ")");
    }

private static Vector3 MeshCenter(Transform t)
    {
        if (t == null) return Vector3.zero;
        Renderer r = t.GetComponent<Renderer>();
        return r != null ? r.bounds.center : t.position;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == childName)
                return child;
        return null;
    }

    private static GameObject BuildPanel(TMP_FontAsset font, Dictionary<string, Sprite> sprites,
        Dictionary<string, Dictionary<string, Vector2>> uvsByHand, Camera vrCamera)
    {
        GameObject panel = new GameObject("ControlsPanel_Hints");
        panel.transform.position = vrCamera != null ? vrCamera.transform.position + vrCamera.transform.forward * 1.35f : Vector3.zero;
        panel.transform.localScale = Vector3.one * 0.0011f;

        Canvas canvas = panel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = panel.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 8;
        scaler.referencePixelsPerUnit = 100;

        float panelW = 1500f;
        float panelH = 980f;

        RectTransform bgRt = AddImage(panel.transform, "Fondo", "bg", new Vector2(panelW, panelH), Vector2.zero,
            new Color(0.02f, 0.03f, 0.06f, 0.92f), null);

        AddText(font, bgRt, "Titulo", "CONTROLES", new Vector2(400, 70), new Vector2(0, panelH * 0.5f - 70f),
            58, new Color(0.92f, 0.96f, 1f), FontStyles.Bold);
        AddText(font, bgRt, "Subtitulo", "Pulsa B (mando derecho) para cerrar", new Vector2(400, 40),
            new Vector2(0, panelH * 0.5f - 130f), 26, new Color(0.75f, 0.82f, 0.92f), FontStyles.Normal);

        BuildHandColumn(font, sprites, uvsByHand, bgRt, "Left", "MANO IZQUIERDA", -390f, new Vector2(650, 840));
        BuildHandColumn(font, sprites, uvsByHand, bgRt, "Right", "MANO DERECHA", 390f, new Vector2(650, 840));

        return panel;
    }

    private static void BuildHandColumn(TMP_FontAsset font, Dictionary<string, Sprite> sprites,
        Dictionary<string, Dictionary<string, Vector2>> uvsByHand, Transform parent, string hand,
        string title, float x, Vector2 size)
    {
        RectTransform col = AddImage(parent, "Columna_" + hand, "col", size, new Vector2(x, -20f),
            new Color(0.035f, 0.05f, 0.095f, 0.95f), null);

        AddText(font, col, "TituloCol", title, new Vector2(size.x, 44), new Vector2(0, size.y * 0.5f - 60f),
            32, new Color(0.85f, 0.9f, 1f), FontStyles.Bold);

        float imgSize = 580f;
        RectTransform imgRt = AddImage(col, "Img_" + hand, "img", new Vector2(imgSize, imgSize), new Vector2(0, -8f),
            Color.white, sprites[hand]);

        Dictionary<string, Vector2> uvs = uvsByHand[hand];
        string[] funcs = FunctionByHand[hand];
        string[] legendRows = new string[ButtonKeys.Length];

        Vector2[] badgePositions = new Vector2[ButtonKeys.Length];
        bool[] hasUv = new bool[ButtonKeys.Length];
        for (int i = 0; i < ButtonKeys.Length; i++)
        {
            hasUv[i] = uvs.ContainsKey(ButtonKeys[i]);
            if (!hasUv[i]) continue;
            Vector2 uv = uvs[ButtonKeys[i]];
            badgePositions[i] = new Vector2((uv.x - 0.5f) * imgSize, (0.5f - uv.y) * imgSize);
            legendRows[i] = (i + 1) + "  " + ButtonLabels[i] + " -> " + funcs[i];
        }

        const float badgeSep = 28f;
        for (int iter = 0; iter < 6; iter++)
        {
            for (int i = 0; i < ButtonKeys.Length; i++)
            {
                if (!hasUv[i]) continue;
                for (int j = i + 1; j < ButtonKeys.Length; j++)
                {
                    if (!hasUv[j]) continue;
                    Vector2 diff = badgePositions[j] - badgePositions[i];
                    float overlapX = badgeSep - Mathf.Abs(diff.x);
                    float overlapY = badgeSep - Mathf.Abs(diff.y);
                    if (overlapX <= 0.0001f && overlapY <= 0.0001f) continue;
                    Vector2 push = Vector2.zero;
                    if (overlapX > 0.0001f && (overlapY <= 0.0001f || overlapX <= overlapY) && Mathf.Abs(diff.x) > 0.0001f)
                        push = new Vector2(Mathf.Sign(diff.x) * overlapX, 0f);
                    else if (Mathf.Abs(diff.y) > 0.0001f)
                        push = new Vector2(0f, Mathf.Sign(diff.y) * overlapY);
                    else if (Mathf.Abs(diff.x) > 0.0001f)
                        push = new Vector2(Mathf.Sign(diff.x) * overlapX, 0f);
                    badgePositions[i] -= push * 0.5f;
                    badgePositions[j] += push * 0.5f;
                }
            }
        }

        for (int i = 0; i < ButtonKeys.Length; i++)
        {
            if (!hasUv[i]) continue;
            AddBadge(font, imgRt, (i + 1).ToString(), ButtonLabels[i], badgePositions[i], imgSize);
        }

        RectTransform legend = AddImage(col, "Leyenda_" + hand, "legend", new Vector2(600, 120), new Vector2(0, imgSize * 0.5f + 46f),
            new Color(0.02f, 0.03f, 0.07f, 0.6f), null);
        for (int i = 0; i < legendRows.Length; i++)
        {
            int row = i % 3;
            int colIdx = i / 3;
            float cellY = 40f - row * 40f;
            float cellX = colIdx == 0 ? -150f : 150f;
            RectTransform cell = AddImage(legend, "Fila" + (i + 1) + "_" + ButtonLabels[i], "row", new Vector2(290, 38), new Vector2(cellX, cellY),
                new Color(0.06f, 0.1f, 0.2f, 0.9f), null);
            AddText(font, cell, "Text", legendRows[i] ?? "", new Vector2(290, 38), Vector2.zero, 26,
                new Color(0.9f, 0.95f, 1f), FontStyles.Bold);
        }
    }

    private static void AddBadge(TMP_FontAsset font, RectTransform parent, string number, string buttonLabel, Vector2 pos, float imgSize)
    {
        float half = imgSize * 0.5f;
        Vector2 clamped = new Vector2(
            Mathf.Clamp(pos.x, -half + 20f, half - 20f),
            Mathf.Clamp(pos.y, -half + 20f, half - 20f));

        RectTransform badge = AddImage(parent, "Badge_" + buttonLabel, "badge", new Vector2(26, 26), clamped,
            new Color(0.85f, 0.32f, 0.42f, 0.96f), null);
        AddText(font, badge, "Text", number, new Vector2(26, 26), Vector2.zero, 16, Color.white, FontStyles.Bold);
    }

    private static RectTransform AddImage(Transform parent, string name, string tag, Vector2 size,
        Vector2 pos, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = go.AddComponent<Image>();
        img.color = color;
        if (sprite != null) img.sprite = sprite;
        return rt;
    }

    private static RectTransform AddText(TMP_FontAsset font, RectTransform parent, string name, string text,
        Vector2 size, Vector2 pos, float fontSize, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        return rt;
    }
}
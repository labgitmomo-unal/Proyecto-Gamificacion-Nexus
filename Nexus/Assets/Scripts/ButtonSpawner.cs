using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

/// <summary>
/// FIX DEFINITIVO: Cubre los tres casos posibles:
///   A) Descarga ya termino antes de cargar esta escena (DataReady=true) -> genera inmediato
///   B) Descarga termina despues de OnEnable                             -> HandleDataLoaded genera
///   C) Panel se reactiva (OnEnable de nuevo)                           -> siempre relee el disco
/// </summary>
public class ButtonSpawner : MonoBehaviour
{
    [Header("Prefab y contenedor")]
    public GameObject buttonPrefab;
    public Transform  panel;

    [Header("Texto")]
    [Tooltip("Tamaño maximo del texto")]
    public float fontSizeMax = 40f;
    [Tooltip("Tamaño minimo del texto (auto-size)")]
    public float fontSizeMin = 8f;

    private ScrollRect _scrollRect;

    void Awake()
    {
        if (panel != null)
            _scrollRect = panel.GetComponentInParent<ScrollRect>();
    }

    void OnEnable()
    {
        // Suscribirse PRIMERO para no perder el evento
        DriveDataLoader.OnDataLoaded += HandleDataLoaded;

        // CASO A: La descarga ya termino (DataReady=true) O el archivo existe del run anterior
        // En ambos casos generamos los botones de inmediato con lo que hay en disco.
        if (DriveDataLoader.DataReady || DriveDataLoader.HasLocalData())
        {
            string json = DriveDataLoader.ReadLocalJson();
            if (json != null)
            {
                SpawnBotones(json);
                ResetScroll();
            }
            // Quedarse suscrito por si RefreshData() se llama durante la sesion
            return;
        }

        // CASO B: No hay nada aun. HandleDataLoaded() generara cuando termine la descarga.
        Debug.Log("[ButtonSpawner] Esperando descarga de DriveDataLoader...");
    }

    void OnDisable()
    {
        DriveDataLoader.OnDataLoaded -= HandleDataLoaded;
    }

    // CASO B: llega cuando DriveDataLoader termina de guardar el JSON nuevo
    private void HandleDataLoaded()
    {
        Debug.Log("[ButtonSpawner] JSON actualizado recibido, regenerando botones...");
        string json = DriveDataLoader.ReadLocalJson();
        if (json != null)
        {
            SpawnBotones(json);
            ResetScroll();
        }
    }

    private void ResetScroll()
    {
        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 1f;
    }

    private Transform AutoResolvePanel()
    {
        var sr = GetComponentInParent<ScrollRect>();
        if (sr != null && sr.content != null) return sr.content;
        var t = transform.Find("Panel");
        if (t != null) return t;
        return null;
    }

    private void SpawnBotones(string json)
    {
        BotonDataList lista;
        try { lista = JsonUtility.FromJson<BotonDataList>(json); }
        catch (Exception e)
        {
            Debug.LogError($"[ButtonSpawner] Error al parsear JSON: {e.Message}");
            return;
        }

        if (lista == null || lista.botones == null || lista.botones.Count == 0)
        {
            Debug.LogWarning("[ButtonSpawner] JSON vacio o sin botones.");
            return;
        }

        // Robustez: si 'panel' no está asignado en el inspector (p.ej. al restaurar
        // la escena tras un merge), intentar resolverlo automáticamente.
        if (panel == null)
        {
            panel = AutoResolvePanel();
            if (panel != null && _scrollRect == null)
                _scrollRect = panel.GetComponentInParent<ScrollRect>();
        }
        if (panel == null)
        {
            Debug.LogError("[ButtonSpawner] 'panel' no está asignado. Arrastra el " +
                           "Transform del contenedor de botones al slot 'panel' del " +
                           "ButtonSpawner en el inspector.");
            return;
        }

        for (int i = panel.childCount - 1; i >= 0; i--)
            Destroy(panel.GetChild(i).gameObject);

        // Mezclar lista aleatoriamente (Fisher-Yates)
        for (int i = lista.botones.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = lista.botones[i];
            lista.botones[i] = lista.botones[j];
            lista.botones[j] = temp;
        }

        for (int i = 0; i < lista.botones.Count; i++)
        {
            GameObject instancia = Instantiate(buttonPrefab, panel);
            instancia.transform.localScale = Vector3.one;
            ConfigurarBoton(instancia, lista.botones[i], i);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());

        // Contar botones con ponderacion 1.0 y notificar al panel de progreso
        int totalObjetivo = 0;
        foreach (var b in lista.botones)
            if (Mathf.Approximately(b.ponderacion, 1f)) totalObjetivo++;

        var progreso = UnityEngine.Object.FindObjectOfType<ProgresoAbstraccion>();
        if (progreso != null)
            progreso.InicializarConTotal(totalObjetivo);
        else
            Debug.LogWarning("[ButtonSpawner] ProgresoAbstraccion no encontrado en escena.");

        Debug.Log($"[ButtonSpawner] {lista.botones.Count} botones instanciados. Objetivos (pond=1): {totalObjetivo}");
    }

    private void ConfigurarBoton(GameObject instancia, BotonData datos, int index)
    {
        Color color = Color.white;

        TextMeshProUGUI tmpText = instancia.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text               = datos.texto;
            tmpText.color              = color;
            tmpText.enableWordWrapping = true;
            tmpText.overflowMode       = TextOverflowModes.Truncate;
            tmpText.enableAutoSizing   = true;
            tmpText.fontSizeMin        = fontSizeMin;
            tmpText.fontSizeMax        = fontSizeMax;
            tmpText.alignment          = TextAlignmentOptions.Center;
        }
        else
        {
            Text legacyText = instancia.GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                legacyText.text                 = datos.texto;
                legacyText.color                = color;
                legacyText.resizeTextForBestFit = true;
                legacyText.resizeTextMinSize    = (int)fontSizeMin;
                legacyText.resizeTextMaxSize    = (int)fontSizeMax;
                legacyText.horizontalOverflow   = HorizontalWrapMode.Wrap;
                legacyText.verticalOverflow     = VerticalWrapMode.Truncate;
                legacyText.alignment            = TextAnchor.MiddleCenter;
            }
        }

        Button btn = instancia.GetComponent<Button>();
        if (btn != null)
        {
            float ponderacion = datos.ponderacion;
            btn.onClick.AddListener(() => { ClickBoton(index, ponderacion); Destroy(instancia); });
        }
        else Debug.LogWarning("[ButtonSpawner] El prefab no tiene componente Button.");
    }

    private void ClickBoton(int i, float ponderacion)
    {
        // Si el scroll esta bloqueado (100% alcanzado), no procesar mas eliminaciones
        if (_scrollRect != null && !_scrollRect.enabled) return;

        Debug.Log($"[ButtonSpawner] Click en boton {i} | Ponderacion: {ponderacion}");

        if (Mathf.Approximately(ponderacion, 1f))
            ProgresoAbstraccion.NotificarEliminacion();
    }
}
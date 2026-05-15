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

    [Header("Visual por ponderacion")]
    public Color colorAlta = new Color(0f, 1f, 1f, 1f);
    public Color colorBaja = new Color(1f, 1f, 1f, 0.55f);
    [Tooltip("Ponderacion minima para usar colorAlta")]
    public float umbralAlta = 0.75f;

    [Header("Texto")]
    [Tooltip("Tamaño maximo del texto")]
    public float fontSizeMax = 24f;
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

        for (int i = panel.childCount - 1; i >= 0; i--)
            Destroy(panel.GetChild(i).gameObject);

        for (int i = 0; i < lista.botones.Count; i++)
        {
            GameObject instancia = Instantiate(buttonPrefab, panel);
            instancia.transform.localScale = Vector3.one;
            ConfigurarBoton(instancia, lista.botones[i], i);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
        Debug.Log($"[ButtonSpawner] {lista.botones.Count} botones instanciados.");
    }

    private void ConfigurarBoton(GameObject instancia, BotonData datos, int index)
    {
        Color color = datos.ponderacion >= umbralAlta ? colorAlta : colorBaja;

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
        => Debug.Log($"[ButtonSpawner] Click en boton {i} | Ponderacion: {ponderacion}");
}
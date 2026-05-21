using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cuando la fase de abstraccion se completa (100%), este script muestra
/// un canvas con los botones de ponderacion 0 organizados en 4 columnas tematicas.
/// Los botones se mezclan aleatoriamente antes de distribuirse.
/// </summary>
public class PanelPonderacionCero : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("CanvasGroup del canvas para fade")]
    public CanvasGroup panelCanvasGroup;

    [Tooltip("Contenedores Content de cada columna")]
    public RectTransform columnaFlujo;
    public RectTransform columnaHorasPico;
    public RectTransform columnaCapacidad;
    public RectTransform columnaPuntosCriticos;

    [Header("Prefab y visual")]
    public GameObject buttonPrefab;

    [Tooltip("Color para los botones de ponderacion 0")]
    public Color colorBoton = new Color(0.5f, 0.5f, 0.55f, 1f);

    [Header("Texto")]
    public float fontSizeMax = 18f;
    public float fontSizeMin = 8f;

    private bool _faseCompletada = false;

    void OnEnable()
    {
        ProgresoAbstraccion.OnFaseCompletada += HandleFaseCompletada;
    }

    void OnDisable()
    {
        ProgresoAbstraccion.OnFaseCompletada -= HandleFaseCompletada;
    }

    private void HandleFaseCompletada()
    {
        if (_faseCompletada) return;
        _faseCompletada = true;

        string json = DriveDataLoader.ReadLocalJson();
        if (json == null)
        {
            Debug.LogWarning("[PanelPonderacionCero] No hay JSON disponible.");
            return;
        }

        BotonDataList lista;
        try { lista = JsonUtility.FromJson<BotonDataList>(json); }
        catch (Exception e)
        {
            Debug.LogError($"[PanelPonderacionCero] Error al parsear JSON: {e.Message}");
            return;
        }

        if (lista == null || lista.botones == null) return;

        // Filtrar botones con ponderacion == 0
        List<BotonData> ponderacionCero = new List<BotonData>();
        foreach (var b in lista.botones)
            if (Mathf.Approximately(b.ponderacion, 0f))
                ponderacionCero.Add(b);

        if (ponderacionCero.Count == 0)
        {
            Debug.Log("[PanelPonderacionCero] No hay botones con ponderacion 0.");
            return;
        }

        // Mezclar aleatoriamente (Fisher-Yates)
        for (int i = ponderacionCero.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = ponderacionCero[i];
            ponderacionCero[i] = ponderacionCero[j];
            ponderacionCero[j] = temp;
        }

        // Distribuir en 4 columnas (round-robin)
        RectTransform[] columnas = { columnaFlujo, columnaHorasPico, columnaCapacidad, columnaPuntosCriticos };
        for (int i = 0; i < ponderacionCero.Count; i++)
        {
            RectTransform columna = columnas[i % 4];
            if (columna == null) continue;

            GameObject instancia = Instantiate(buttonPrefab, columna);
            instancia.transform.localScale = Vector3.one;
            ConfigurarBoton(instancia, ponderacionCero[i]);
        }

        // Activar el canvas con fade
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.gameObject.SetActive(true);
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
            StartCoroutine(FadeIn(0.5f));
        }

        Canvas.ForceUpdateCanvases();
        foreach (var col in columnas)
            if (col != null) LayoutRebuilder.ForceRebuildLayoutImmediate(col);

        Debug.Log($"[PanelPonderacionCero] {ponderacionCero.Count} botones de ponderacion 0 distribuidos aleatoriamente en 4 columnas.");
    }

    private void ConfigurarBoton(GameObject instancia, BotonData datos)
    {
        TextMeshProUGUI tmpText = instancia.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text               = datos.texto;
            tmpText.color              = colorBoton;
            tmpText.enableWordWrapping = true;
            tmpText.overflowMode       = TextOverflowModes.Truncate;
            tmpText.enableAutoSizing   = true;
            tmpText.fontSizeMin        = fontSizeMin;
            tmpText.fontSizeMax        = fontSizeMax;
            tmpText.alignment          = TextAlignmentOptions.Center;
        }

        // Desactivar interaccion (solo informativo)
        Button btn = instancia.GetComponent<Button>();
        if (btn != null) btn.interactable = false;
    }

    private System.Collections.IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;
    }
}

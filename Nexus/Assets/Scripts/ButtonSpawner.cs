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
/// Colocar en la escena del RETO junto al panel holografico.
/// Lee el JSON local guardado previamente por DriveDataLoader.
/// La URL de Drive se maneja unicamente en DriveDataLoader.
///
/// En el Inspector:
///   - buttonPrefab -> prefab del boton (debe tener Button + TextMeshProUGUI)
///   - panel        -> Transform del contenedor donde se instancian los botones
///   - colorAlta    -> color para botones con ponderacion >= umbralAlta
///   - colorBaja    -> color para botones con ponderacion < umbralAlta
///   - umbralAlta   -> ponderacion minima para considerar un boton como relevante
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
    public float umbralAlta = 2f;

    // ---------------------------------------------------------------

    void Start()
    {
        string json = DriveDataLoader.ReadLocalJson();

        if (json == null)
        {
            Debug.LogError("[ButtonSpawner] No hay datos locales. Asegurate de que DriveDataLoader haya descargado el JSON desde el menu.");
            return;
        }

        SpawnBotones(json);
    }

    // ---------------------------------------------------------------

    private void SpawnBotones(string json)
    {
        BotonDataList lista;
        try
        {
            lista = JsonUtility.FromJson<BotonDataList>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ButtonSpawner] Error al parsear el JSON: {e.Message}");
            return;
        }

        if (lista == null || lista.botones == null || lista.botones.Count == 0)
        {
            Debug.LogWarning("[ButtonSpawner] El JSON no contiene botones.");
            return;
        }

        // Limpiar instancias previas si las hubiera
        foreach (Transform child in panel)
            Destroy(child.gameObject);

        for (int i = 0; i < lista.botones.Count; i++)
        {
            BotonData datos = lista.botones[i];
            GameObject instancia = Instantiate(buttonPrefab, panel);
            ConfigurarBoton(instancia, datos, i);
            Debug.Log($"[ButtonSpawner] Boton {i} creado | Texto: {datos.texto} | Ponderacion: {datos.ponderacion}");
        }

        Debug.Log($"[ButtonSpawner] {lista.botones.Count} botones instanciados correctamente.");
    }

    private void ConfigurarBoton(GameObject instancia, BotonData datos, int index)
    {
        Color color = datos.ponderacion >= umbralAlta ? colorAlta : colorBaja;

        // Soporta TextMeshPro y Text legacy
        TextMeshProUGUI tmpText = instancia.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text  = datos.texto;
            tmpText.color = color;
        }
        else
        {
            Text legacyText = instancia.GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                legacyText.text  = datos.texto;
                legacyText.color = color;
            }
        }

        // Click: ejecuta logica y destruye el boton
        Button btn = instancia.GetComponent<Button>();
        if (btn != null)
        {
            float ponderacion = datos.ponderacion;
            btn.onClick.AddListener(() =>
            {
                ClickBoton(index, ponderacion);
                Destroy(instancia);
            });
        }
        else
        {
            Debug.LogWarning("[ButtonSpawner] El prefab no tiene componente Button.");
        }
    }

    private void ClickBoton(int i, float ponderacion)
    {
        Debug.Log($"[ButtonSpawner] Click en boton {i} | Ponderacion: {ponderacion}");
        // Aqui va la logica del reto cuando se amplie la mecanica
    }
}

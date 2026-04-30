using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
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
///
/// Siempre descarga el JSON fresco desde Drive (funciona en Editor y en APK).
/// Si la descarga falla, usa la copia local "variables_abstraccion.json" como respaldo.
///
/// En el Inspector:
///   - drivePublicUrl  -> la misma URL publica del JSON en Drive
///   - buttonPrefab    -> prefab del boton (debe tener Button + TextMeshProUGUI)
///   - panel           -> Transform del contenedor donde se instancian los botones
///   - colorAlta       -> color para botones con ponderacion >= umbralAlta
///   - colorBaja       -> color para botones con ponderacion < umbralAlta
///   - umbralAlta      -> ponderacion minima para considerar un boton como relevante
/// </summary>
public class ButtonSpawner : MonoBehaviour
{
    [Header("Google Drive")]
    [Tooltip("URL publica del JSON en Drive. Se descarga siempre al iniciar el reto.")]
    public string drivePublicUrl = "";

    [Header("Prefab y contenedor")]
    public GameObject buttonPrefab;
    public Transform  panel;

    [Header("Visual por ponderacion")]
    public Color colorAlta = new Color(0f, 1f, 1f, 1f);       // cyan  — alta relevancia
    public Color colorBaja = new Color(1f, 1f, 1f, 0.55f);    // blanco tenue — baja relevancia
    [Tooltip("Ponderacion minima para usar colorAlta")]
    public float umbralAlta = 2f;

    void Start()
    {
        StartCoroutine(CargarYSpawnear());
    }

    private IEnumerator CargarYSpawnear()
    {
        // Sin URL configurada: usar datos locales directamente
        if (string.IsNullOrEmpty(drivePublicUrl))
        {
            Debug.LogWarning("[ButtonSpawner] drivePublicUrl vacia. Intentando datos locales...");
            LeerLocalYSpawnear();
            yield break;
        }

        string downloadUrl = ConvertToDirectDownloadUrl(drivePublicUrl);
        Debug.Log($"[ButtonSpawner] Descargando JSON desde Drive: {downloadUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(downloadUrl))
        {
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[ButtonSpawner] Fallo la descarga ({request.error}). Usando datos locales...");
                LeerLocalYSpawnear();
                yield break;
            }

            string json = request.downloadHandler.text;

            // Actualizar la copia local como respaldo para la proxima vez
            try { File.WriteAllText(DriveDataLoader.LocalFilePath, json); }
            catch (Exception e) { Debug.LogWarning($"[ButtonSpawner] No se pudo actualizar local: {e.Message}"); }

            SpawnBotones(json);
        }
    }

    private void LeerLocalYSpawnear()
    {
        string json = DriveDataLoader.ReadLocalJson();
        if (json == null)
        {
            Debug.LogError("[ButtonSpawner] No hay datos disponibles ni en Drive ni localmente.");
            return;
        }
        SpawnBotones(json);
    }

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
            Debug.Log($"[ButtonSpawner] Creando boton {i} | Texto: {datos.texto} | Ponderacion: {datos.ponderacion}");

            GameObject instancia = Instantiate(buttonPrefab, panel);
            ConfigurarBoton(instancia, datos, i);
        }

        Debug.Log($"[ButtonSpawner] {lista.botones.Count} botones instanciados.");
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

    void ClickBoton(int i, float ponderacion)
    {
        Debug.Log($"[ButtonSpawner] Click en boton {i} | Ponderacion: {ponderacion}");
        // Aqui va la logica del reto cuando se amplie la mecanica
    }

    private string ConvertToDirectDownloadUrl(string url)
    {
        if (url.Contains("/file/d/"))
        {
            int start = url.IndexOf("/file/d/") + 8;
            int end   = url.IndexOf("/", start);
            if (end == -1) end = url.Length;
            string fileId = url.Substring(start, end - start);
            return $"https://drive.google.com/uc?export=download&id={fileId}";
        }
        if (url.Contains("id="))
        {
            int start     = url.IndexOf("id=") + 3;
            string fileId = url.Substring(start);
            return $"https://drive.google.com/uc?export=download&id={fileId}";
        }
        return url;
    }
}

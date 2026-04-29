using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;

[System.Serializable]
public class BotonData
{
    public string texto;
    public float ponderacion;
}

[System.Serializable]
public class BotonDataList
{
    public List<BotonData> botones;
}

public class ButtonSpawner : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform panel;

    private const string JSON_FILE_NAME = "variables_abstraccion.json";

    void Start()
    {
#if UNITY_EDITOR
        // En el Editor siempre lee desde StreamingAssets
        string ruta = Path.Combine(Application.streamingAssetsPath, JSON_FILE_NAME);
        List<BotonData> botones = LeerJSON(ruta);
        InstanciarBotones(botones);

#elif UNITY_ANDROID
        // En APK (Quest 3 / Android) lee desde persistentDataPath
        StartCoroutine(CargarEnAndroid());
#endif
    }

    // ---------------------------------------------------------------
    // ANDROID / QUEST 3
    // Lee desde persistentDataPath (archivo puesto via ADB)
    // Si no existe aun, copia el JSON del APK como fallback inicial
    // ---------------------------------------------------------------
    IEnumerator CargarEnAndroid()
    {
        string destino = Path.Combine(Application.persistentDataPath, JSON_FILE_NAME);

        if (!File.Exists(destino))
        {
            Debug.LogWarning("[ButtonSpawner] JSON no encontrado en persistentDataPath. Copiando desde StreamingAssets como fallback...");

            string origen = Path.Combine(Application.streamingAssetsPath, JSON_FILE_NAME);

            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(origen))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[ButtonSpawner] No se pudo copiar el JSON desde StreamingAssets: " + www.error);
                    yield break;
                }

                File.WriteAllText(destino, www.downloadHandler.text);
                Debug.Log("[ButtonSpawner] JSON copiado a: " + destino);
            }
        }
        else
        {
            Debug.Log("[ButtonSpawner] JSON encontrado en: " + destino);
        }

        List<BotonData> botones = LeerJSON(destino);
        InstanciarBotones(botones);
    }

    // ---------------------------------------------------------------
    // Lee y parsea el JSON desde una ruta absoluta
    // ---------------------------------------------------------------
    List<BotonData> LeerJSON(string ruta)
    {
        if (!File.Exists(ruta))
        {
            Debug.LogError("[ButtonSpawner] Archivo JSON no encontrado en: " + ruta);
            return null;
        }

        Debug.Log("[ButtonSpawner] Leyendo JSON desde: " + ruta);
        string contenido = File.ReadAllText(ruta);
        BotonDataList lista = JsonUtility.FromJson<BotonDataList>(contenido);

        if (lista == null || lista.botones == null)
        {
            Debug.LogError("[ButtonSpawner] Formato incorrecto. El JSON debe tener la estructura: { \"botones\": [...] }");
            return null;
        }

        return lista.botones;
    }

    // ---------------------------------------------------------------
    // Instancia los botones en el panel
    // ---------------------------------------------------------------
    void InstanciarBotones(List<BotonData> botones)
    {
        if (botones == null || botones.Count == 0)
        {
            Debug.LogWarning("[ButtonSpawner] No hay botones para instanciar.");
            return;
        }

        for (int i = 0; i < botones.Count; i++)
        {
            Debug.Log("Creando botón " + i + " | Texto: " + botones[i].texto + " | Ponderación: " + botones[i].ponderacion);
            GameObject btn = Instantiate(buttonPrefab, panel);

            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = botones[i].texto;
            }

            int index = i;
            float ponderacion = botones[i].ponderacion;

            btn.GetComponent<Button>().onClick.AddListener(() => ClickBoton(index, ponderacion));
        }
    }

    void ClickBoton(int i, float ponderacion)
    {
        Debug.Log("Click en botón " + i + " | Ponderación: " + ponderacion);
    }
}

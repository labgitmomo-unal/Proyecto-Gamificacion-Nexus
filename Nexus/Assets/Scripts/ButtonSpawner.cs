using UnityEngine;
using UnityEngine.UI;
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

    // Nombre del archivo JSON (debe estar en la misma carpeta que el APK o en persistentDataPath)
    private const string JSON_FILE_NAME = "niveles_abstraccion.json";

    void Start()
    {
        List<BotonData> botones = CargarBotonesDesdeJSON();

        if (botones == null || botones.Count == 0)
        {
            Debug.LogWarning("[ButtonSpawner] No se encontraron botones en el JSON o el archivo no existe.");
            return;
        }

        for (int i = 0; i < botones.Count; i++)
        {
            Debug.Log("Creando botón " + i + " | Texto: " + botones[i].texto + " | Ponderación: " + botones[i].ponderacion);
            GameObject btn = Instantiate(buttonPrefab, panel);

            // Asignar texto
            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = botones[i].texto;
            }

            // Capturar datos del botón
            int index = i;
            float ponderacion = botones[i].ponderacion;

            btn.GetComponent<Button>().onClick.AddListener(() => ClickBoton(index, ponderacion));
        }
    }

    List<BotonData> CargarBotonesDesdeJSON()
    {
        string jsonPath = ObtenerRutaJSON();

        if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
        {
            Debug.LogError("[ButtonSpawner] Archivo JSON no encontrado en: " + jsonPath);
            return null;
        }

        Debug.Log("[ButtonSpawner] Leyendo JSON desde: " + jsonPath);

        string jsonContent = File.ReadAllText(jsonPath);
        BotonDataList lista = JsonUtility.FromJson<BotonDataList>(jsonContent);

        if (lista == null || lista.botones == null)
        {
            Debug.LogError("[ButtonSpawner] El JSON no tiene el formato correcto. Se esperaba { \"botones\": [...] }");
            return null;
        }

        return lista.botones;
    }

    string ObtenerRutaJSON()
    {
        // 1. Busca junto al APK (en la carpeta persistente del dispositivo)
        string persistentPath = Path.Combine(Application.persistentDataPath, JSON_FILE_NAME);
        if (File.Exists(persistentPath))
        {
            return persistentPath;
        }

        // 2. Fallback: busca en StreamingAssets (incluido dentro del APK, solo lectura)
        string streamingPath = Path.Combine(Application.streamingAssetsPath, JSON_FILE_NAME);

#if UNITY_ANDROID && !UNITY_EDITOR
        // En Android, StreamingAssets está comprimido en el APK y no se puede leer con File.Exists
        // Se usa el persistentDataPath como ruta principal en Android
        Debug.LogWarning("[ButtonSpawner] En Android usa persistentDataPath. Ruta: " + persistentPath);
        return persistentPath;
#else
        if (File.Exists(streamingPath))
        {
            return streamingPath;
        }
#endif

        // 3. Fallback editor: busca en la raíz del proyecto
        string projectRootPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, JSON_FILE_NAME);
        if (File.Exists(projectRootPath))
        {
            return projectRootPath;
        }

        return persistentPath; // Retorna la ruta esperada aunque no exista, para el log de error
    }

    void ClickBoton(int i, float ponderacion)
    {
        Debug.Log("Click en botón " + i + " | Ponderación: " + ponderacion);
    }
}

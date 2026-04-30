using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Colocar en la escena del MENU.
/// Descarga el JSON desde Google Drive y lo guarda en persistentDataPath
/// para que ButtonSpawner lo consuma en la escena del reto.
///
/// Estructura del JSON:
/// {
///   "botones": [
///     { "texto": "Texto del boton", "ponderacion": 3.0 },
///     { "texto": "Otro texto",      "ponderacion": 1.0 }
///   ]
/// }
///
/// Conexion con la UI (cuando este lista):
///   - SetDriveUrl(string url)  -> llamar cuando el encargado pega la URL en el InputField
///   - RefreshData()            -> llamar desde el boton "Actualizar"
///   - OnDataLoaded             -> evento para habilitar el boton de continuar / ocultar loading
///   - OnDataLoadFailed         -> evento para mostrar mensaje de error en pantalla
/// </summary>
public class DriveDataLoader : MonoBehaviour
{
    [Header("Google Drive")]
    [Tooltip("Pega aqui el link publico del archivo JSON en Google Drive")]
    public string drivePublicUrl = "";

    [Tooltip("Descargar automaticamente al iniciar la escena del menu")]
    public bool autoLoadOnStart = true;

    // Ruta local compartida con ButtonSpawner
    public static string LocalFilePath =>
        Path.Combine(Application.persistentDataPath, "variables_abstraccion.json");

    // Eventos para la UI
    public static event Action OnDataLoaded;
    public static event Action<string> OnDataLoadFailed;

    public bool IsLoading { get; private set; }
    public bool DataReady { get; private set; }

    void Start()
    {
        if (autoLoadOnStart)
            LoadFromDrive();
    }

    // Llama esto para iniciar la descarga con la URL actual
    public void LoadFromDrive()
    {
        if (string.IsNullOrEmpty(drivePublicUrl))
        {
            Debug.LogWarning("[DriveDataLoader] La URL de Drive esta vacia.");
            return;
        }
        if (IsLoading) return;
        StartCoroutine(DownloadAndSave(drivePublicUrl));
    }

    // Fuerza re-descarga — conectar al boton "Actualizar" del encargado
    public void RefreshData()
    {
        DataReady = false;
        LoadFromDrive();
    }

    // La UI llama esto cuando el encargado escribe/pega la URL en el InputField
    public void SetDriveUrl(string url)
    {
        drivePublicUrl = url;
    }

    private IEnumerator DownloadAndSave(string rawUrl)
    {
        IsLoading = true;
        DataReady = false;

        string downloadUrl = ConvertToDirectDownloadUrl(rawUrl);
        Debug.Log($"[DriveDataLoader] Descargando desde: {downloadUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(downloadUrl))
        {
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                IsLoading = false;
                string error = $"Error de red: {request.error}";
                Debug.LogError($"[DriveDataLoader] {error}");
                OnDataLoadFailed?.Invoke(error);
                yield break;
            }

            string json = request.downloadHandler.text;

            if (!IsValidJson(json))
            {
                IsLoading = false;
                string error = "El archivo descargado no es un JSON valido.";
                Debug.LogError($"[DriveDataLoader] {error}");
                OnDataLoadFailed?.Invoke(error);
                yield break;
            }

            try
            {
                File.WriteAllText(LocalFilePath, json);
                Debug.Log($"[DriveDataLoader] JSON guardado en: {LocalFilePath}");
                DataReady = true;
                IsLoading = false;
                OnDataLoaded?.Invoke();
            }
            catch (Exception e)
            {
                IsLoading = false;
                string error = $"Error al guardar localmente: {e.Message}";
                Debug.LogError($"[DriveDataLoader] {error}");
                OnDataLoadFailed?.Invoke(error);
            }
        }
    }

    // Convierte cualquier formato de link publico de Drive a descarga directa
    private string ConvertToDirectDownloadUrl(string url)
    {
        // Formato: https://drive.google.com/file/d/FILE_ID/view?usp=sharing
        if (url.Contains("/file/d/"))
        {
            int start = url.IndexOf("/file/d/") + 8;
            int end   = url.IndexOf("/", start);
            if (end == -1) end = url.Length;
            string fileId = url.Substring(start, end - start);
            return $"https://drive.google.com/uc?export=download&id={fileId}";
        }
        // Formato: https://drive.google.com/open?id=FILE_ID
        if (url.Contains("id="))
        {
            int start     = url.IndexOf("id=") + 3;
            string fileId = url.Substring(start);
            return $"https://drive.google.com/uc?export=download&id={fileId}";
        }
        return url;
    }

    private bool IsValidJson(string json)
    {
        json = json.Trim();
        return (json.StartsWith("{") && json.EndsWith("}")) ||
               (json.StartsWith("[") && json.EndsWith("]"));
    }

    // Utilidades estaticas para que ButtonSpawner acceda al archivo local
    public static bool HasLocalData() => File.Exists(LocalFilePath);

    public static string ReadLocalJson()
    {
        if (!HasLocalData())
        {
            Debug.LogWarning("[DriveDataLoader] No hay datos locales guardados.");
            return null;
        }
        return File.ReadAllText(LocalFilePath);
    }
}

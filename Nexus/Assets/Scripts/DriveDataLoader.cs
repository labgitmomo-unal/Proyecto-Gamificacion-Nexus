using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// FIX DEFINITIVO:
///   - DataReady es ESTATICO: persiste entre escenas. ButtonSpawner lo consulta
///     en OnEnable para saber si la descarga ya termino sin necesitar el evento.
///   - NO se borra el archivo local antes de descargar: ButtonSpawner siempre
///     tiene algo que mostrar mientras llega el nuevo JSON.
///   - Start() resetea DataReady=false para forzar re-descarga en cada Play.
/// </summary>
public class DriveDataLoader : MonoBehaviour
{
    [Header("Google Drive")]
    [Tooltip("Pega aqui el link publico del archivo JSON en Google Drive")]
    public string drivePublicUrl = "";

    [Tooltip("Descargar automaticamente al iniciar la escena del menu")]
    public bool autoLoadOnStart = true;

    public static string LocalFilePath =>
        Path.Combine(Application.persistentDataPath, "variables_abstraccion.json");

    public static event Action OnDataLoaded;
    public static event Action<string> OnDataLoadFailed;

    public bool IsLoading { get; private set; }

    // ESTATICO: ButtonSpawner lo lee desde cualquier escena
    public static bool DataReady { get; private set; }

    void Start()
    {
        DataReady = false; // Resetear cada sesion para forzar descarga fresca
        if (autoLoadOnStart)
            LoadFromDrive();
    }

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

    public void RefreshData()
    {
        DataReady = false;
        LoadFromDrive();
    }

    public void SetDriveUrl(string url) => drivePublicUrl = url;

    private IEnumerator DownloadAndSave(string rawUrl)
    {
        IsLoading = true;
        DataReady = false;

        // NO borramos el archivo local: ButtonSpawner puede mostrar datos del run anterior
        // mientras la descarga nueva llega. Cuando termina, HandleDataLoaded lo regenera.

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
                OnDataLoaded?.Invoke(); // ButtonSpawner regenera si esta en escena
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

    private bool IsValidJson(string json)
    {
        json = json.Trim();
        return (json.StartsWith("{") && json.EndsWith("}")) ||
               (json.StartsWith("[") && json.EndsWith("]"));
    }

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
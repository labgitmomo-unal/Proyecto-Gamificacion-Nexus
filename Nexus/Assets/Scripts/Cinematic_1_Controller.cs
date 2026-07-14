using UnityEngine;
using System.Collections;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Cinematic_1_Controller : MonoBehaviour
{
    public static bool SuppressStart = false;

    public PlayableDirector director;
    public GameObject VirtualCamera;
    public GameObject XrigCamera;
    public Camera vistaPilotoCamera;
    
    public TrafficManager trafficManager;
    public AudioSource Challenge_Indicator_1;

    private LODGroup[] _cachedLODGroups;
    private Camera _cinematicCamera;
    private UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset _urpAsset;

    void Start()
    {
        if (SuppressStart)
        {
            SuppressStart = false;
            return;
        }

        _cachedLODGroups = FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
        forcaLODsBaixos(true);
        OcultarNiebla(false);

        SuspenderAdaptiveQuality(true);
        SuspenderTrafficCleanup(true);

        if (VirtualCamera != null) _cinematicCamera = VirtualCamera.GetComponent<Camera>();
        _urpAsset = GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        AjustarLimitesTrafico(true);
        AjustarFarClip(true);

        XrigCamera.SetActive(false);
        VirtualCamera.SetActive(true);
        director.stopped += OnCinematicEnd;
        MostrarCongestion();

        StartCoroutine(ForzarGC(43f));
        StartCoroutine(VentanaFarClip(38f, 43f, 53f, 58f));
        StartCoroutine(VentanaFarClip(66f, 71f, 82f, 87f));
        StartCoroutine(VentanaFarClip(95f, 99f, 103f, 108f));
    }

    void OnCinematicEnd(PlayableDirector d)
    {
        forcaLODsBaixos(false);
        OcultarNiebla(false);
        XrigCamera.SetActive(true);
        VirtualCamera.SetActive(false);
        AjustarFarClip(false);
        if (vistaPilotoCamera != null) vistaPilotoCamera.enabled = false;
        AjustarLimitesTrafico(false);
        SuspenderAdaptiveQuality(false);
        SuspenderTrafficCleanup(false);
        StartCoroutine(Wait());
        Challenge_Indicator_1.Play();
        
    }

    private void forcaLODsBaixos(bool ativo)
    {
        for (int i = 0; i < _cachedLODGroups.Length; i++)
        {
            var lg = _cachedLODGroups[i];
            if (lg.name.Contains("StreetBuilding") || lg.name.Contains("Building") || lg.name.Contains("Rounded"))
            {
                lg.ForceLOD(ativo ? 2 : -1);
            }
        }
    }

    private void OcultarNiebla(bool ocultar)
    {
        var envFxs = GameObject.Find("EnvironFxs");
        if (envFxs == null) return;
        for (int i = 0; i < envFxs.transform.childCount; i++)
        {
            var child = envFxs.transform.GetChild(i);
            if (child.name.StartsWith("FogFake"))
                child.gameObject.SetActive(!ocultar);
        }
    }

private void AjustarFarClip(bool reducir)
    {
        if (_cinematicCamera != null)
            _cinematicCamera.farClipPlane = 150f;
        QualitySettings.lodBias = reducir ? 0.4f : 0.3f;
        if (_urpAsset != null)
            _urpAsset.renderScale = reducir ? 0.8f : 1.0f;
    }

    private void AjustarLimitesTrafico(bool cinematicActiva)
    {
        var cleanup = FindFirstObjectByType<TrafficCleanup>();
        if (cleanup != null)
        {
            if (cinematicActiva)
            {
                cleanup.maxTrafficCars = 35;
                cleanup.maxDistanceFromCamera = 150f;
            }
            else
            {
                cleanup.maxTrafficCars = 80;
                cleanup.maxDistanceFromCamera = 300f;
            }
        }
    }

private void SuspenderAdaptiveQuality(bool suspender)
    {
        // CORRECCION: ya no se desactiva AdaptiveQuality durante la cinematica.
        // Antes se apagaba justo en la parte mas pesada (el puente), dejando
        // sin proteccion contra caidas de FPS, lo que disparaba el ASW del
        // compositor de Quest y causaba la distorsion visual reportada.
        // Ahora se mantiene activo y se pone en modo "cinematica" (mas agresivo).
        var aq = FindFirstObjectByType<AdaptiveQuality>();
        if (aq != null) aq.SetCinematicMode(suspender);
    }

    private void SuspenderTrafficCleanup(bool suspender)
    {
        var tc = FindFirstObjectByType<TrafficCleanup>();
        if (tc != null)
        {
            if (suspender)
                tc.CancelInvoke("Cleanup");
            else
                tc.InvokeRepeating("Cleanup", 2f, 0.5f);
        }
    }

    private IEnumerator ForzarGC(float delay)
    {
        yield return new WaitForSeconds(delay);
        System.GC.Collect();
    }

    private IEnumerator VentanaFarClip(float inicio, float llegada, float fin, float restaurado)
    {
        yield return new WaitForSeconds(inicio);

        float farTarget = 50f;
        float duracion = llegada - inicio;
        float t = 0f;
        if (duracion > 0.01f)
        {
            float farStart = _cinematicCamera != null ? _cinematicCamera.farClipPlane : 150f;
            while (t < duracion)
            {
                t += Time.deltaTime;
                if (_cinematicCamera != null)
                    _cinematicCamera.farClipPlane = Mathf.Lerp(farStart, farTarget, t / duracion);
                yield return null;
            }
            if (_cinematicCamera != null) _cinematicCamera.farClipPlane = farTarget;
        }

        if (_urpAsset != null) _urpAsset.renderScale = 0.65f;
        QualitySettings.lodBias = 0.1f;
        for (int i = 0; i < _cachedLODGroups.Length; i++)
            _cachedLODGroups[i].ForceLOD(_cachedLODGroups[i].lodCount - 1);

        yield return new WaitForSeconds(fin - llegada);

        duracion = restaurado - fin;
        t = 0f;
        if (duracion > 0.01f)
        {
            float farStart = farTarget;
            farTarget = 150f;
            while (t < duracion)
            {
                t += Time.deltaTime;
                if (_cinematicCamera != null)
                    _cinematicCamera.farClipPlane = Mathf.Lerp(farStart, farTarget, t / duracion);
                yield return null;
            }
            if (_cinematicCamera != null) _cinematicCamera.farClipPlane = farTarget;
        }

        if (_urpAsset != null) _urpAsset.renderScale = 0.8f;
        QualitySettings.lodBias = 0.4f;
        for (int i = 0; i < _cachedLODGroups.Length; i++)
            _cachedLODGroups[i].ForceLOD(-1);
        forcaLODsBaixos(true);
    }

    public void MostrarCongestion()
    {
        TrafficManager.Instance.SetVelocidad(0.4f);

        foreach (var spawner in FindObjectsByType<RandomObjectSpawner>(FindObjectsSortMode.None))
        {
            spawner.minSpawnInterval = 0.3f;
            spawner.maxSpawnInterval = 1f;
        }
    }

    public void RestaurarTrafico()
    {
        TrafficManager.Instance.RestaurarVelocidad();

        foreach (var spawner in FindObjectsByType<RandomObjectSpawner>(FindObjectsSortMode.None))
        {
            spawner.minSpawnInterval = 0.5f;
            spawner.maxSpawnInterval = 4f;
        }
    }

    IEnumerator RestoreTrafficAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RestaurarTrafico();
    }

    private IEnumerator Wait()
    {

        yield return new WaitForSeconds(45f);
 
    }
}

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
    public GameObject nexusLogo;
    private GameObject _xrCamera;
    public TrafficManager trafficManager;
    public BridgeControlManager bridgeControl;
    public AudioSource Challenge_Indicator_1;
    private LODGroup[] _cachedLODGroups;
    private Camera _cinematicCamera;
    private UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset _urpAsset;
    private float _timeOffset;

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
        if (VirtualCamera != null)
            _cinematicCamera = VirtualCamera.GetComponent<Camera>();
        _urpAsset = GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        AjustarLimitesTrafico(true);
        AjustarFarClip(true);
        FindAndDisableXRCamera();
        VirtualCamera.SetActive(true);
        director.stopped += OnCinematicEnd;
        MostrarCongestion();
        StartCoroutine(ForzarGC(65f));
        StartCoroutine(IniciarVentanas());
    }

    private void FindAndDisableXRCamera()
    {
        if (XrigCamera != null)
        {
            _xrCamera = XrigCamera;
            _xrCamera.SetActive(false);
            return;
        }
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            _xrCamera = mainCam.gameObject;
            _xrCamera.SetActive(false);
            return;
        }
        var virtualCam = VirtualCamera != null ? VirtualCamera.GetComponent<Camera>() : null;
        var allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            if (cam != virtualCam && cam != vistaPilotoCamera)
            {
                var go = cam.gameObject;
                if (go.transform.parent != null && go.transform.parent.name.Contains("XR"))
                {
                    _xrCamera = go;
                    _xrCamera.SetActive(false);
                    return;
                }
            }
        }
    }

    private IEnumerator IniciarVentanas()
    {
        while (director == null) yield return null;
        while (double.IsNaN(director.time) || director.time < 0.0) yield return null;
        double lastTime = director.time;
        while (director.time <= lastTime)
        {
            lastTime = director.time;
            yield return null;
        }
        _timeOffset = Time.time - (float)director.time;
        Debug.Log("[Cinematic] Director started. timeOffset=" + _timeOffset.ToString("F3") + "s");
        StartCoroutine(VentanaFarClip(38f, 43f, 53f, 58f));
        StartCoroutine(VentanaFarClip(55f, 65f, 82f, 87f));
        StartCoroutine(VentanaFarClip(85f, 90f, 103f, 108f));
    }

    void OnCinematicEnd(PlayableDirector d)
    {
        forcaLODsBaixos(false);
        OcultarNiebla(false);
        VirtualCamera.SetActive(false);
        AjustarFarClip(false);
        if (vistaPilotoCamera != null) vistaPilotoCamera.enabled = false;
        AjustarLimitesTrafico(false);
        SuspenderAdaptiveQuality(false);
        SuspenderTrafficCleanup(false);
        RestaurarTrafico();
        if (bridgeControl != null) bridgeControl.FreezeBridge();
        StartCoroutine(ShowLogoThenEnableXR());
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
        if (_cinematicCamera != null) _cinematicCamera.farClipPlane = 150f;
        QualitySettings.lodBias = reducir ? 0.4f : 0.3f;
        if (_urpAsset != null) _urpAsset.renderScale = reducir ? 0.8f : 1.0f;
    }

    private void AjustarLimitesTrafico(bool cinematicActiva)
    {
        var cleanup = FindFirstObjectByType<TrafficCleanup>();
        if (cleanup != null)
        {
            if (cinematicActiva)
            {
                cleanup.maxTrafficCars = 80;
                cleanup.maxDistanceFromCamera = 300f;
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
        float realTarget = inicio + _timeOffset;
        if (realTarget > Time.time)
            yield return new WaitForSeconds(realTarget - Time.time);
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
        // Congestión desde el inicio: tráfico lento y apretado, no a velocidad completa.
        if (bridgeControl != null)
            bridgeControl.AplicarCongestionInicial();
        else
            TrafficManager.Instance.SetVelocidad(1f);

        foreach (var spawner in FindObjectsByType<RandomObjectSpawner>(FindObjectsSortMode.None))
        {
            spawner.minSpawnInterval = 0.3f;
            spawner.maxSpawnInterval = 2f;
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

    private IEnumerator ShowLogoThenEnableXR()
    {
        if (_xrCamera != null)
            _xrCamera.SetActive(true);
        yield break;
    }
}

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

    void Start()
    {
        if (SuppressStart)
        {
            SuppressStart = false;
            return;
        }

        _cachedLODGroups = FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
        forcaLODsBaixos(true);
        OcultarNiebla(true);

        SuspenderAdaptiveQuality(true);
        SuspenderTrafficCleanup(true);

        AjustarLimitesTrafico(true);
        AjustarFarClip(true);

        XrigCamera.SetActive(false);
        VirtualCamera.SetActive(true);
        director.stopped += OnCinematicEnd;
        MostrarCongestion();

        StartCoroutine(ForzarGC(65f));
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
        if (VirtualCamera != null)
        {
            var vcam = VirtualCamera.GetComponent<Cinemachine.CinemachineVirtualCamera>();
            if (vcam != null)
            {
                vcam.m_Lens.FarClipPlane = reducir ? 45f : 150f;
            }
        }
        QualitySettings.lodBias = reducir ? 0.1f : 0.3f;
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urp.renderScale = reducir ? 0.8f : 1.0f;
        }
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
        var aq = FindFirstObjectByType<AdaptiveQuality>();
        if (aq != null) aq.enabled = !suspender;
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

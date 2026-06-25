using UnityEngine;
using System.Collections;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem.XR;
using Unity.XR.CoreUtils;

public class Cinematic_1_Controller : MonoBehaviour
{
    public static bool SuppressStart = false;

    public PlayableDirector director;
    public GameObject VirtualCamera;   // La cámara Vista_Piloto del barco (GO)
    public GameObject XrigCamera;      // Main Camera del XR Rig
    public Camera vistaPilotoCamera;   // Camera component del barco

    public TrafficManager trafficManager;
    public AudioSource Challenge_Indicator_1;

    private LODGroup[] _cachedLODGroups;

    // VR: referencias para mover el XR Rig siguiendo la cámara del barco
    private XROrigin _xrOrigin;
    private TrackedPoseDriver _trackedPoseDriver;
    private bool _cinematicActiva = false;

    void Start()
    {
        if (SuppressStart)
        {
            SuppressStart = false;
            return;
        }

        // Obtener referencias XR
        _xrOrigin = FindFirstObjectByType<XROrigin>();
        if (XrigCamera != null)
            _trackedPoseDriver = XrigCamera.GetComponent<TrackedPoseDriver>();

        _cachedLODGroups = FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
        forcaLODsBaixos(true);
        OcultarNiebla(true);

        SuspenderAdaptiveQuality(true);
        SuspenderTrafficCleanup(true);

        AjustarLimitesTrafico(true);
        AjustarFarClip(true);

        // CORRECCIÓN VR (Quest 3):
        // El error original desactivaba la Main Camera XR y activaba la Vista_Piloto Camera.
        // Esto provocaba que Quest 3 renderizase en modo mono sin datos de head-tracking,
        // y el ATW (Asynchronous TimeWarp) generaba distorsión visual al pasar por el puente.
        //
        // Solución: mantener la Main Camera XR siempre activa.
        // Desactivamos solo el TrackedPoseDriver para que no pelee con el
        // posicionamiento manual. En LateUpdate movemos el XR Origin para
        // que siga exactamente la posición y rotación de la cámara del barco.
        // La Vista_Piloto Camera se deja DESACTIVADA para no renderizar dos veces.

        if (_trackedPoseDriver != null)
            _trackedPoseDriver.enabled = false;

        // Asegurar que la Main Camera XR está activa
        if (XrigCamera != null)
            XrigCamera.SetActive(true);

        // Desactivar la cámara del barco (el XR Rig la reemplaza visualmente)
        if (vistaPilotoCamera != null)
            vistaPilotoCamera.enabled = false;

        _cinematicActiva = true;

        director.stopped += OnCinematicEnd;
        MostrarCongestion();

        StartCoroutine(ForzarGC(65f));
    }

    void LateUpdate()
    {
        // Durante la cinemática, mover el XR Origin para que la Main Camera XR
        // siga exactamente la posición y rotación de la cámara del barco.
        // Esto garantiza renderizado estéreo correcto en Quest 3 durante todo el vuelo.
        if (!_cinematicActiva || vistaPilotoCamera == null || _xrOrigin == null) return;

        _xrOrigin.transform.SetPositionAndRotation(
            vistaPilotoCamera.transform.position,
            vistaPilotoCamera.transform.rotation
        );
    }

    void OnCinematicEnd(PlayableDirector d)
    {
        _cinematicActiva = false;

        forcaLODsBaixos(false);
        OcultarNiebla(false);

        // Restaurar TrackedPoseDriver para que el jugador controle la cámara
        if (_trackedPoseDriver != null)
            _trackedPoseDriver.enabled = true;

        // La Vista_Piloto Camera ya estaba desactivada, dejarla así
        if (vistaPilotoCamera != null)
            vistaPilotoCamera.enabled = false;

        AjustarFarClip(false);
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
        // lodBias 0.1f era demasiado agresivo: causaba un pop masivo de los 258 LODGroups
        // del puente HighPlatform_LargeBridge01 (2) al entrar en esa zona, disparando
        // la carga de CPU/GPU y agravando la distorsión del ATW en Quest 3.
        // Subido a 0.5f para equilibrio rendimiento/calidad durante la cinemática.
        QualitySettings.lodBias = reducir ? 0.5f : 0.3f;

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

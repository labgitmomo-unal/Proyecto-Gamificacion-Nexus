using UnityEngine;
using System.Collections;
using UnityEngine.Playables;

public class Cinematic_1_Controller : MonoBehaviour
{
    public static bool SuppressStart = false;
    public PlayableDirector director;
    public GameObject VirtualCamera;
    public GameObject XrigCamera;
    public Camera vistaPilotoCamera;
    public GameObject nexusLogo;
    private GameObject _xrCamera;
    private Camera _cinematicCamera;
    private float _savedFarClip;
    public TrafficManager trafficManager;
    public BridgeControlManager bridgeControl;
    public AudioSource Challenge_Indicator_1;

    private void Start()
    {
        QuestOptimizer.ApplyQuestSettings();

        if (SuppressStart)
        {
            SuppressStart = false;
            return;
        }
        OcultarNiebla(false);
        if (VirtualCamera != null && VirtualCamera.activeSelf)
            VirtualCamera.SetActive(false);
        if (director != null)
            director.stopped += OnCinematicEnd;
        MostrarCongestion();
        StartCoroutine(WaitForDirector());
    }

    private IEnumerator WaitForDirector()
    {
        while (director == null) yield return null;
        while (double.IsNaN(director.time) || director.time < 0.0) yield return null;
        yield return null;
        if (VirtualCamera != null)
        {
            _cinematicCamera = VirtualCamera.GetComponent<Camera>();
            if (_cinematicCamera != null)
            {
                _savedFarClip = _cinematicCamera.farClipPlane;
                _cinematicCamera.farClipPlane = 150f;
            }
            VirtualCamera.SetActive(true);
        }
        FindAndDisableXRCamera();
    }

    private void OnCinematicEnd(PlayableDirector d)
    {
        StopAllCoroutines();
        if (_cinematicCamera != null)
        {
            _cinematicCamera.farClipPlane = _savedFarClip;
            _cinematicCamera = null;
        }
        if (VirtualCamera != null)
            VirtualCamera.SetActive(false);
        if (vistaPilotoCamera != null)
            vistaPilotoCamera.enabled = false;
        StartCoroutine(ShowLogoThenEnableXR());
        if (Challenge_Indicator_1 != null)
            Challenge_Indicator_1.Play();
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
        var allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var virtualCam = VirtualCamera != null ? VirtualCamera.GetComponent<Camera>() : null;
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

    public void MostrarCongestion()
    {
        if (trafficManager != null)
            trafficManager.SetVelocidad(0.5f);
        else
            TrafficManager.Instance?.SetVelocidad(0.5f);
    }

    public void RestaurarTrafico()
    {
        if (trafficManager != null)
            trafficManager.SetVelocidad(2f);
        else
            TrafficManager.Instance?.SetVelocidad(2f);
    }

    private IEnumerator ShowLogoThenEnableXR()
    {
        if (_xrCamera != null)
            _xrCamera.SetActive(true);
        yield break;
    }
}

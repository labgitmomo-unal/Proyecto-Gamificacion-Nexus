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
    
    public TrafficManager trafficManager;
    public AudioSource Challenge_Indicator_1;

    void Start()
    {
        if (SuppressStart)
        {
            SuppressStart = false;
            return;
        }

        AjustarLimitesTrafico(true);

        XrigCamera.SetActive(false);
        VirtualCamera.SetActive(true);
        director.stopped += OnCinematicEnd;
        MostrarCongestion();
        // StartCoroutine(RestoreTrafficAfterDelay(60f)); // Restaurar tráfico después de 10 segundos
    }

    void OnCinematicEnd(PlayableDirector d)
    {
        XrigCamera.SetActive(true);
        VirtualCamera.SetActive(false);
        if (vistaPilotoCamera != null) vistaPilotoCamera.enabled = false;
        AjustarLimitesTrafico(false);
        StartCoroutine(Wait());
        Challenge_Indicator_1.Play();
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
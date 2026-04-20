using UnityEngine;
using System.Collections;
using UnityEngine.Playables;

public class Cinematic_1_Controller : MonoBehaviour
{
    public PlayableDirector director;
    public GameObject VirtualCamera;
    public GameObject XrigCamera;
    
    public TrafficManager trafficManager;

    void Start()
    {
        XrigCamera.SetActive(false);
        VirtualCamera.SetActive(true);
        director.stopped += OnCinematicEnd;
        MostrarCongestion();
        StartCoroutine(RestoreTrafficAfterDelay(60f)); // Restaurar tráfico después de 10 segundos
        
    }

    void OnCinematicEnd(PlayableDirector d)
    {
        XrigCamera.SetActive(true);
        VirtualCamera.SetActive(false);

        
    }

    public void MostrarCongestion()
    {
        // Ralentizar tráfico
        TrafficManager.Instance.SetVelocidad(0.4f);

        // Aumentar frecuencia de spawn
        foreach (var spawner in FindObjectsByType<RandomObjectSpawner>(FindObjectsSortMode.None))
        {
            spawner.minSpawnInterval = 0.1f;
            spawner.maxSpawnInterval = 0.5f;
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
}
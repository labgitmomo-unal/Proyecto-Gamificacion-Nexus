using UnityEngine;
using UnityEngine.Playables;

public class Cinematic_1_Controller : MonoBehaviour
{
    public PlayableDirector director;
    public GameObject VirtualCamera;
    public GameObject XrigCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        XrigCamera.SetActive(false);
        VirtualCamera.SetActive(true);
        director.stopped +=OnCinematicEnd;
    }

    // Update is called once per frame
    void OnCinematicEnd(PlayableDirector d)
    {
        XrigCamera.SetActive(true);
        VirtualCamera.SetActive(false);
    }
}

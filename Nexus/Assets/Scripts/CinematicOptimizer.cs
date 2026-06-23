using UnityEngine;
using System.Collections;

public class CinematicOptimizer : MonoBehaviour
{
    public Cinematic_1_Controller cinematic;
    public AudioSource prewarmAudio;

    private void Awake()
    {
        Time.maximumDeltaTime = 0.1f;
        Application.backgroundLoadingPriority = ThreadPriority.Low;
    }

    private IEnumerator Start()
    {
        PrewarmTrafficPools();
        PrewarmAudio();
        yield return null;
        System.GC.Collect();
    }

    private void PrewarmAudio()
    {
        if (prewarmAudio != null && prewarmAudio.clip != null)
        {
            prewarmAudio.Play();
            prewarmAudio.Stop();
        }
        if (cinematic != null && cinematic.Challenge_Indicator_1 != null)
        {
            var a = cinematic.Challenge_Indicator_1;
            if (a.clip != null)
            {
                a.Play();
                a.Stop();
            }
        }
    }

    private void PrewarmTrafficPools()
    {
        var spawners = FindObjectsByType<RandomObjectSpawner>(FindObjectsSortMode.None);
        foreach (var s in spawners)
        {
            if (s.objectList == null || s.objectList.Count == 0) continue;

            for (int i = 0; i < s.objectList.Count; i++)
            {
                var prefab = s.objectList[i];
                if (prefab == null) continue;

                for (int j = 0; j < 3; j++)
                {
                    var obj = Instantiate(prefab, s.transform.position, Quaternion.identity);
                    obj.SetActive(false);
                    s.ReturnToPool(obj);
                }
            }
        }
    }
}

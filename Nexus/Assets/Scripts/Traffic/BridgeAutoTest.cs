using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>TEST ONLY - simulates the bridge button presses in play mode.</summary>
[DefaultExecutionOrder(-1000)]
public class BridgeAutoTest : MonoBehaviour
{
    private BridgeControlManager bc;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(1.5f);
        bc = FindAnyObjectByType<BridgeControlManager>(FindObjectsInactive.Include);
        if (bc == null) { Debug.Log("[AutoTest] no BridgeControlManager found"); yield break; }

        var sb = new StringBuilder();
        sb.AppendLine($"[AutoTest] START | template.vel={bc.spawnerTemplate?.initialVelocity} basePlantilla={GetBase()}");
        Debug.Log(sb.ToString());

        for (int i = 0; i < 4; i++)
        {
            if (!bc.IsActive) bc.FreezeBridge();
            bc.ReleaseStep();
            yield return new WaitForSeconds(1.0f);

            float avg = PromedioVelocidadSinTemplate();
            bool complete = bc.IsComplete;
            Debug.Log($"[AutoTest] press#{i + 1} ReleaseCount={bc.ReleaseCount} IsComplete={complete} avgCarVel={avg:F1} template.vel={bc.spawnerTemplate?.initialVelocity}");
        }

        Debug.Log("[AutoTest] DONE - stopping play mode");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

    private Vector3 GetBase()
    {
        if (TrafficManager.Instance == null) return Vector3.zero;
        return TrafficManager.Instance.GetBaseVelocityForPlantilla(bc.spawnerTemplate);
    }

    private float PromedioVelocidadSinTemplate()
    {
        var all = FindObjectsByType<MovementController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        float sum = 0f; int n = 0;
        foreach (var c in all)
        {
            if (c == null || c == bc.spawnerTemplate) continue;
            if (!c.gameObject.activeInHierarchy) continue;
            sum += c.initialVelocity.magnitude;
            n++;
        }
        return n > 0 ? sum / n : 0f;
    }
}

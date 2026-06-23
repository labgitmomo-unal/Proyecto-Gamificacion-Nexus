using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class QuestOptimizer : MonoBehaviour
{
    private void Awake()
    {
        Application.targetFrameRate = 72;
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadowDistance = 8f;
        QualitySettings.shadowCascades = 0;
        QualitySettings.softParticles = false;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.maximumLODLevel = 0;

        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            urpAsset.renderScale = 0.85f;
            urpAsset.shadowDistance = 8f;
        }
    }
}

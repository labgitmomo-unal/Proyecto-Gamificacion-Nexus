using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class QuestOptimizer : MonoBehaviour
{
    private const int TargetFrameRate = 72;
    private const float MaximumQuestRenderScale = 0.8f;
    private const float QuestShadowDistance = 8f;

    private void Awake()
    {
        Application.targetFrameRate = TargetFrameRate;
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadowDistance = QuestShadowDistance;
        QualitySettings.shadowCascades = 0;
        QualitySettings.softParticles = false;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.maximumLODLevel = 0;
        QualitySettings.lodBias = 0.3f;

        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            urpAsset.renderScale = Mathf.Min(urpAsset.renderScale, MaximumQuestRenderScale);
            urpAsset.shadowDistance = QuestShadowDistance;
        }
    }
}

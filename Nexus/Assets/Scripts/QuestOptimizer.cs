using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class QuestOptimizer : MonoBehaviour
{
    private const int TargetFrameRate = 72;
    private const float MaximumQuestRenderScale = 0.8f;
    private const int MaximumQuestLODLevel = 0;
    private const float QuestShadowDistance = 8f;

    private void Awake()
    {
        ApplyQuestSettings();
    }

    public static void ApplyQuestSettings()
    {
        if (Application.platform != RuntimePlatform.Android)
            return;

        Application.targetFrameRate = TargetFrameRate;
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadowDistance = QuestShadowDistance;
        QualitySettings.shadowCascades = 0;
        QualitySettings.softParticles = false;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.maximumLODLevel = MaximumQuestLODLevel;
        QualitySettings.lodBias = 0.3f;

        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            urpAsset.renderScale = Mathf.Min(urpAsset.renderScale, MaximumQuestRenderScale);
            urpAsset.shadowDistance = QuestShadowDistance;
        }

        ConfigureCamerasForQuest();
    }

    private static void ConfigureCamerasForQuest()
    {
        var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var camera in cameras)
        {
            if (camera == null)
                continue;

            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = true;
            camera.nearClipPlane = Mathf.Max(camera.nearClipPlane, 0.05f);

            Vector3 parentScale = camera.transform.parent != null
                ? camera.transform.parent.lossyScale
                : Vector3.one;
            Vector3 localScale = camera.transform.localScale;
            bool hasNonUniformScale = !Mathf.Approximately(localScale.x, localScale.y)
                || !Mathf.Approximately(localScale.y, localScale.z);
            if (hasNonUniformScale && parentScale.x > 0.001f && parentScale.y > 0.001f && parentScale.z > 0.001f)
            {
                camera.transform.localScale = new Vector3(
                    1f / parentScale.x,
                    1f / parentScale.y,
                    1f / parentScale.z);
            }
        }
    }
}

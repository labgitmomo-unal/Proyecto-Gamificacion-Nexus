using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AdaptiveQuality : MonoBehaviour
{
    public float checkInterval = 1f;
    public int targetFPS = 72;
    public int lowFPSTreshold = 50;

    private const int FrameSampleCount = 60;
    private const float MinimumNormalRenderScale = 0.5f;
    private const float MinimumCinematicRenderScale = 0.35f;
    private const float MaximumNormalRenderScale = 0.8f;
    private const float MaximumCinematicRenderScale = 0.75f;
    private const float NormalScaleStep = 0.08f;
    private const float CinematicScaleStep = 0.15f;

    private float[] frameTimes;
    private int index;
    private float renderScale;
    private float shadowDist = 8f;
    private bool cinematicMode;

    private void Awake()
    {
        frameTimes = new float[FrameSampleCount];
        float initialFrameTime = 1f / Mathf.Max(targetFPS, 1);
        for (int i = 0; i < frameTimes.Length; i++)
            frameTimes[i] = initialFrameTime;

        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        renderScale = urp != null ? urp.renderScale : MaximumNormalRenderScale;
        renderScale = Mathf.Clamp(renderScale, MinimumNormalRenderScale, MaximumNormalRenderScale);
        SetRenderScale(renderScale);
        Application.targetFrameRate = targetFPS;
    }

    private void Start()
    {
        InvokeRepeating(nameof(CheckPerformance), checkInterval, checkInterval);
    }

    public void SetCinematicMode(bool enabled)
    {
        cinematicMode = enabled;
    }

    private void CheckPerformance()
    {
        float averageFrameTime = 0f;
        for (int i = 0; i < frameTimes.Length; i++)
            averageFrameTime += frameTimes[i];

        averageFrameTime /= frameTimes.Length;
        float fps = averageFrameTime > 0f ? 1f / averageFrameTime : targetFPS;
        int threshold = cinematicMode ? Mathf.Max(lowFPSTreshold, 50) : lowFPSTreshold;
        float minimumScale = cinematicMode ? MinimumCinematicRenderScale : MinimumNormalRenderScale;
        float maximumScale = cinematicMode ? MaximumCinematicRenderScale : MaximumNormalRenderScale;
        float scaleStep = cinematicMode ? CinematicScaleStep : NormalScaleStep;

        if (fps < threshold && renderScale > minimumScale)
        {
            renderScale = Mathf.Max(renderScale - scaleStep, minimumScale);
            SetRenderScale(renderScale);
            shadowDist = Mathf.Max(shadowDist - 1f, 1f);
            QualitySettings.shadowDistance = shadowDist;
        }
        else if (fps > targetFPS + 10 && renderScale < maximumScale)
        {
            renderScale = Mathf.Min(renderScale + 0.05f, maximumScale);
            SetRenderScale(renderScale);
        }
    }

    private void SetRenderScale(float scale)
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
            urp.renderScale = scale;
    }

    private void Update()
    {
        frameTimes[index] = Time.unscaledDeltaTime;
        index = (index + 1) % frameTimes.Length;
    }
}

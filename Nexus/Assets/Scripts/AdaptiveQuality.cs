using UnityEngine;
using System.Collections.Generic;

public class AdaptiveQuality : MonoBehaviour
{
    public float checkInterval = 1f;
    public int targetFPS = 60;
    public int lowFPSTreshold = 30;

    private float[] frameTimes;
    private int index;
    private float renderScale = 1f;
    private float shadowDist = 8f;

    private void Awake()
    {
        frameTimes = new float[60];
        Application.targetFrameRate = targetFPS;
    }

    private void Start()
    {
        InvokeRepeating(nameof(CheckPerformance), checkInterval, checkInterval);
    }

    private void CheckPerformance()
    {
        float avg = 0f;
        for (int i = 0; i < frameTimes.Length; i++)
            avg += frameTimes[i];
        avg /= frameTimes.Length;
        float fps = avg > 0 ? 1f / avg : 999f;

        if (fps < lowFPSTreshold && renderScale > 0.55f)
        {
            renderScale -= 0.08f;
            renderScale = Mathf.Max(renderScale, 0.5f);
            SetRenderScale(renderScale);
            shadowDist -= 1f;
            QualitySettings.shadowDistance = Mathf.Max(shadowDist, 1f);
        }
        else if (fps > targetFPS + 10 && renderScale < 0.85f)
        {
            renderScale += 0.05f;
            renderScale = Mathf.Min(renderScale, 0.85f);
            SetRenderScale(renderScale);
        }
    }

    private void SetRenderScale(float scale)
    {
        var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline
            as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        if (urp != null)
            urp.renderScale = scale;
    }

    private void Update()
    {
        frameTimes[index] = Time.unscaledDeltaTime;
        index = (index + 1) % frameTimes.Length;
    }
}

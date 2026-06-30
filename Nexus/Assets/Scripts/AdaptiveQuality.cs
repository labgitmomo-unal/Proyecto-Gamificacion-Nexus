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

    // Modo "cinemática": umbrales más bajos y reacción más rápida/agresiva
    // para evitar que el FPS caiga lo suficiente como para disparar el
    // ASW (Adaptive SpaceWarp) del compositor de Quest, que es lo que
    // generaba la distorsión visual al pasar por el puente.
    private bool _cinematicMode = false;
    private float _minRenderScaleNormal = 0.5f;
    private float _minRenderScaleCinematic = 0.35f;
    private float _stepNormal = 0.08f;
    private float _stepCinematic = 0.15f;

    private void Awake()
    {
        frameTimes = new float[60];
        Application.targetFrameRate = targetFPS;
    }

    private void Start()
    {
        InvokeRepeating(nameof(CheckPerformance), checkInterval, checkInterval);
    }

    // Llamado por Cinematic_1_Controller en vez de desactivar este componente.
    // Mantiene el sistema de protección de FPS activo durante la parte más
    // pesada (el puente), bajando el umbral y reaccionando más rápido.
    public void SetCinematicMode(bool activo)
    {
        _cinematicMode = activo;
        if (!activo)
        {
            // Al salir de la cinemática, permitir que vuelva a subir gradualmente
            // hacia la calidad normal.
        }
    }

    private void CheckPerformance()
    {
        float avg = 0f;
        for (int i = 0; i < frameTimes.Length; i++)
            avg += frameTimes[i];
        avg /= frameTimes.Length;
        float fps = avg > 0 ? 1f / avg : 999f;

        int threshold = _cinematicMode ? Mathf.Max(lowFPSTreshold, 50) : lowFPSTreshold;
        float minScale = _cinematicMode ? _minRenderScaleCinematic : _minRenderScaleNormal;
        float step = _cinematicMode ? _stepCinematic : _stepNormal;
        float maxScale = _cinematicMode ? 0.75f : 0.85f;

        if (fps < threshold && renderScale > minScale)
        {
            renderScale -= step;
            renderScale = Mathf.Max(renderScale, minScale);
            SetRenderScale(renderScale);
            shadowDist -= 1f;
            QualitySettings.shadowDistance = Mathf.Max(shadowDist, 1f);
        }
        else if (fps > targetFPS + 10 && renderScale < maxScale)
        {
            renderScale += 0.05f;
            renderScale = Mathf.Min(renderScale, maxScale);
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

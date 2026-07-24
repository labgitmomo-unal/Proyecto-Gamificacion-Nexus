using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public sealed class MapViewController : MonoBehaviour
{
    [SerializeField] private Camera mapCamera;
    [SerializeField] private RawImage displayImage;
    [SerializeField] private GameObject presentationObject;
    [SerializeField] private int renderTextureSize = 2048;
    [SerializeField] private int antiAliasing = 4;
    [SerializeField] private float mapYawOffsetDegrees = 180f;

    private RenderTexture _renderTexture;
    private bool _fogWasEnabled;
    private bool _fogStateCaptured;

    private void Awake()
    {
        if (mapCamera == null)
            mapCamera = GetComponent<Camera>();
        if (mapCamera == null || displayImage == null || presentationObject == null)
        {
            Debug.LogWarning($"[{nameof(MapViewController)}] {name}: asigna mapCamera, displayImage y presentationObject.", this);
            enabled = false;
            return;
        }
        if (renderTextureSize <= 0 || antiAliasing is not (1 or 2 or 4 or 8))
        {
            Debug.LogWarning($"[{nameof(MapViewController)}] {name}: renderTextureSize debe ser mayor que cero y antiAliasing debe ser 1, 2, 4 u 8.", this);
            enabled = false;
            return;
        }
        if (!Mathf.Approximately(mapYawOffsetDegrees, 0f))
            transform.Rotate(0f, mapYawOffsetDegrees, 0f, Space.World);
        mapCamera.cullingMask &= ~(1 << presentationObject.layer);
        SetupRenderTexture();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        RestoreFog();
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != mapCamera || mapCamera == null || !mapCamera.isActiveAndEnabled)
            return;
        _fogWasEnabled = RenderSettings.fog;
        _fogStateCaptured = true;
        RenderSettings.fog = false;
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == mapCamera)
            RestoreFog();
    }

    private void RestoreFog()
    {
        if (!_fogStateCaptured)
            return;
        RenderSettings.fog = _fogWasEnabled;
        _fogStateCaptured = false;
    }

    private void SetupRenderTexture()
    {
        _renderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 32, RenderTextureFormat.Default)
        {
            antiAliasing = antiAliasing,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp,
            depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat_S8_UInt
        };
        _renderTexture.Create();
        mapCamera.targetTexture = _renderTexture;
        displayImage.texture = _renderTexture;
    }

    private void OnDestroy()
    {
        RestoreFog();
        if (mapCamera != null && mapCamera.targetTexture == _renderTexture)
            mapCamera.targetTexture = null;
        if (displayImage != null && displayImage.texture == _renderTexture)
            displayImage.texture = null;
        if (_renderTexture == null)
            return;
        _renderTexture.Release();
        Destroy(_renderTexture);
        _renderTexture = null;
    }
}

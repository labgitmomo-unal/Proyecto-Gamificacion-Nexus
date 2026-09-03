using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public sealed class MapViewController : MonoBehaviour
{
    private const int AndroidRenderTextureSize = 256;
    private const int AndroidFrameSkipInterval = 3;

    [Header("References")]
    [SerializeField] private Camera mapCamera;
    [SerializeField] private RawImage displayImage;
    [SerializeField] private GameObject presentationObject;

    [Header("Render Texture")]
    [SerializeField] private int renderTextureSize = 512;
    [SerializeField] private int antiAliasing = 1;

    [Header("Camera")]
    [SerializeField] private float mapYawOffsetDegrees = 180f;

    private RenderTexture _renderTexture;
    private RenderTexture _lastValidTexture;
    private bool _fogWasEnabled;
    private bool _fogStateCaptured;
    private bool _initialized;
    private int _frameCounter;

    public RenderTexture CurrentRenderTexture => _renderTexture;
    public bool HasValidTexture => _renderTexture != null && _renderTexture.IsCreated();

    private void Awake()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            renderTextureSize = Mathf.Min(renderTextureSize, AndroidRenderTextureSize);
            antiAliasing = 1;
        }

        if (mapCamera == null)
            mapCamera = GetComponent<Camera>();

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        ConfigureCamera();
        SetupRenderTexture();
        _initialized = true;
    }

    private void Start()
    {
        RenderOnce();
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

    private void LateUpdate()
    {
        if (!_initialized || mapCamera == null)
            return;
        _frameCounter++;
        if (_frameCounter % AndroidFrameSkipInterval == 0)
            RenderOnce();
    }

    public void RequestRefresh()
    {
        if (!_initialized)
        {
            Debug.LogWarning("[MapViewController] RequestRefresh llamado antes de inicializar.", this);
            return;
        }
        if (_renderTexture == null || !_renderTexture.IsCreated())
        {
            Debug.LogWarning("[MapViewController] RenderTexture inválida. No se puede actualizar.", this);
            return;
        }
        RenderOnce();
    }

    public void RenderOnce()
    {
        if (mapCamera == null || !mapCamera.isActiveAndEnabled)
            return;
        if (_renderTexture == null || !_renderTexture.IsCreated())
            return;
        mapCamera.Render();
    }

    public void InvalidateTexture()
    {
        if (_renderTexture != null && _renderTexture.IsCreated())
        {
            if (_lastValidTexture != null)
                _lastValidTexture.Release();
            _lastValidTexture = _renderTexture;
        }
        _renderTexture = null;
        _initialized = false;
    }

    private bool ValidateReferences()
    {
        if (mapCamera == null)
        {
            Debug.LogError("[MapViewController] mapCamera no asignado.", this);
            return false;
        }
        if (displayImage == null)
        {
            Debug.LogError("[MapViewController] displayImage no asignado.", this);
            return false;
        }
        if (presentationObject == null)
        {
            Debug.LogError("[MapViewController] presentationObject no asignado.", this);
            return false;
        }
        if (renderTextureSize <= 0)
        {
            Debug.LogError("[MapViewController] renderTextureSize debe ser mayor que cero.", this);
            return false;
        }
        if (antiAliasing is not (1 or 2 or 4 or 8))
        {
            Debug.LogError("[MapViewController] antiAliasing debe ser 1, 2, 4 u 8.", this);
            return false;
        }
        return true;
    }

    private void ConfigureCamera()
    {
        mapCamera.cullingMask = -1;

        if (!Mathf.Approximately(mapYawOffsetDegrees, 0f))
            mapCamera.transform.Rotate(0f, mapYawOffsetDegrees, 0f, Space.World);

        mapCamera.allowHDR = false;
        mapCamera.allowMSAA = false;
        mapCamera.useOcclusionCulling = true;

        if (Application.platform == RuntimePlatform.Android)
        {
            mapCamera.stereoTargetEye = StereoTargetEyeMask.None;
            var cameraData = mapCamera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData != null)
            {
                cameraData.allowXRRendering = false;
                cameraData.renderShadows = false;
                cameraData.renderPostProcessing = false;
                cameraData.requiresDepthOption = CameraOverrideOption.Off;
                cameraData.requiresColorOption = CameraOverrideOption.Off;
                cameraData.antialiasing = AntialiasingMode.None;
            }
        }
    }

    private void SetupRenderTexture()
    {
        if (_renderTexture != null)
        {
            if (mapCamera.targetTexture == _renderTexture)
                mapCamera.targetTexture = null;
            _renderTexture.Release();
            Destroy(_renderTexture);
        }

        _renderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 32, RenderTextureFormat.Default)
        {
            antiAliasing = antiAliasing,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };

        if (!_renderTexture.Create())
        {
            Debug.LogError("[MapViewController] No se pudo crear la RenderTexture.", this);
            InvalidateTexture();
            return;
        }

        mapCamera.targetTexture = _renderTexture;
        if (displayImage != null)
            displayImage.texture = _renderTexture;
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

    private void OnDestroy()
    {
        RestoreFog();
        if (mapCamera != null && mapCamera.targetTexture == _renderTexture)
            mapCamera.targetTexture = null;
        if (displayImage != null && displayImage.texture == _renderTexture)
            displayImage.texture = null;
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }
        if (_lastValidTexture != null)
        {
            _lastValidTexture.Release();
            Destroy(_lastValidTexture);
            _lastValidTexture = null;
        }
    }
}

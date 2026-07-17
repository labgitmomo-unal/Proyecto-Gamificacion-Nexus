using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

/// <summary>
/// Creates a runtime RenderTexture, assigns it to this Camera, and pipes it
/// to the RawImage found by name in the scene (expected to be on a World Space Canvas).
/// Disables built-in fog for this camera only using render pipeline callbacks.
/// Attach this component to the aerial map camera GameObject.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MapViewController : MonoBehaviour
{
    [SerializeField] private string displayObjectName = "MapViewDisplay";
    [SerializeField] private int renderTextureSize = 2048;

    private RenderTexture _renderTexture;
    private Camera _mapCamera;
    private bool _fogWasEnabled;

    private void Awake()
    {
        _mapCamera = GetComponent<Camera>();
        ExcludeDisplayLayerFromCamera();
        SetupRenderTexture();
        ConfigureDisplayObject();
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
    }

    /// <summary>
    /// Called before each camera renders. Disables built-in fog only for the map camera.
    /// </summary>
    private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam != _mapCamera) return;
        _fogWasEnabled = RenderSettings.fog;
        RenderSettings.fog = false;
    }

    /// <summary>
    /// Called after each camera renders. Restores the fog state for all other cameras.
    /// </summary>
    private void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam != _mapCamera) return;
        RenderSettings.fog = _fogWasEnabled;
    }

    /// <summary>
    /// Excludes the layer of the display object from the camera's culling mask at runtime,
    /// preventing a visual feedback loop where the canvas renders into its own texture.
    /// </summary>
    private void ExcludeDisplayLayerFromCamera()
    {
        var displayObj = GameObject.Find(displayObjectName);
        if (displayObj == null) return;

        _mapCamera.cullingMask &= ~(1 << displayObj.layer);
    }

    /// <summary>Creates the RenderTexture and assigns it to the aerial camera output.</summary>
    private void SetupRenderTexture()
    {
        // Use Depth format compatible with URP Render Graph (requires depth buffer)
        _renderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 32, RenderTextureFormat.Default)
        {
            antiAliasing = 1,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat_S8_UInt
        };
        _renderTexture.Create();
        _mapCamera.targetTexture = _renderTexture;
    }

    /// <summary>Finds the RawImage display by name and assigns the RenderTexture to it.</summary>
    private void ConfigureDisplayObject()
    {
        var displayObj = GameObject.Find(displayObjectName);
        if (displayObj == null)
        {
            Debug.LogWarning($"[MapViewController] Display object '{displayObjectName}' not found in scene.");
            return;
        }

        var rawImage = displayObj.GetComponent<RawImage>();
        if (rawImage == null)
        {
            Debug.LogWarning($"[MapViewController] Object '{displayObjectName}' has no RawImage component.");
            return;
        }

        rawImage.texture = _renderTexture;
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }
}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Bola agarrable que representa un nodo lógico del grafo holográfico.
/// Se combina con XRGrabInteractable para permitir drag-and-drop en VR.
/// Cuando es soltada en un NodeSlot, el GridGraphManager evalúa qué aristas activar.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class GraphNodeBall : MonoBehaviour
{
    // ─── Constantes ───────────────────────────────────────────────────────────
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Identificación del Nodo")]
    [Tooltip("Nodo lógico que representa esta bola en el grafo de movilidad.")]
    public NodeID nodeID = NodeID.None;

    [Header("Visual")]
    [SerializeField]
    [Tooltip("Renderer de la esfera. Se auto-detecta si está en el mismo GO.")]
    private Renderer _renderer;

    [SerializeField]
    [Tooltip("Color de emisión en estado inactivo (sin hover).")]
    private Color _colorBase = new Color(0f, 0.8f, 1f);

    [SerializeField]
    [Tooltip("Color de emisión durante el hover.")]
    private Color _colorHover = Color.white;

    [SerializeField] [Range(0f, 6f)]
    [Tooltip("Intensidad de brillo HDR en estado base.")]
    private float _intensidadBase = 2f;

    [SerializeField] [Range(0f, 10f)]
    [Tooltip("Intensidad de brillo HDR durante el hover.")]
    private float _intensidadHover = 5f;

    // ─── Estado ───────────────────────────────────────────────────────────────
    private XRGrabInteractable _grab;
    private MaterialPropertyBlock _mpb;

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _mpb  = new MaterialPropertyBlock();

        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        AplicarEmision(_colorBase, _intensidadBase);
    }

    private void OnEnable()
    {
        _grab.hoverEntered.AddListener(OnHoverEnter);
        _grab.hoverExited.AddListener(OnHoverExit);
    }

    private void OnDisable()
    {
        _grab.hoverEntered.RemoveListener(OnHoverEnter);
        _grab.hoverExited.RemoveListener(OnHoverExit);
    }

    // ─── Callbacks XRI ────────────────────────────────────────────────────────
    private void OnHoverEnter(HoverEnterEventArgs _) => AplicarEmision(_colorHover, _intensidadHover);
    private void OnHoverExit(HoverExitEventArgs _)   => AplicarEmision(_colorBase,  _intensidadBase);

    // ─── Visual ───────────────────────────────────────────────────────────────
    /// <summary>Aplica emisión HDR al renderer sin instanciar material (usa MaterialPropertyBlock).</summary>
    private void AplicarEmision(Color color, float intensidad)
    {
        if (_renderer == null) return;
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(EmissionColorID, color * Mathf.Pow(2f, intensidad));
        _renderer.SetPropertyBlock(_mpb);
    }
}

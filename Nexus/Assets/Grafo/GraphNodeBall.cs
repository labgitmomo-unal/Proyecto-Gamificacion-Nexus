using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Bola agarrable en VR que representa un nodo del grafo holográfico.
///
/// Estados visuales (evaluados por polling cada frame, no por eventos):
///   - Base     : azul muy oscuro (idle o en mano del jugador).
///   - Hover    : blanco brillante (controlador encima, bola libre).
///   - En Base  : azul brillante (seleccionada por un XRSocketInteractor).
///
/// Usa renderer.material (instancia por nodo) para garantizar que la
/// emisión no afecte al material compartido y siempre sea visible.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class GraphNodeBall : MonoBehaviour
{
    // ─── Constantes ───────────────────────────────────────────────────────────

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private const string EmissionKeyword = "_EMISSION";

    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Visual")]
    [SerializeField]
    [Tooltip("Renderer de la esfera. Se auto-detecta si está en el mismo GO.")]
    private Renderer _renderer;

    [SerializeField]
    [Tooltip("Color base (idle). Azul oscuro por defecto.")]
    private Color _colorBase = new Color(0f, 0.08f, 0.35f);

    [SerializeField]
    [Tooltip("Color cuando el controlador está encima (bola libre).")]
    private Color _colorHover = new Color(0.7f, 0.85f, 1f);

    [SerializeField]
    [Tooltip("Color cuando el nodo está anclado en una Base.")]
    private Color _colorEnBase = new Color(0f, 0.45f, 1f);

    [SerializeField] [Range(-4f, 4f)]
    [Tooltip("Intensidad HDR (2^x) en estado base. Negativo = muy oscuro.")]
    private float _intensidadBase = -1f;

    [SerializeField] [Range(0f, 8f)]
    [Tooltip("Intensidad HDR durante hover.")]
    private float _intensidadHover = 4f;

    [SerializeField] [Range(0f, 6f)]
    [Tooltip("Intensidad HDR cuando está sobre una Base.")]
    private float _intensidadEnBase = 2f;

    // ─── Estado ───────────────────────────────────────────────────────────────

    private enum EstadoVisual { Base, Hover, EnBase }

    private XRGrabInteractable _grab;
    private Material           _material;
    private EstadoVisual       _estadoActual = EstadoVisual.Base;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();

        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        // Crear instancia de material exclusiva para este nodo.
        // Esto garantiza que setear _EmissionColor aquí no cambie otros objetos.
        _material = _renderer.material;
        _material.EnableKeyword(EmissionKeyword);

        AplicarEmision(_colorBase, _intensidadBase);
    }

    private void Update()
    {
        EstadoVisual nuevo = CalcularEstado();
        if (nuevo == _estadoActual) return;

        _estadoActual = nuevo;
        switch (_estadoActual)
        {
            case EstadoVisual.EnBase:
                AplicarEmision(_colorEnBase, _intensidadEnBase);
                break;
            case EstadoVisual.Hover:
                AplicarEmision(_colorHover, _intensidadHover);
                break;
            default:
                AplicarEmision(_colorBase, _intensidadBase);
                break;
        }
    }

    // ─── Estado ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Consulta el estado XRI en tiempo real para determinar el estado visual.
    /// Se usa polling en lugar de eventos para evitar race conditions con el socket.
    /// </summary>
    private EstadoVisual CalcularEstado()
    {
        foreach (var interactor in _grab.interactorsSelecting)
        {
            if (interactor is XRSocketInteractor)
                return EstadoVisual.EnBase;
        }

        if (_grab.isHovered)
            return EstadoVisual.Hover;

        return EstadoVisual.Base;
    }

    // ─── Visual ───────────────────────────────────────────────────────────────

    /// <summary>Aplica emisión HDR a la instancia de material de este nodo.</summary>
    private void AplicarEmision(Color color, float intensidad)
    {
        if (_material == null) return;
        _material.SetColor(EmissionColorID, color * Mathf.Pow(2f, intensidad));
    }
}

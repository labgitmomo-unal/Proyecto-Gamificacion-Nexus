using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Bola agarrable en VR que representa un nodo del grafo holográfico.
///
/// Estados visuales simples por contacto:
///   - Base     : azul muy oscuro.
///   - En Base  : azul brillante (tocando una base con tag).
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
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int TintColorID = Shader.PropertyToID("_TintColor");

    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Visual")]
    [SerializeField]
    [Tooltip("Renderer de la esfera. Se auto-detecta si está en el mismo GO.")]
    private Renderer _renderer;

    [SerializeField]
    [Tooltip("Tag que identifica las bases del grafo.")]
    private string _tagBase = "Base";

    [SerializeField]
    [Tooltip("Color base (idle). Azul oscuro por defecto.")]
    private Color _colorBase = new Color(0f, 0.08f, 0.35f);

    [SerializeField]
    [Tooltip("Color cuando el nodo está anclado en una Base.")]
    private Color _colorEnBase = new Color(0f, 0.45f, 1f);

    [SerializeField] [Range(-4f, 4f)]
    [Tooltip("Intensidad HDR (2^x) en estado base. Negativo = muy oscuro.")]
    private float _intensidadBase = -1f;

    [SerializeField] [Range(0f, 6f)]
    [Tooltip("Intensidad HDR cuando está sobre una Base.")]
    private float _intensidadEnBase = 2f;

    // ─── Estado ───────────────────────────────────────────────────────────────

    private enum EstadoVisual { Base, EnBase }

    private Material           _material;
    private EstadoVisual       _estadoActual = EstadoVisual.Base;
    private int                _contactosBase;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        // Crear instancia de material exclusiva para este nodo.
        // Esto garantiza que setear _EmissionColor aquí no cambie otros objetos.
        _material = _renderer.material;
        _material.EnableKeyword(EmissionKeyword);

        AplicarMaterial(_colorBase, _intensidadBase);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || !collision.collider.CompareTag(_tagBase)) return;

        _contactosBase++;
        if (_estadoActual == EstadoVisual.EnBase) return;

        _estadoActual = EstadoVisual.EnBase;
        AplicarMaterial(_colorEnBase, _intensidadEnBase);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision == null || !collision.collider.CompareTag(_tagBase)) return;

        _contactosBase = Mathf.Max(0, _contactosBase - 1);
        if (_contactosBase > 0) return;

        _estadoActual = EstadoVisual.Base;
        AplicarMaterial(_colorBase, _intensidadBase);
    }

    // ─── Visual ───────────────────────────────────────────────────────────────

    /// <summary>Aplica emisión HDR a la instancia de material de este nodo.</summary>
    private void AplicarMaterial(Color color, float intensidad)
    {
        if (_material == null) return;

        if (_material.HasProperty(BaseColorID))
            _material.SetColor(BaseColorID, color);

        if (_material.HasProperty(ColorID))
            _material.SetColor(ColorID, color);

        if (_material.HasProperty(TintColorID))
            _material.SetColor(TintColorID, color);

        _material.SetColor(EmissionColorID, color * Mathf.Pow(2f, intensidad));
    }
}

using UnityEngine;

/// <summary>
/// Configuración visual mínima de una línea de conexión.
/// Se edita al seleccionar el GameObject de la línea en Unity.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class BaseConnectionLine : MonoBehaviour
{
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    [Tooltip("Indica si la línea empieza o queda activada.")]
    [SerializeField] private bool _activa;

    [Tooltip("Color de la línea.")]
    [SerializeField] private Color _color = new Color(0f, 0.4f, 1f, 0.8f);

    [Tooltip("Tamaño/grosor de la línea.")]
    [SerializeField, Min(0.0001f)] private float _tamanio = 0.01f;

    public bool Activa
    {
        get => _activa;
        set => _activa = value;
    }

    public Color ColorLinea
    {
        get => _color;
        set => _color = value;
    }

    public float Tamanio
    {
        get => _tamanio;
        set => _tamanio = Mathf.Max(0.0001f, value);
    }

    private LineRenderer _lineRenderer;
    private Material _materialInstancia;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        Aplicar();
    }

    private void OnEnable()
    {
        if (_lineRenderer == null)
            _lineRenderer = GetComponent<LineRenderer>();

        Aplicar();
    }

    private void OnValidate()
    {
        _tamanio = Mathf.Max(0.0001f, _tamanio);

        if (_lineRenderer == null)
            _lineRenderer = GetComponent<LineRenderer>();

        Aplicar();
    }

    public void Aplicar()
    {
        if (_lineRenderer == null)
            _lineRenderer = GetComponent<LineRenderer>();

        if (_lineRenderer == null) return;

        _lineRenderer.useWorldSpace = true;

        if (_materialInstancia == null)
        {
            Material materialBase = _lineRenderer.sharedMaterial;
            if (materialBase != null)
            {
                _materialInstancia = new Material(materialBase);
                _lineRenderer.material = _materialInstancia;
            }
            else
            {
                // Si no hay material asignado en el LineRenderer, crear uno seguro en tiempo de ejecución.
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                                ?? Shader.Find("Universal Render Pipeline/Lit")
                                ?? Shader.Find("Standard");

                if (shader != null)
                {
                    _materialInstancia = new Material(shader);
                    _lineRenderer.material = _materialInstancia;
                }
            }
        }

        _lineRenderer.enabled = _activa;
        _lineRenderer.startWidth = _activa ? _tamanio : 0f;
        _lineRenderer.endWidth = _activa ? _tamanio : 0f;
        _lineRenderer.startColor = _color;
        _lineRenderer.endColor = _color;

        if (_materialInstancia != null)
        {
            AplicarColorMaterial(_materialInstancia, _color);
            if (_materialInstancia.HasProperty(EmissionColorID))
                _materialInstancia.SetColor(EmissionColorID, _color);
        }
    }

    private static void AplicarColorMaterial(Material material, Color color)
    {
        if (material == null) return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_TintColor"))
            material.SetColor("_TintColor", color);
    }
}
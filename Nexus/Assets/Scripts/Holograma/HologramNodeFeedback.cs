using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona el feedback visual de un nodo del grafo holográfico.
/// Expone factorPrioridad para que GraphManager amplifique el impacto en nodos clave
/// (ej. Nodo Centro = 1.4 para reflejar el 70 % de viajes de la Línea 3).
/// </summary>
public class HologramNodeFeedback : MonoBehaviour
{
    // ─── Neón base ────────────────────────────────────────────────────────────
    [Header("Configuración de Neón")]
    public MeshRenderer nodeRenderer;
    public Color hoverColor = Color.cyan;

    [Header("Intensidad ALOM")]
    public float normalIntensity = 1.0f;
    public float hoverIntensity  = 4.0f;

    // ─── Prioridad de nodo ────────────────────────────────────────────────────
    [Header("Prioridad de Nodo")]
    [Tooltip("Multiplicador de impacto al aplicar mejoras. " +
             "Centro = 1.4 (70 % de viajes). Resto = 1.0.")]
    [Range(1f, 3f)]
    public float factorPrioridad = 1f;

    // ─── Pulso de confirmación ────────────────────────────────────────────────
    [Header("Pulso de Confirmación (URP)")]
    [Tooltip("Color HDR del flash al validar un snap. Cian por defecto.")]
    public Color colorPulsoConfirmacion = new Color(0f, 1f, 1f, 1f);

    [Tooltip("Segundos que tarda el pulso en desvanecerse hasta el color base.")]
    [Range(0.1f, 1.5f)]
    public float duracionPulso = 0.4f;

    // ─── Estado interno ───────────────────────────────────────────────────────
    private Color   _colorOriginal;
    private Material _nodeMaterial;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Start()
    {
        // Instanciar material para no afectar al resto de nodos
        _nodeMaterial = nodeRenderer.material;
        _colorOriginal = _nodeMaterial.GetColor("_EmissionColor");
    }

    // ─── Hover ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Llamado por XRSimpleInteractable (evento hoverEntered).
    /// Resalta el nodo y notifica al GraphManager para resaltar las aristas conectadas.
    /// </summary>
    public void OnHoverEnter()
    {
        _nodeMaterial.SetColor("_EmissionColor", hoverColor * hoverIntensity);
        GraphManager.Instance?.OnNodoHoverEnter(this);
    }

    /// <summary>
    /// Llamado por XRSimpleInteractable (evento lastHoverExited).
    /// Restaura el estado base del nodo y sus aristas.
    /// </summary>
    public void OnHoverExit()
    {
        _nodeMaterial.SetColor("_EmissionColor", _colorOriginal * normalIntensity);
        GraphManager.Instance?.OnNodoHoverExit(this);
    }

    // ─── Pulso de confirmación ────────────────────────────────────────────────

    /// <summary>
    /// Dispara un pulso de emisión en el material del nodo para confirmar
    /// que un pivote fue anclado correctamente. Llamado por NodoSnapZone.
    /// </summary>
    public void DispararPulsoConfirmacion()
    {
        StopCoroutine(nameof(CorrutinaPulso));
        StartCoroutine(nameof(CorrutinaPulso));
    }

    private IEnumerator CorrutinaPulso()
    {
        const float intensidadFlash = 8f;

        // Flash inmediato al pico
        _nodeMaterial.SetColor("_EmissionColor", colorPulsoConfirmacion * intensidadFlash);

        float elapsed = 0f;
        Color colorBase = _colorOriginal * normalIntensity;

        while (elapsed < duracionPulso)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duracionPulso;
            Color actual = Color.Lerp(colorPulsoConfirmacion * intensidadFlash, colorBase, t);
            _nodeMaterial.SetColor("_EmissionColor", actual);
            yield return null;
        }

        _nodeMaterial.SetColor("_EmissionColor", colorBase);
    }
}

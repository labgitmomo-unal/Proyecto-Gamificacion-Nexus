using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Identifica el tipo de solución urbana que representa un pivote
/// y permite que sea manipulado en VR mediante XRGrabInteractable.
/// La aplicación y reversión del impacto es gestionada externamente
/// por NodoSnapZone, que llama a GraphManager directamente.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class PivotMejora : MonoBehaviour
{
    // ─── Configuración ────────────────────────────────────────────────────────
    [Header("Tipo de Solución")]
    [Tooltip("Tipo de medida de movilidad que implementa este pivote.")]
    public TipoPivote tipoPivote = TipoPivote.SemaforoInteligente;

    [Header("Factores de Reducción")]
    [Tooltip("Fracción del volumen de pasajeros que se reduce al colocar este pivote (0–1).")]
    [Range(0f, 1f)]
    public float factorReduccionVolumen = 0.25f;

    [Tooltip("Fracción de la densidad vehicular que se reduce al colocar este pivote (0–1).")]
    [Range(0f, 1f)]
    public float factorReduccionDensidad = 0.30f;

    // ─── Estado (leído por NodoSnapZone y GraphManager) ───────────────────────
    /// <summary>True cuando el pivote está colocado y su mejora está activa.</summary>
    public bool EstaAplicado { get; private set; }

    /// <summary>Nodo al que fue anclado este pivote. Null si no está colocado.</summary>
    public HologramNodeFeedback NodoAnclado { get; private set; }

    private XRGrabInteractable _grab;

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
    }

    // ─── API pública (llamada por NodoSnapZone) ───────────────────────────────

    /// <summary>
    /// Marca el pivote como aplicado y desactiva el grab mientras está colocado.
    /// El impacto sobre el grafo lo procesa NodoSnapZone llamando a GraphManager.
    /// </summary>
    public void MarcarComoAplicado(HologramNodeFeedback nodo)
    {
        if (EstaAplicado) return;
        NodoAnclado  = nodo;
        EstaAplicado = true;
        _grab.enabled = false;
    }

    /// <summary>
    /// Marca el pivote como libre y reactiva el grab.
    /// La reversión del impacto la gestiona NodoSnapZone llamando a GraphManager.
    /// </summary>
    public void MarcarComoRetirado()
    {
        NodoAnclado  = null;
        EstaAplicado = false;
        _grab.enabled = true;
    }
}

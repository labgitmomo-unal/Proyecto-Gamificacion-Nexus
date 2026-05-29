using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Objeto agarrable en VR que representa una medida de movilidad urbana.
/// Debe estar en la Interaction Layer "Soluciones" para ser aceptado por NodoSnapZone.
/// La aplicación y reversión del impacto sobre el grafo es gestionada por NodoSnapZone
/// a través de GraphManager.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class MejoraMovilidad : MonoBehaviour
{
    // ─── Configuración ────────────────────────────────────────────────────────
    [Header("Tipo de Solución")]
    [Tooltip("Tipo de medida de movilidad que implementa este objeto.")]
    public TipoPivote tipoPivote = TipoPivote.SemaforoInteligente;

    [Header("Factores de Reducción")]
    [Tooltip("Fracción del volumen de pasajeros que se reduce al colocar esta mejora (0–1).")]
    [Range(0f, 1f)]
    public float factorReduccionVolumen = 0.25f;

    [Tooltip("Fracción de la densidad vehicular que se reduce al colocar esta mejora (0–1).")]
    [Range(0f, 1f)]
    public float factorReduccionDensidad = 0.30f;

    // ─── Estado ───────────────────────────────────────────────────────────────
    /// <summary>True cuando la mejora está colocada en un nodo y su impacto está activo.</summary>
    public bool EstaAplicado { get; private set; }

    /// <summary>Nodo al que fue anclada esta mejora. Null si no está colocada.</summary>
    public HologramNodeFeedback NodoAnclado { get; private set; }

    private XRGrabInteractable _grab;

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
    }

    // ─── API pública (llamada por NodoSnapZone) ───────────────────────────────

    /// <summary>
    /// Marca la mejora como aplicada y desactiva el grab mientras está colocada.
    /// </summary>
    public void MarcarComoAplicado(HologramNodeFeedback nodo)
    {
        if (EstaAplicado) return;
        NodoAnclado  = nodo;
        EstaAplicado = true;
        _grab.enabled = false;
    }

    /// <summary>
    /// Marca la mejora como libre y reactiva el grab para que pueda ser tomada de nuevo.
    /// </summary>
    public void MarcarComoRetirado()
    {
        NodoAnclado  = null;
        EstaAplicado = false;
        _grab.enabled = true;
    }
}

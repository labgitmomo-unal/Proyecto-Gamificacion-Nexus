// NodoSnapZone v3 — XRI 3.x — clase única
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Zona de anclaje (snap zone) asociada a un nodo del grafo holográfico.
/// Compatible con XRI 3.x: envuelve XRSocketInteractor.
///
/// Requisitos para que un snap sea válido:
///   1. El objeto debe tener el componente MejoraMovilidad.
///   2. El objeto debe estar en la Interaction Layer "Soluciones".
///   3. El presupuesto ALOM no debe estar agotado (máx. 3 pivotes).
///
/// Al validar el snap llama a GraphManager para aplicar la mejora y
/// dispara el pulso de emisión URP en el nodo.
/// Se deshabilita automáticamente cuando el presupuesto ALOM se agota,
/// y se reactiva cuando una mejora es retirada.
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public class NodoSnapZone : MonoBehaviour
{
    private const string NombreCapaSoluciones = "Soluciones";

    [Header("Nodo Asociado")]
    [Tooltip("HologramNodeFeedback del nodo al que pertenece esta snap zone.")]
    public HologramNodeFeedback nodoAsociado;

    private XRSocketInteractor _socket;

    /// <summary>True cuando hay una mejora actualmente anclada en este socket.</summary>
    private bool _ocupado;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _socket = GetComponent<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        _socket.selectEntered.AddListener(OnSelectEntered);
        _socket.selectExited.AddListener(OnSelectExited);
        GraphManager.OnPresupuestoCambiado += OnPresupuestoCambiado;
    }

    private void OnDisable()
    {
        _socket.selectEntered.RemoveListener(OnSelectEntered);
        _socket.selectExited.RemoveListener(OnSelectExited);
        GraphManager.OnPresupuestoCambiado -= OnPresupuestoCambiado;
    }

    // ─── Callback de presupuesto ALOM ─────────────────────────────────────────

    /// <summary>
    /// Recibido desde GraphManager.OnPresupuestoCambiado.
    /// Deshabilita el socket si el presupuesto se agotó y este nodo está libre.
    /// Lo reactiva cuando una mejora es retirada en cualquier nodo.
    /// </summary>
    private void OnPresupuestoCambiado(bool agotado)
    {
        if (!_ocupado)
            _socket.enabled = !agotado;
    }

    // ─── Eventos XRI ─────────────────────────────────────────────────────────

    /// <summary>
    /// Disparado por XRSocketInteractor cuando un interactable entra en la zona.
    /// Valida capa "Soluciones", componente MejoraMovilidad y presupuesto ALOM.
    /// </summary>
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (nodoAsociado == null) return;

        // ── Validar Interaction Layer "Soluciones" ────────────────────────────
        if (!ValidarCapaSoluciones(args.interactableObject))
        {
            Debug.LogWarning("[NodoSnapZone] Snap rechazado: el objeto no está en la " +
                             $"Interaction Layer '{NombreCapaSoluciones}'.");
            return;
        }

        // ── Validar componente MejoraMovilidad ────────────────────────────────
        MejoraMovilidad mejora = args.interactableObject.transform
                                     .GetComponent<MejoraMovilidad>();
        if (mejora == null)
        {
            Debug.LogWarning("[NodoSnapZone] Snap rechazado: el objeto no tiene MejoraMovilidad.");
            return;
        }

        if (mejora.EstaAplicado) return;

        // ── Validar presupuesto ALOM ───────────────────────────────────────────
        GraphManager gm = GraphManager.Instance;
        if (gm == null) return;

        if (gm.PresupuestoAgotado)
        {
            Debug.LogWarning("[NodoSnapZone] Snap rechazado: presupuesto ALOM agotado " +
                             $"({gm.MedidasActivas}/{gm.maxMedidas}).");
            return;
        }

        // ── Aplicar mejora ────────────────────────────────────────────────────
        mejora.MarcarComoAplicado(nodoAsociado);
        gm.AplicarPivoteEnNodo(nodoAsociado, mejora);

        // ── Pulso de confirmación URP ──────────────────────────────────────────
        nodoAsociado.DispararPulsoConfirmacion();
        _ocupado = true;
    }

    /// <summary>
    /// Disparado por XRSocketInteractor cuando el interactable es retirado.
    /// Revierte la mejora y reactiva el socket si el presupuesto lo permite.
    /// </summary>
    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (nodoAsociado == null) return;

        MejoraMovilidad mejora = args.interactableObject.transform
                                     .GetComponent<MejoraMovilidad>();
        if (mejora == null || !mejora.EstaAplicado) return;

        GraphManager gm = GraphManager.Instance;
        if (gm == null) return;

        gm.RevertirPivoteDeNodo(nodoAsociado, mejora);
        mejora.MarcarComoRetirado();
        _ocupado = false;

        // Re-habilitar este socket ya que ahora está libre
        _socket.enabled = true;
    }

    // ─── Validación de Capa ───────────────────────────────────────────────────

    /// <summary>
    /// Verifica que el interactable esté en la Interaction Layer "Soluciones".
    /// Si la capa no existe aún en el proyecto, permite el paso y emite un aviso.
    /// </summary>
    private static bool ValidarCapaSoluciones(IXRSelectInteractable interactable)
    {
        if (interactable is not XRBaseInteractable baseInteractable) return true;

        int mask = InteractionLayerMask.GetMask(NombreCapaSoluciones);
        if (mask == 0)
        {
            Debug.LogWarning($"[NodoSnapZone] Interaction Layer '{NombreCapaSoluciones}' no encontrada. " +
                             "Añádela en Project Settings > XR Interaction Toolkit > Interaction Layer Settings.");
            return true; // Permisivo hasta que la capa esté configurada
        }

        return (baseInteractable.interactionLayers.value & mask) != 0;
    }
}

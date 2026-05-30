using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Componente ligero que expone el estado de ocupación de una base del grafo.
/// Delega completamente en XRSocketInteractor.hasSelection para determinar
/// si hay una bola anclada — sin eventos, sin listeners, sin race conditions.
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public class BaseSlot : MonoBehaviour
{
    private XRSocketInteractor _socket;

    /// <summary>True si hay un interactable seleccionado por el socket de esta base.</summary>
    public bool IsOccupied => _socket != null && _socket.hasSelection;

    private void Awake() => _socket = GetComponent<XRSocketInteractor>();
}

using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Detecta cuándo una GraphNodeBall es colocada o retirada sobre una de las bases físicas
/// del sistema holográfico usando un XRSocketInteractor subyacente.
/// Dispara eventos que BaseConnectionManager consume para activar las aristas adyacentes.
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public class BaseSlot : MonoBehaviour
{
    // ─── Estado ───────────────────────────────────────────────────────────────

    /// <summary>True si hay una GraphNodeBall actualmente encajada en este slot.</summary>
    public bool IsOccupied { get; private set; }

    // ─── Eventos ──────────────────────────────────────────────────────────────

    /// <summary>Se dispara cuando una GraphNodeBall válida es colocada sobre la base.</summary>
    public event Action<BaseSlot> OnBallPlaced;

    /// <summary>Se dispara cuando la GraphNodeBall es retirada de la base.</summary>
    public event Action<BaseSlot> OnBallRemoved;

    // ─── Referencias Internas ─────────────────────────────────────────────────

    private XRSocketInteractor _socket;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        _socket = GetComponent<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        _socket.selectEntered.AddListener(OnSelectEntered);
        _socket.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        _socket.selectEntered.RemoveListener(OnSelectEntered);
        _socket.selectExited.RemoveListener(OnSelectExited);
    }

    // ─── Callbacks XRI ────────────────────────────────────────────────────────

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactableObject.transform.GetComponent<GraphNodeBall>() == null) return;

        IsOccupied = true;
        OnBallPlaced?.Invoke(this);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        IsOccupied = false;
        OnBallRemoved?.Invoke(this);
    }
}

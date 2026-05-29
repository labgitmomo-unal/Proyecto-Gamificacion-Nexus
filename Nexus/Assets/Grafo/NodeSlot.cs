using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Casilla de la grilla holográfica que acepta una GraphNodeBall mediante XRSocketInteractor.
/// Dispara eventos cuando un nodo es colocado o retirado, permitiendo que GridGraphManager
/// evalúe qué aristas deben activarse.
/// Inicializado por GridBase a través de Initialize().
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public class NodeSlot : MonoBehaviour
{
    // ─── Constantes ───────────────────────────────────────────────────────────
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private static readonly Color ColorVacio   = new Color(0f, 0.4f, 0.8f) * 1.2f;
    private static readonly Color ColorOcupado = new Color(0f, 1f, 0.4f)   * 3.5f;

    // ─── Estado ───────────────────────────────────────────────────────────────
    /// <summary>Columna en la grilla (0-based).</summary>
    public int GridX { get; private set; }

    /// <summary>Fila en la grilla (0-based).</summary>
    public int GridY { get; private set; }

    /// <summary>Nodo lógico actualmente colocado en este slot. None si está vacío.</summary>
    public NodeID CurrentNode { get; private set; } = NodeID.None;

    /// <summary>True si hay una bola colocada en este slot.</summary>
    public bool IsOccupied => CurrentNode != NodeID.None;

    // ─── Eventos ──────────────────────────────────────────────────────────────
    /// <summary>Se dispara cuando una GraphNodeBall válida es colocada en el slot.</summary>
    public event Action<NodeSlot, NodeID> OnNodePlaced;

    /// <summary>Se dispara cuando la GraphNodeBall es retirada del slot.</summary>
    public event Action<NodeSlot, NodeID> OnNodeRemoved;

    // ─── Referencias Internas ─────────────────────────────────────────────────
    private XRSocketInteractor _socket;
    private Renderer _anilloRenderer;
    private MaterialPropertyBlock _mpb;

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        _socket = GetComponent<XRSocketInteractor>();
        _mpb    = new MaterialPropertyBlock();
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

    // ─── Inicialización (llamada por GridBase) ────────────────────────────────

    /// <summary>
    /// Configura la posición en la grilla y el renderer del anillo visual.
    /// Debe llamarse inmediatamente tras AddComponent en GridBase.
    /// </summary>
    public void Initialize(int col, int fila, Renderer anilloRenderer)
    {
        GridX          = col;
        GridY          = fila;
        _anilloRenderer = anilloRenderer;
        ActualizarVisual();
    }

    // ─── Callbacks XRI ────────────────────────────────────────────────────────
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        var ball = args.interactableObject.transform.GetComponent<GraphNodeBall>();
        if (ball == null || ball.nodeID == NodeID.None) return;

        CurrentNode = ball.nodeID;
        ActualizarVisual();
        OnNodePlaced?.Invoke(this, CurrentNode);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        var nodoRemovido = CurrentNode;
        CurrentNode = NodeID.None;
        ActualizarVisual();
        OnNodeRemoved?.Invoke(this, nodoRemovido);
    }

    // ─── Visual ───────────────────────────────────────────────────────────────
    /// <summary>Cambia el color del anillo según el estado de ocupación del slot.</summary>
    private void ActualizarVisual()
    {
        if (_anilloRenderer == null) return;
        _anilloRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(EmissionColorID, IsOccupied ? ColorOcupado : ColorVacio);
        _anilloRenderer.SetPropertyBlock(_mpb);
    }
}

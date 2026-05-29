using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona la activación de aristas del grafo holográfico basándose en la
/// colocación de GraphNodeBalls en los NodeSlots de la grilla.
///
/// Lógica central:
///   - Cuando ambos nodos extremo de una arista están colocados en cualquier slot,
///     activa el LineRenderer y actualiza sus posiciones extremas a los slots ocupados.
///   - Si cualquiera de los dos nodos es retirado, desactiva el LineRenderer.
///
/// Los slots son registrados por GridBase en Awake vía RegistrarSlot().
/// </summary>
public class GridGraphManager : MonoBehaviour
{
    // ─── Tipos de Datos ───────────────────────────────────────────────────────

    [Serializable]
    public struct DefinicionArista
    {
        [Tooltip("Primer nodo extremo de la arista.")]
        public NodeID nodoA;

        [Tooltip("Segundo nodo extremo de la arista.")]
        public NodeID nodoB;

        [Tooltip("LineRenderer que dibuja la conexión cuando ambos nodos están colocados.")]
        public LineRenderer lineRenderer;
    }

    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Aristas del Grafo")]
    [Tooltip("Define qué par de NodeIDs activa cada LineRenderer existente en la escena.")]
    [SerializeField] private List<DefinicionArista> _aristas = new();

    // ─── Estado Interno ───────────────────────────────────────────────────────

    private readonly List<NodeSlot> _slots = new();

    /// <summary>Mapa de nodo lógico → slot donde está actualmente colocado.</summary>
    private readonly Dictionary<NodeID, NodeSlot> _nodosColocados = new();

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Todas las aristas inician inactivas; se activan al completar pares de nodos.
        foreach (var arista in _aristas)
        {
            if (arista.lineRenderer != null)
                arista.lineRenderer.enabled = false;
        }
    }

    private void OnDisable()
    {
        foreach (var slot in _slots)
        {
            slot.OnNodePlaced  -= HandleNodePlaced;
            slot.OnNodeRemoved -= HandleNodeRemoved;
        }
    }

    // ─── API Pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Registra un NodeSlot para que GridGraphManager escuche sus eventos de colocación.
    /// Llamado automáticamente por GridBase durante la generación de la grilla.
    /// </summary>
    public void RegistrarSlot(NodeSlot slot)
    {
        if (slot == null || _slots.Contains(slot)) return;
        _slots.Add(slot);
        slot.OnNodePlaced  += HandleNodePlaced;
        slot.OnNodeRemoved += HandleNodeRemoved;
    }

    // ─── Handlers de Slot ─────────────────────────────────────────────────────

    private void HandleNodePlaced(NodeSlot slot, NodeID nodeID)
    {
        _nodosColocados[nodeID] = slot;
        RefrescarAristas();
    }

    private void HandleNodeRemoved(NodeSlot slot, NodeID nodeID)
    {
        _nodosColocados.Remove(nodeID);
        RefrescarAristas();
    }

    // ─── Lógica de Aristas ────────────────────────────────────────────────────

    /// <summary>
    /// Evalúa todas las aristas definidas y activa/desactiva su LineRenderer según
    /// si ambos nodos extremos están actualmente colocados en algún slot de la grilla.
    /// Actualiza las posiciones de extremo del LineRenderer al mundo de los slots.
    /// </summary>
    private void RefrescarAristas()
    {
        foreach (var arista in _aristas)
        {
            if (arista.lineRenderer == null) continue;

            var tieneA = _nodosColocados.TryGetValue(arista.nodoA, out var slotA);
            var tieneB = _nodosColocados.TryGetValue(arista.nodoB, out var slotB);

            if (tieneA && tieneB)
            {
                arista.lineRenderer.enabled = true;
                arista.lineRenderer.SetPosition(0, slotA.transform.position);
                arista.lineRenderer.SetPosition(1, slotB.transform.position);
            }
            else
            {
                arista.lineRenderer.enabled = false;
            }
        }
    }
}

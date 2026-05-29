using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Genera proceduralmente una grilla NxM de NodeSlots sobre una base holográfica plana.
///
/// En Awake:
///   1. Crea un cubo plano como base visual.
///   2. Instancia N×M slots con un anillo visual (cilindro), XRSocketInteractor y NodeSlot.
///   3. Registra cada slot en GridGraphManager para que pueda evaluar aristas.
///
/// Configuración en Inspector: columnas, filas, espaciado, material y referencia al GridGraphManager.
/// </summary>
public class GridBase : MonoBehaviour
{
    // ─── Constantes Visuales ──────────────────────────────────────────────────
    private const float AlturaSlots    = 0.012f;
    private const float RadioAnillo    = 0.075f;
    private const float AltoAnillo     = 0.004f;

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Dimensiones de la Grilla")]
    [SerializeField] [Range(2, 6)]
    private int _columnas = 4;

    [SerializeField] [Range(2, 6)]
    private int _filas = 4;

    [SerializeField]
    [Tooltip("Distancia en metros entre centros de slots adyacentes.")]
    private float _espaciado = 0.20f;

    [Header("Base Plana")]
    [SerializeField]
    [Tooltip("Material holográfico para la base y los anillos de cada slot.")]
    private Material _materialHolograma;

    [SerializeField]
    [Tooltip("Altura del cubo base en metros.")]
    private float _alturaBase = 0.008f;

    [SerializeField]
    [Tooltip("Margen extra alrededor del grid en la base plana.")]
    private float _margen = 0.08f;

    [Header("Dependencia")]
    [SerializeField]
    [Tooltip("GridGraphManager que recibirá los slots generados. " +
             "Puede estar en el mismo GameObject.")]
    private GridGraphManager _graphManager;

    // ─── Estado ───────────────────────────────────────────────────────────────
    private readonly List<NodeSlot> _slots = new();

    /// <summary>Lista de todos los NodeSlots generados por esta base.</summary>
    public IReadOnlyList<NodeSlot> Slots => _slots;

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        CrearBasePlana();
        GenerarSlots();
    }

    // ─── Generación de Base ───────────────────────────────────────────────────

    /// <summary>Crea el cubo plano que actúa como soporte visual de toda la grilla.</summary>
    private void CrearBasePlana()
    {
        float ancho = (_columnas - 1) * _espaciado + 2f * _margen;
        float largo = (_filas    - 1) * _espaciado + 2f * _margen;

        var baseGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseGO.name = "Base_Visual";
        baseGO.transform.SetParent(transform, false);
        baseGO.transform.localPosition = new Vector3(0f, -_alturaBase * 0.5f, 0f);
        baseGO.transform.localScale    = new Vector3(ancho, _alturaBase, largo);

        // La base no debe tener colisión propia; los sockets manejan la interacción.
        Destroy(baseGO.GetComponent<Collider>());

        if (_materialHolograma != null)
            baseGO.GetComponent<Renderer>().sharedMaterial = _materialHolograma;
    }

    // ─── Generación de Slots ──────────────────────────────────────────────────

    /// <summary>Instancia los N×M slots centrados en el transform local.</summary>
    private void GenerarSlots()
    {
        float offsetX = (_columnas - 1) * _espaciado * 0.5f;
        float offsetZ = (_filas    - 1) * _espaciado * 0.5f;

        for (int fila = 0; fila < _filas; fila++)
        {
            for (int col = 0; col < _columnas; col++)
            {
                var posLocal = new Vector3(
                    col * _espaciado - offsetX,
                    AlturaSlots,
                    fila * _espaciado - offsetZ
                );

                var slot = CrearSlot(col, fila, posLocal);
                _slots.Add(slot);
                _graphManager?.RegistrarSlot(slot);
            }
        }
    }

    /// <summary>
    /// Crea un slot individual con su anillo visual, XRSocketInteractor y NodeSlot.
    /// Orden de AddComponent garantizado: primero XRSocketInteractor (requerido por NodeSlot).
    /// </summary>
    private NodeSlot CrearSlot(int col, int fila, Vector3 posLocal)
    {
        var slotGO = new GameObject($"Slot_{col}_{fila}");
        slotGO.transform.SetParent(transform, false);
        slotGO.transform.localPosition = posLocal;

        // ── Anillo visual (cilindro muy plano) ───────────────────────────────
        var anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anillo.name = "Anillo_Visual";
        anillo.transform.SetParent(slotGO.transform, false);
        anillo.transform.localPosition = Vector3.zero;
        anillo.transform.localScale    = new Vector3(RadioAnillo * 2f, AltoAnillo, RadioAnillo * 2f);

        // El anillo no aporta colisión; la interacción la gestiona el socket.
        Destroy(anillo.GetComponent<Collider>());

        var anilloRenderer = anillo.GetComponent<Renderer>();
        if (_materialHolograma != null)
            anilloRenderer.sharedMaterial = _materialHolograma;

        // ── XRSocketInteractor (primero, satisface RequireComponent de NodeSlot) ──
        var socket = slotGO.AddComponent<XRSocketInteractor>();
        socket.interactionLayers = new InteractionLayerMask { value = ~0 };

        // ── NodeSlot ──────────────────────────────────────────────────────────
        var nodeSlot = slotGO.AddComponent<NodeSlot>();
        nodeSlot.Initialize(col, fila, anilloRenderer);

        return nodeSlot;
    }
}

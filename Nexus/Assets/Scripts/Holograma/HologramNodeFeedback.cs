using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Gestiona el feedback visual de un nodo del grafo holográfico.
/// Al recibir eventos hover desde XRSimpleInteractable, actualiza su propia
/// emisión y notifica al GraphManager para que resalte las aristas conectadas.
/// </summary>
public class HologramNodeFeedback : MonoBehaviour
{
    [Header("Configuración de Neón")]
    public MeshRenderer nodeRenderer;
    public Color hoverColor = Color.cyan;
    private Color originalColor;
    private Material nodeMaterial;

    [Header("Intensidad ALOM")]
    public float normalIntensity = 1.0f;
    public float hoverIntensity = 4.0f;

    private void Start()
    {
        // Instanciamos el material para no afectar al resto de nodos
        nodeMaterial = nodeRenderer.material;
        originalColor = nodeMaterial.GetColor("_EmissionColor");
    }

    /// <summary>
    /// Llamado por XRSimpleInteractable (evento hoverEntered).
    /// Resalta el nodo y notifica al GraphManager para resaltar las aristas conectadas.
    /// </summary>
    public void OnHoverEnter()
    {
        nodeMaterial.SetColor("_EmissionColor", hoverColor * hoverIntensity);
        GraphManager.Instance?.OnNodoHoverEnter(this);
    }

    /// <summary>
    /// Llamado por XRSimpleInteractable (evento lastHoverExited).
    /// Restaura el estado base del nodo y sus aristas.
    /// </summary>
    public void OnHoverExit()
    {
        nodeMaterial.SetColor("_EmissionColor", originalColor * normalIntensity);
        GraphManager.Instance?.OnNodoHoverExit(this);
    }
}

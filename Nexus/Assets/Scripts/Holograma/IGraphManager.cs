/// <summary>
/// Interfaz que desacopla MejoraMovilidad de la implementación concreta de GraphManager.
/// </summary>
public interface IGraphManager
{
    /// <summary>Aplica el impacto de una mejora sobre las rutas del nodo.</summary>
    void AplicarPivoteEnNodo(HologramNodeFeedback nodo, MejoraMovilidad mejora);

    /// <summary>Revierte el impacto de una mejora retirada.</summary>
    void RevertirPivoteDeNodo(HologramNodeFeedback nodo, MejoraMovilidad mejora);
}

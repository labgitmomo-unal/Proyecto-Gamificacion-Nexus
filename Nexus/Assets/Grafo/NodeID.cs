/// <summary>
/// Identificador único para cada nodo lógico del grafo holográfico.
/// Usado por GraphNodeBall y GridGraphManager para emparejar bolas con aristas.
/// </summary>
public enum NodeID
{
    None   = 0,
    Norte  = 1,
    Sur    = 2,
    Centro = 3,
    Este   = 4,
    Oeste  = 5
}

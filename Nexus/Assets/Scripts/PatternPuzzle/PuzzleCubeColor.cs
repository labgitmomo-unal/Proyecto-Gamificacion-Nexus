namespace PatternPuzzle
{
    /// <summary>
    /// Identificador logico de color, usado tanto por los soportes (secuencia esperada)
    /// como por los cubos interactuables del dispensador.
    /// Agregar nuevos valores aqui permite reutilizar la mecanica con mas colores
    /// sin tocar ninguna otra clase del sistema.
    /// </summary>
    public enum PuzzleCubeColor
    {
        None,
        Green,
        Yellow,
        Red,
        Blue,
        Purple,
        Cyan,
        Orange
    }
}

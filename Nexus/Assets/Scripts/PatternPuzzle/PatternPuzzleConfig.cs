using UnityEngine;

namespace PatternPuzzle
{
    /// <summary>
    /// Datos de un reto de patrones. Crear un asset distinto por cada estacion/nivel
    /// permite reutilizar toda la logica (PuzzleSlot, PuzzleCube, PatternPuzzleManager)
    /// simplemente cambiando este archivo, sin tocar codigo.
    /// </summary>
    [CreateAssetMenu(fileName = "PatternPuzzleConfig", menuName = "Puzzles/Pattern Puzzle Config")]
    public class PatternPuzzleConfig : ScriptableObject
    {
        [Header("Secuencia visible (solo informativo, referencia para el diseñador)")]
        [Tooltip("Colores mostrados en los soportes ya llenos, de izquierda a derecha.")]
        public PuzzleCubeColor[] visibleSequence;

        [Header("Respuesta")]
        [Tooltip("Color correcto que debe colocar el jugador en el soporte vacio.")]
        public PuzzleCubeColor correctAnswer = PuzzleCubeColor.Red;

        [Header("Colores disponibles en el dispensador")]
        public PuzzleCubeColor[] availableColors;

        [Header("Timing de validacion")]
        [Tooltip("Retardo entre cada soporte al iluminarse en la secuencia correcta.")]
        public float stepDelay = 0.25f;

        [Tooltip("Cuanto tiempo permanecen en rojo todos los soportes cuando la respuesta es incorrecta.")]
        public float errorDuration = 1f;

        [Tooltip("Duracion de la animacion de retorno del cubo al dispensador tras un error.")]
        public float returnToDispenserDuration = 0.6f;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PatternPuzzle
{
    /// <summary>
    /// Orquesta la validacion del reto de reconocimiento de patrones.
    /// No necesita conocer la secuencia de los soportes ya llenos (eso lo decide
    /// el diseñador colocando manualmente los cubos/colores en la escena); solo
    /// necesita la respuesta correcta para el soporte vacio y la lista completa
    /// de soportes para la animacion de validacion progresiva.
    /// </summary>
    public class PatternPuzzleManager : MonoBehaviour
    {
        [Header("Configuracion")]
        [SerializeField] private PatternPuzzleConfig config;

        [Header("Soportes (en orden, de izquierda a derecha)")]
        [Tooltip("El ultimo elemento del arreglo debe ser el soporte vacio/interactivo.")]
        [SerializeField] private PuzzleSlot[] slots;

        [Header("Audio")]
        [SerializeField] private AudioSource successSound;
        [SerializeField] private AudioSource errorSound;

        [Header("Eventos")]
        [Tooltip("Se invoca una sola vez, cuando el jugador coloca el cubo correcto y termina la animacion de validacion.")]
        public UnityEvent onChallengeCompleted;

        public bool IsComplete { get; private set; }

        private PuzzleSlot _emptySlot;
        private bool _isValidating;

        private void Awake()
        {
            Debug.Log("Cantidad de slots: " + slots.Length);
            if (slots != null && slots.Length > 0)
            {
                _emptySlot = slots[slots.Length - 1];
                foreach (var slot in slots)
                {
                    if (slot != null) slot.Manager = this;
                }
            }
        }

        public void OnCubePlaced(PuzzleCube cube, PuzzleSlot slot)
        {
            Debug.Log("Se colocó un cubo: " + cube.color);
            if (IsComplete || _isValidating || cube.IsValidated) return;
            if (slot != _emptySlot) return; // solo el soporte vacio dispara validacion

            if (cube.color == config.correctAnswer)
                StartCoroutine(ValidateSuccess(cube));
            else
                StartCoroutine(ValidateError(cube));
        }

        public void OnCubeRemoved(PuzzleCube cube, PuzzleSlot slot)
        {
            // El jugador retiro el cubo manualmente antes de que corra la validacion;
            // no se requiere accion especial, el socket queda libre de nuevo.
        }

        private IEnumerator ValidateSuccess(PuzzleCube cube)
        {
            _isValidating = true;
            _emptySlot.SetSocketActive(false);

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].SetState(PuzzleSlotState.Success);
                yield return new WaitForSeconds(config.stepDelay);
            }

            if (successSound != null) successSound.Play();

            cube.Lock();
            IsComplete = true;
            _isValidating = false;

            onChallengeCompleted?.Invoke();
        }

        private IEnumerator ValidateError(PuzzleCube cube)
        {
            _isValidating = true;
            _emptySlot.SetSocketActive(false);

            foreach (var slot in slots)
                slot.SetState(PuzzleSlotState.Error);

            if (errorSound != null) errorSound.Play();

            yield return new WaitForSeconds(config.errorDuration);

            foreach (var slot in slots)
                slot.SetState(PuzzleSlotState.Normal);

            _emptySlot.EjectCube();
            cube.ReturnToDispenser(config.returnToDispenserDuration);

            _emptySlot.SetSocketActive(true);
            _isValidating = false;
        }
    }
}

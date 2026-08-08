using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PatternPuzzle
{
    public enum PuzzleSlotState { Normal, Success, Error }

    /// <summary>
    /// Representa un soporte de la consola. Controla su propia emision visual
    /// (Normal / Success / Error) y, si tiene un XRSocketInteractor asignado,
    /// notifica al PatternPuzzleManager cuando el jugador coloca o retira un cubo.
    /// Los soportes decorativos (ya con un cubo fijo) no necesitan socket, solo
    /// reciben SetState() durante la animacion de validacion.
    /// </summary>
    public class PuzzleSlot : MonoBehaviour
    {
        [Header("Referencias visuales")]
        [Tooltip("Renderer cuyo material se usara para el efecto de emision. Si se deja vacio, se busca en los hijos.")]
        [SerializeField] private Renderer targetRenderer;

        [Header("Colores de emision")]
        [SerializeField] private Color successEmission = Color.green;
        [SerializeField] private Color errorEmission = Color.red;
        [SerializeField] private float emissionIntensity = 2f;
        [SerializeField] private float transitionDuration = 0.15f;

        [Header("Socket (solo el soporte vacio/interactivo lo necesita)")]
        [Tooltip("Dejalo vacio en los soportes decorativos que ya vienen con un cubo.")]
        [SerializeField] private XRSocketInteractor socket;

        public PatternPuzzleManager Manager { get; set; }
        public bool IsInteractive => socket != null;

        private Material _materialInstance;
        private Color _originalEmission;
        private bool _hasEmissionProperty;
        private Coroutine _transitionRoutine;

        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();

            if (targetRenderer != null)
            {
                _materialInstance = targetRenderer.material; // instancia propia, no comparte con otros soportes
                _hasEmissionProperty = _materialInstance.HasProperty("_EmissionColor");
                if (_hasEmissionProperty)
                {
                    _materialInstance.EnableKeyword("_EMISSION");
                    _materialInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    _originalEmission = _materialInstance.GetColor("_EmissionColor");
                }
            }
        }

        private void OnEnable()
        {
            if (socket != null)
            {
                socket.selectEntered.AddListener(HandleCubePlaced);
                socket.selectExited.AddListener(HandleCubeRemoved);
            }
        }

        private void OnDisable()
        {
            if (socket != null)
            {
                socket.selectEntered.RemoveListener(HandleCubePlaced);
                socket.selectExited.RemoveListener(HandleCubeRemoved);
            }
        }

        private void HandleCubePlaced(SelectEnterEventArgs args)
        {
            var cube = args.interactableObject.transform.GetComponent<PuzzleCube>();
            if (cube == null || Manager == null) return;

            Manager.OnCubePlaced(cube, this);
        }

        private void HandleCubeRemoved(SelectExitEventArgs args)
        {
            var cube = args.interactableObject.transform.GetComponent<PuzzleCube>();
            if (cube == null || Manager == null) return;
            Manager.OnCubeRemoved(cube, this);
        }

        /// <summary>Bloquea o desbloquea la aceptacion de nuevos cubos (usado durante la validacion).</summary>
        public void SetSocketActive(bool active)
        {
            if (socket != null)
                socket.socketActive = active;
        }

        /// <summary>Fuerza al socket a soltar el cubo que tenga seleccionado (para devolver un cubo incorrecto).</summary>
        public void EjectCube()
        {
            if (socket != null && socket.hasSelection && socket.interactionManager != null)
                socket.interactionManager.CancelInteractorSelection((IXRSelectInteractor)socket);
        }

        public void SetState(PuzzleSlotState state)
        {
            if (!_hasEmissionProperty) return;

            Color target;
            switch (state)
            {
                case PuzzleSlotState.Success:
                    target = successEmission * emissionIntensity;
                    break;
                case PuzzleSlotState.Error:
                    target = errorEmission * emissionIntensity;
                    break;
                default:
                    target = _originalEmission;
                    break;
            }

            if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
            _transitionRoutine = StartCoroutine(TransitionEmission(target));
        }

        private IEnumerator TransitionEmission(Color target)
        {
            Color start = _materialInstance.GetColor("_EmissionColor");
            float t = 0f;
            while (t < transitionDuration)
            {
                t += Time.deltaTime;
                _materialInstance.SetColor("_EmissionColor", Color.Lerp(start, target, t / transitionDuration));
                yield return null;
            }
            _materialInstance.SetColor("_EmissionColor", target);
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;


namespace PatternPuzzle
{
    /// <summary>
    /// Cubo interactuable del dispensador. Solo conoce su color, su posicion inicial
    /// (para poder regresar tras un error) y si ya fue validado correctamente.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class PuzzleCube : MonoBehaviour
    {
        [Tooltip("Color logico de este cubo. Debe coincidir con el material/arte asignado en el prefab.")]
        public PuzzleCubeColor color = PuzzleCubeColor.None;

        public bool IsValidated { get; private set; }
        public Vector3 OriginalScale => _dispenserScale;

        private XRGrabInteractable _grabInteractable;
        private Rigidbody _rigidbody;
        private Vector3 _dispenserPosition;
        private Quaternion _dispenserRotation;
        private Vector3 _dispenserScale;
        private Transform _dispenserParent;
        private Coroutine _returnRoutine;

        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _grabInteractable.trackScale = false;
            _rigidbody = GetComponent<Rigidbody>();

            // Posicion/rotacion/scale/parent inicial dentro del dispensador
            _dispenserPosition = transform.position;
            _dispenserRotation = transform.rotation;
            _dispenserScale = transform.localScale;
            _dispenserParent = transform.parent;

            _grabInteractable.selectEntered.AddListener(OnGrabbed);
            _grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            Debug.Log($"PuzzleCube {color} GRABBED by {args.interactorObject.transform.name} | localScale: {transform.localScale} | lossyScale: {transform.lossyScale} | parent: {(transform.parent ? transform.parent.name : "null")}");
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            Debug.Log($"PuzzleCube {color} RELEASED from {args.interactorObject.transform.name} | localScale: {transform.localScale} | lossyScale: {transform.lossyScale} | parent: {(transform.parent ? transform.parent.name : "null")}");
        }

        public void SetInteractable(bool value)
        {
            if (_grabInteractable != null)
                _grabInteractable.enabled = value;
        }

        /// <summary>Bloquea el cubo de forma permanente (reto completado).</summary>
        public void Lock()
        {
            IsValidated = true;
            SetInteractable(false);
        }

        /// <summary>Anima el regreso suave del cubo a su posicion original en el dispensador. Nunca se destruye.</summary>
        public void ReturnToDispenser(float duration)
        {
            if (_returnRoutine != null) StopCoroutine(_returnRoutine);
            _returnRoutine = StartCoroutine(ReturnRoutine(duration));
        }

        private IEnumerator ReturnRoutine(float duration)
        {
            // Mientras regresa, que nadie pueda volver a agarrarlo a mitad de camino
            SetInteractable(false);

            if (_rigidbody != null) _rigidbody.isKinematic = true;

            transform.SetParent(_dispenserParent, true);
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Vector3 startScale = transform.localScale;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = t / duration;
                transform.position = Vector3.Lerp(startPos, _dispenserPosition, k);
                transform.rotation = Quaternion.Lerp(startRot, _dispenserRotation, k);
                transform.localScale = Vector3.Lerp(startScale, _dispenserScale, k);
                yield return null;
            }
            transform.position = _dispenserPosition;
            transform.rotation = _dispenserRotation;
            transform.localScale = _dispenserScale;

            SetInteractable(true);
        }
    }
}

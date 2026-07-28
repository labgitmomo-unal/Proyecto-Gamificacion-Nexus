using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

        private XRGrabInteractable _grabInteractable;
        private Rigidbody _rigidbody;
        private Vector3 _dispenserPosition;
        private Quaternion _dispenserRotation;
        private Transform _dispenserParent;
        private Coroutine _returnRoutine;

        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _rigidbody = GetComponent<Rigidbody>();

            // Posicion/rotacion/parent inicial dentro del dispensador
            _dispenserPosition = transform.position;
            _dispenserRotation = transform.rotation;
            _dispenserParent = transform.parent;
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
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = t / duration;
                transform.position = Vector3.Lerp(startPos, _dispenserPosition, k);
                transform.rotation = Quaternion.Lerp(startRot, _dispenserRotation, k);
                yield return null;
            }
            transform.position = _dispenserPosition;
            transform.rotation = _dispenserRotation;

            SetInteractable(true);
        }
    }
}

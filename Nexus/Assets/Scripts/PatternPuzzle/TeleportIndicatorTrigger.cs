using UnityEngine;

namespace PatternPuzzle
{
    [RequireComponent(typeof(BoxCollider))]
    public class TeleportIndicatorTrigger : MonoBehaviour
    {
        [Tooltip("Referencia al Nivel_Patrones_Organizer que controla el nivel.")]
        public Nivel_Patrones_Organizer organizer;

        private bool triggered;

        private void Reset()
        {
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered)
                return;

            if (!other.CompareTag("Player"))
                return;

            triggered = true;

            if (organizer != null)
                organizer.PlayReto2Intro();
        }

        public void ResetTrigger()
        {
            triggered = false;
        }
    }
}

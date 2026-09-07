using UnityEngine;

namespace PatternPuzzle
{
    [RequireComponent(typeof(Collider))]
    public class ActivacionPanelTrigger : MonoBehaviour
    {
        [Tooltip("Referencia al Nivel_Patrones_Organizer que controla el nivel.")]
        public Nivel_Patrones_Organizer organizer;

        private bool triggered;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered)
                return;

            if (!other.CompareTag("Player"))
                return;

            triggered = true;
            Debug.Log("[ActivacionPanelTrigger] Jugador entró al panel de Reto 2.");

            if (organizer != null)
                organizer.PlayReto2Intro();
            else
                Debug.LogWarning("[ActivacionPanelTrigger] Organizer es null.");
        }

        public void ResetTrigger()
        {
            triggered = false;
        }
    }
}

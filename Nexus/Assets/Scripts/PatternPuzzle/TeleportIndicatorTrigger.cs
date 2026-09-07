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
            {
                Debug.Log($"[TeleportIndicatorTrigger] Objeto '{other.name}' entró al trigger pero no tiene tag 'Player'.");
                return;
            }

            triggered = true;
            Debug.Log($"[TeleportIndicatorTrigger] Jugador detectado en trigger de Reto 2. Organizer: {(organizer != null ? "asignado" : "NULL")}");

            if (organizer != null)
                organizer.PlayReto2Intro();
            else
                Debug.LogWarning("[TeleportIndicatorTrigger] Organizer es null. No se puede reproducir audio del Reto 2.");
        }

        public void ResetTrigger()
        {
            triggered = false;
        }
    }
}

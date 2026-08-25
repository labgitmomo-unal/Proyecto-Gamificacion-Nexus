using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PatternPuzzle
{
    /// <summary>
    /// Administra la secuencia de inicio del Nivel_Patrones.
    /// Al teletransportarse: reproduce el audio donde Nono explica el nivel y,
    /// cuando termina, inicia la cuenta regresiva del temporizador.
    /// Si no hay audio configurado, el temporizador inicia de inmediato.
    /// </summary>
    public class Nivel_Patrones_Organizer : MonoBehaviour
    {
        [Header("Audio de introducción (voz de Nono)")]
        [Tooltip("Audio que explica el nivel. El tiempo empieza a contar cuando este audio termina. Si se deja vacio, el tiempo inicia de inmediato.")]
        public AudioSource introAudio;

        [Header("Sirena de ambulancia")]
        [Tooltip("Audio de la sirena que se reproduce de forma repetitiva durante el nivel.")]
        public AudioSource sirenAudio;
        [Tooltip("Intervalo en segundos entre reproducciones de la sirena.")]
        public float sirenInterval = 45f;

        [Header("Temporizador")]
        [Tooltip("Temporizador del Nivel_Patrones. Si se deja vacio, se busca automaticamente en la escena.")]
        public Timer_Patrones timer;

        [Header("Eventos")]
        [Tooltip("Se invoca cuando el audio de introduccion termina (antes de iniciar el temporizador).")]
        public UnityEvent onIntroFinished;

        [Tooltip("Se invoca cuando la cuenta regresiva comienza.")]
        public UnityEvent onTimerStarted;

        [Header("Reto 2 - Revelar")]
        [Tooltip("Indicador de teleport del Reto 2. Se activa al completar el Reto 1.")]
        public GameObject smTeleportIndicator2;

        [Tooltip("Trigger/panel de activacion del Reto 2. Se activa al completar el Reto 1.")]
        public GameObject activacionPanel;

        [Header("Audio de instrucciones")]
        [Tooltip("Audio que indica al jugador que debe hacer despues de completar el Reto 1 (por ejemplo, dirigirse al indicador del Reto 2).")]
        public AudioSource reto1CompletadoAudio;

        public Transform nonoDestination;
        public bool IsIntroPlaying { get; private set; }

        private Coroutine sirenRoutine;

        private void Awake()
        {
            if (timer == null)
                timer = FindFirstObjectByType<Timer_Patrones>();
        }

        private void Start()
        {
            var reto1 = FindFirstObjectByType<PatternPuzzleManager>();
            if (reto1 != null)
            {
                reto1.onChallengeCompleted.AddListener(OnReto1Completado);
            }
            else
            {
                Debug.LogWarning("[Nivel_Patrones_Organizer] No se encontro PatternPuzzleManager del Reto 1 para revelar el Reto 2.");
            }
        }

        public void StartLevelIntro()
        {
            if (introAudio != null && introAudio.clip != null)
            {
                introAudio.Play();
                IsIntroPlaying = true;
                StartCoroutine(WaitForIntro());
            }
            else
            {
                StartTimer();
            }
        }

        private IEnumerator WaitForIntro()
        {
            while (introAudio != null && introAudio.isPlaying)
                yield return null;

            IsIntroPlaying = false;
            onIntroFinished?.Invoke();
            StartTimer();
        }

        private void StartSirenLoop()
        {
            if (sirenAudio == null || sirenAudio.clip == null) return;

            if (sirenRoutine != null)
                StopCoroutine(sirenRoutine);

            sirenRoutine = StartCoroutine(SirenRoutine());
        }

        private IEnumerator SirenRoutine()
        {
            float interval = Mathf.Max(0.1f, sirenInterval);

            while (true)
            {
                if (sirenAudio != null)
                    sirenAudio.Play();

                yield return new WaitForSeconds(interval);
            }
        }

        public void StopSiren()
        {
            if (sirenRoutine != null)
            {
                StopCoroutine(sirenRoutine);
                sirenRoutine = null;
            }
        }

        private void OnDisable()
        {
            StopSiren();
        }

        public void StartTimer()
        {
            if (timer != null)
                timer.Start_Timer();

            StartSirenLoop();
            onTimerStarted?.Invoke();
            Debug.Log("[Nivel_Patrones_Organizer] Cuenta regresiva iniciada.");
        }

public void OnReto1Completado()
    {
        if (reto1CompletadoAudio != null && reto1CompletadoAudio.clip != null)
            reto1CompletadoAudio.Play();
        StartCoroutine(WaitForReto1Audio());

        if (smTeleportIndicator2 != null)
            smTeleportIndicator2.SetActive(true);

        if (activacionPanel != null)
            activacionPanel.SetActive(true);

        Debug.Log("[Nivel_Patrones_Organizer] Reto 1 completado: Reto 2 revelado.");
        
        // PAUSA SOLO el Car Line Spawner (2), los demás siguen igual
        BridgeControlManager.Instance.PauseSpawnerByName("Car Line Spawner (2)", true);
    }

        private IEnumerator WaitForReto1Audio()
        {
            if (reto1CompletadoAudio != null)
                yield return new WaitWhile(() => reto1CompletadoAudio.isPlaying);
            
            if (Nono_Guide.Instance != null && nonoDestination != null)
                Nono_Guide.Instance.FlyTo(nonoDestination);

            Debug.Log("[Nivel_Patrones_Organizer] Audio de instrucciones del Reto 2 finalizado.");
        }
    }
}
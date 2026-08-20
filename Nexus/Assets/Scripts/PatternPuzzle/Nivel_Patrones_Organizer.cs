using System.Collections;
using UnityEngine;
using UnityEngine.Events;

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

        public bool IsIntroPlaying { get; private set; }

        private Coroutine sirenRoutine;

        private void Awake()
        {
            if (timer == null)
                timer = FindFirstObjectByType<Timer_Patrones>();
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
                yield return new WaitForSeconds(interval);
                if (sirenAudio != null)
                    sirenAudio.Play();
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
    }
}
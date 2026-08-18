using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace PatternPuzzle
{
    /// <summary>
    /// Temporizador de cuenta regresiva para el Nivel_Patrones.
    /// Mismo comportamiento que Timer / Timer_2 (cuenta atras en Watch,
    /// color de fondo segun el tiempo restante, alertas de audio en 75/50/25%)
    /// pero sin depender de una clase de progreso fija: notifica por UnityEvent
    /// cuando el tiempo se agota. El temporizador sigue descontando al completar
    /// cada reto; solo se detiene cuando se llama a StopTimer() (p. ej. al
    /// completar el ultimo reto) o cuando el tiempo llega a cero.
    /// </summary>
    public class Timer_Patrones : MonoBehaviour
    {
        [Header("Duracion")]
        [SerializeField] private int min;
        [SerializeField] private int seg;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI tiempo;
        [SerializeField] private Image background;

        [Header("Audio")]
        [SerializeField] private AudioSource alertSource;

        [Header("Eventos")]
        [Tooltip("Se invoca cuando el tiempo se agota.")]
        public UnityEvent onTimeExpired;

        private float Max_Time;
        private float remaining;
        private bool Under_Way;

        private float threshold75;
        private float threshold50;
        private float threshold25;
        private bool alert75Played;
        private bool alert50Played;
        private bool alert25Played;

        public void Start_Timer()
        {
            if (tiempo == null) tiempo = GetComponentInChildren<TextMeshProUGUI>();
            if (background == null) background = GetComponent<Image>();

            Max_Time = min * 60 + seg;
            if (Max_Time <= 0f) Max_Time = 1f;

            remaining = Max_Time;
            Under_Way = true;
            threshold75 = Max_Time * 0.75f;
            threshold50 = Max_Time * 0.50f;
            threshold25 = Max_Time * 0.25f;
            alert75Played = false;
            alert50Played = false;
            alert25Played = false;

            Update_CD_And_Color();
            Debug.Log($"[Timer_Patrones] Iniciado: {Max_Time}s | alertas en {threshold75:F1}s, {threshold50:F1}s, {threshold25:F1}s");
        }

        private void Update()
        {
            if (!Under_Way) return;

            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                remaining = 0f;
                Ender_Time();
                return;
            }

            Update_CD_And_Color();
            CheckAlerts();
        }

        private void Update_CD_And_Color()
        {
            int minutes = Mathf.FloorToInt(remaining / 60);
            int seconds = Mathf.FloorToInt(remaining % 60);
            if (tiempo != null)
                tiempo.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            UpdateColor();
        }

        private void CheckAlerts()
        {
            if (alertSource == null) return;
            if (!alertSource.gameObject.activeInHierarchy) return;

            if (!alert75Played && remaining <= threshold75) { alertSource.Play(); alert75Played = true; }
            if (!alert50Played && remaining <= threshold50) { alertSource.Play(); alert50Played = true; }
            if (!alert25Played && remaining <= threshold25) { alertSource.Play(); alert25Played = true; }
        }

        private void UpdateColor()
        {
            if (background == null) return;

            float percentage = remaining / Max_Time;
            if (percentage > 0.5f)
            {
                float t = (1f - percentage) * 2f;
                background.color = Color.Lerp(Color.green, Color.yellow, t);
            }
            else
            {
                float t = (0.5f - percentage) * 2f;
                background.color = Color.Lerp(Color.yellow, Color.red, t);
            }
        }

        private void Ender_Time()
        {
            Under_Way = false;
            if (tiempo != null) tiempo.text = "00:00";
            if (background != null) background.color = Color.red;
            Debug.Log("[Timer_Patrones] Tiempo agotado.");
            onTimeExpired?.Invoke();
        }

        public void StopTimer()
        {
            Under_Way = false;
            alert75Played = true;
            alert50Played = true;
            alert25Played = true;
        }
    }
}
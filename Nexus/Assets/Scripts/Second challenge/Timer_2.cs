using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class Timer_2 : MonoBehaviour
{
    [SerializeField] int min, seg;
    [SerializeField] TextMeshProUGUI  tiempo;
    [SerializeField] private Image background;
    [SerializeField] private Challenge_Progress progreso;
    private float Max_Time;
    
    private float remaining;
    private bool Under_Way;

    [Header("Timer Alerts")]
    [SerializeField] private AudioSource alertSource;
    private float threshold75;
    private float threshold50;
    private float threshold25;
    private bool alert75Played;
    private bool alert50Played;
    private bool alert25Played;

    private void OnEnable()
    {
    }
    public void Start_Timer()
    {
        Max_Time = min * 60 + seg;
        remaining = Max_Time;
        Under_Way = true;
        threshold75 = Max_Time * 0.75f;
        threshold50 = Max_Time * 0.50f;
        threshold25 = Max_Time * 0.25f;
        alert75Played = false;
        alert50Played = false;
        alert25Played = false;
    }
    void Update()
    {
        if (Under_Way)
        {
            remaining -= Time.deltaTime;
            UpdateColor();
            int minutes = Mathf.FloorToInt(remaining / 60);
            int seconds = Mathf.FloorToInt(remaining % 60);
            tiempo.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            CheckAlerts();

            if (remaining <= 0.00f)
            {
                Ender_Time();
            }
        }
    }
    private void CheckAlerts()
    {
        if (alertSource == null) return;

        if (!alert75Played && remaining <= threshold75)
        {
            alertSource.Play();
            alert75Played = true;
        }
        if (!alert50Played && remaining <= threshold50)
        {
            alertSource.Play();
            alert50Played = true;
        }
        if (!alert25Played && remaining <= threshold25)
        {
            alertSource.Play();
            alert25Played = true;
        }
    }
    private void Ender_Time() { Under_Way = false; tiempo.text = "00:00"; progreso.TimeExpired(); }

    private void UpdateColor()
    {
        float percentage = remaining / Max_Time;

        if (percentage > 0.5f)
        {
            float t = (1f - percentage) * 2f;

            background.color =
                Color.Lerp(Color.green, Color.yellow, t);
        }
        else
        {
            float t = (0.5f - percentage) * 2f;

            background.color =
                Color.Lerp(Color.yellow, Color.red, t);
        }
    }

    public void StopTimer()
    {
        Under_Way = false;
        alert75Played = true;
        alert50Played = true;
        alert25Played = true;
    }
}

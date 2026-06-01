using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;


public class Timer : MonoBehaviour
{
    [SerializeField] int min, seg;
    [SerializeField] TextMeshProUGUI  tiempo;
    [SerializeField] private Image background;
    private float Max_Time;
    
    private float remaining;
    private bool Under_Way;

    private void OnEnable()
    {
        Max_Time = min * 60 + seg;
        remaining = Max_Time;
        Under_Way = true;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   

    // Update is called once per frame
    void Update()
    {
        if (Under_Way)
        {
            remaining -= Time.deltaTime;
            UpdateColor();
            int minutes = Mathf.FloorToInt(remaining / 60);
            int seconds = Mathf.FloorToInt(remaining % 60);
            tiempo.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (remaining <= 0)
            {
                Ender_Time();
            }
        }
    }
    private void Ender_Time()
    {
        Under_Way = false;
        tiempo.text = "00:00";
    }

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
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;


public class Timer : MonoBehaviour
{
    [SerializeField] int min, seg;
    [SerializeField] TextMeshProUGUI  tiempo;
    
    private float remaining;
    private bool Under_Way;

    private void OnEnable()
    {
        remaining = min * 60 + seg;
        Under_Way = true;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   

    // Update is called once per frame
    void Update()
    {
        if (Under_Way)
        {
            remaining -= Time.deltaTime;
            int minutes = Mathf.FloorToInt(remaining / 60);
            int seconds = Mathf.FloorToInt(remaining % 60);
            tiempo.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (remaining <= 0)
            {
                Under_Way = false;
                tiempo.text = "00:00";
                // Aquí puedes agregar cualquier acción que quieras realizar cuando el tiempo se agote
            }
        }
    }
}

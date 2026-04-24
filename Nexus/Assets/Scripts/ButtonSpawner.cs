using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class BotonData
{
    public string texto;
    public float ponderacion;
}

public class ButtonSpawner : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform panel;

    public List<BotonData> botones = new List<BotonData>();

    void Start()
    {
        for (int i = 0; i < botones.Count; i++)
        {
            Debug.Log("Creando botón " + i + " | Texto: " + botones[i].texto + " | Ponderación: " + botones[i].ponderacion);
            GameObject btn = Instantiate(buttonPrefab, panel);

            // Asignar texto
            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = botones[i].texto;
            }

            // Capturar datos del botón
            int index = i;
            float ponderacion = botones[i].ponderacion;

            btn.GetComponent<Button>().onClick.AddListener(() => ClickBoton(index, ponderacion));
        }
    }

    void ClickBoton(int i, float ponderacion)
    {
        Debug.Log("Click en botón " + i + " | Ponderación: " + ponderacion);
    }
}

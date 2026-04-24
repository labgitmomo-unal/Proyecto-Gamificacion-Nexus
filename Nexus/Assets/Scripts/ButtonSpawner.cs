using UnityEngine;
using UnityEngine.UI;

public class ButtonSpawner : MonoBehaviour
{
    public GameObject buttonPrefab; // tu prefab
    public Transform panel; // el panel donde van

    public int cantidad = 5;

    void Start()
    {
        for (int i = 0; i < cantidad; i++)
        {
            GameObject btn = Instantiate(buttonPrefab, panel);

            // Cambiar texto del botón
            Text txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = "Botón " + (i + 1);
            }

            // Añadir evento
            int index = i;
            btn.GetComponent<Button>().onClick.AddListener(() => ClickBoton(index));
        }
    }

    void ClickBoton(int i)
    {
        Debug.Log("Click en botón " + i);
    }
}
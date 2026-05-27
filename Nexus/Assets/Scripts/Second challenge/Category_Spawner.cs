using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Category_Spawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject categoryPrefab;

    [Header("Panel")]
    public Transform panel;

    void Start()
    {
        SpawnCategories();
    }

    private void SpawnCategories()
    {
        List<string> categorias =
            Read_Json.GetUniqueCategories();

        foreach (string categoria in categorias)
        {
            GameObject instancia =
                Instantiate(categoryPrefab, panel);

            instancia.transform.localScale =
                Vector3.one;

            Instantiate_Categories slot =
                instancia.GetComponent<Instantiate_Categories>();

            slot.Setup(categoria);
        }

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            panel.GetComponent<RectTransform>());
    }
}

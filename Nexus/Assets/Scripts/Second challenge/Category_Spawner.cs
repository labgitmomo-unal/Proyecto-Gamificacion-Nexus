using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Category_Spawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject categoryPrefab;

    [Header("Panel")]
    public Transform panel;

    private void OnEnable()
    {
        DriveDataLoader.OnDataLoaded += HandleDataLoaded;

        // Si ya existe data lista
        if (DriveDataLoader.DataReady ||
            DriveDataLoader.HasLocalData())
        {
            SpawnCategories();
        }
    }

    private void OnDisable()
    {
        DriveDataLoader.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded()
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

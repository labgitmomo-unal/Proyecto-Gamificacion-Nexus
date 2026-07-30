using System;
using UnityEngine;
using UnityEngine.UI;
public class Category_Challenger_Manager : MonoBehaviour
{
    [Header("Prefab y panel")]
    public GameObject categoryButtonPrefab;
    public Transform panel;

    private Category_Item_Button currentSelected;

    [SerializeField]
    private Challenge_Progress challenge_Progress;

    private bool audioPlayed = false;

    


    
    void OnEnable()
    {
        DriveDataLoader.OnDataLoaded += HandleDataLoaded;

        if (DriveDataLoader.DataReady || DriveDataLoader.HasLocalData())
        {
            LoadButtons();
        }


    }

    void OnDisable()
    {
        DriveDataLoader.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded()
    {
        LoadButtons();
    }

    private void LoadButtons()
    {
        string json = DriveDataLoader.ReadLocalJson();

        if (json == null)
        {
            Debug.LogError("No se pudo leer JSON.");
            return;
        }

        BotonDataList lista;

        try
        {
            lista = JsonUtility.FromJson<BotonDataList>(json);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return;
        }

        if (lista == null || lista.botones == null)
            return;

        ClearButtons();

        // Fisher-Yates shuffle para que los items aparezcan en orden aleatorio
        for (int i = lista.botones.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = lista.botones[i];
            lista.botones[i] = lista.botones[j];
            lista.botones[j] = temp;
        }

        foreach (var boton in lista.botones)
        {
            if (string.IsNullOrEmpty(boton.categoria))
                continue;

            if (!Mathf.Approximately(boton.ponderacion, 0f))
                continue;

            SpawnButton(boton);
        }

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            panel.GetComponent<RectTransform>());
        
        challenge_Progress.Initialize();
        challenge_Progress.StartChallenge();
    }

    private void SpawnButton(BotonData data)
    {
        GameObject instancia =
            Instantiate(categoryButtonPrefab, panel);
        instancia.transform.localScale = Vector3.one;
        Category_Item_Button item =
            instancia.GetComponent<Category_Item_Button>();

        item.Setup(data, this);
    }

    private void ClearButtons()
    {
        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            Destroy(panel.GetChild(i).gameObject);
        }
    }

    public void SelectItem(Category_Item_Button item)
    {
        if (currentSelected != null)
            currentSelected.SetSelected(false);

        currentSelected = item;

        currentSelected.SetSelected(true);

        Debug.Log("Seleccionado: " + item.texto);
    }

    public void ItemRemoved() { challenge_Progress.ItemSolved(); }

   
}

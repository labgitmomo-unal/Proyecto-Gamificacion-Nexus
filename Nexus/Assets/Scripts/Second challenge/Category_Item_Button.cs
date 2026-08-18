using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Category_Item_Button : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI textoUI;
    public Button button;
    public Image background;

    [Header("Data")]
    public string texto;
    public string categoria;

    private Category_Challenger_Manager manager;

    public void Setup(
        BotonData data,
        Category_Challenger_Manager challengeManager)
    {
        texto = data.texto;
        categoria = data.categoria;

        manager = challengeManager;

        textoUI.text = texto;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnSelected);
    }

    private void OnSelected()
    {
        Relation_Manager.Instance.SelectItem(this);
    }

    public void SetSelected(bool value)
    {
        if (background == null) return;

        background.color = value
            ? Color.yellow
            : Color.white;
    }

    public void SetCorrect()
    {
        background.color = Color.green;
    }

    public void SetIncorrect()
    {
        background.color = Color.red;
    }
}

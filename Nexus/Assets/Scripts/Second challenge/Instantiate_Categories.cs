using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Instantiate_Categories : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI categoryText;
    public Image background;
    public Button button;

    [Header("Data")]
    public string categoryName;

    void Start()
    {
        button.onClick.AddListener(SelectCategory);
    }
    public void Setup(string category)
    {
        categoryName = category;

        categoryText.text = category;
    }

    private void SelectCategory()
    {
        Relation_Manager.Instance.TryMatch(this);
    }

    public void SetCorrect()
    {
        background.color = Color.green;
    }

    public void SetIncorrect()
    {
        background.color = Color.red;
    }

    public void ResetColor()
    {
        background.color = Color.white;
    }
}   

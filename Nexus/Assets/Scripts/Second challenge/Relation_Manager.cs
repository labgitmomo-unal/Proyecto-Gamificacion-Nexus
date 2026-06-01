using System.Collections;
using UnityEngine;

public class Relation_Manager : MonoBehaviour
{
    [SerializeField]
    private Challenge_Progress progress;
    public static Relation_Manager Instance;

    private Category_Item_Button selectedItem;

    private void Awake()
    {
        Instance = this;
    }

    // =========================
    // ITEM SELECCIONADO
    // =========================
    public void SelectItem(Category_Item_Button item)
    {
        if (selectedItem != null)
        {
            selectedItem.SetSelected(false);
        }

        selectedItem = item;

        selectedItem.SetSelected(true);
    }

    // =========================
    // VALIDAR CATEGORIA
    // =========================
    public void TryMatch(
        Instantiate_Categories category)
    {
        if (selectedItem == null)
            return;

        bool correct =
            selectedItem.categoria ==
            category.categoryName;

        if (correct)
        {
            CorrectMatch(category);
        }
        else
        {
            StartCoroutine(
                IncorrectMatch(category)
            );
        }
    }

    // =========================
    // CORRECTO
    // =========================
    private void CorrectMatch(
        Instantiate_Categories category)
    {
        selectedItem.SetCorrect();

        category.SetCorrect();

        progress.ItemSolved();

        Destroy(selectedItem.gameObject, 0.5f);

        selectedItem = null;
    }

    // =========================
    // INCORRECTO
    // =========================
    private IEnumerator IncorrectMatch(
        Instantiate_Categories category)
    {
        selectedItem.SetIncorrect();

        category.SetIncorrect();

        yield return new WaitForSeconds(0.5f);

        selectedItem.SetSelected(false);

        category.ResetColor();

        selectedItem = null;
    }
}

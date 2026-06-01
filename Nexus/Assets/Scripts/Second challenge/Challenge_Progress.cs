using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class Challenge_Progress : MonoBehaviour
{
    [SerializeField] private Timer_2 timer;

    [SerializeField] private Transform itemsContent;

    [SerializeField] private ScrollRect itemsScrollView;

    [SerializeField] private GameObject nextPanel;

    private int remainingItems;
    private int failedItems;

    public void Initialize()
    {
        remainingItems = itemsContent.childCount;
    }

    public void ItemSolved()
    {
        remainingItems--;

        if (remainingItems < 0)
            remainingItems = 0;

        if (remainingItems == 0)
        {
            CompleteChallenge();
        }
    }

    private void CompleteChallenge()
    {
        timer.StopTimer();
    }

    public void TimeExpired()
    {
        failedItems = remainingItems;

        Debug.Log(
            $"Quedaron {failedItems} items sin clasificar"
        );

        BlockItemsPanel();
    }

    private void BlockItemsPanel()
    {
        if (itemsScrollView == null)
            return;

        itemsScrollView.velocity = Vector2.zero;
        itemsScrollView.enabled = false;

        foreach (var graphic in
            itemsScrollView.GetComponentsInChildren<Graphic>())
        {
            graphic.color = Color.gray;
        }

        for (int i = itemsContent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemsContent.GetChild(i).gameObject);
        }

        Transform viewport = itemsScrollView.viewport;

        if (viewport != null)
        {
            GameObject panel = new GameObject("TiempoAgotado");
            panel.transform.SetParent(viewport, false);

            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

            GameObject textObj = new GameObject("Texto");
            textObj.transform.SetParent(panel.transform, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Tiempo\nAgotado";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10;
            tmp.fontSizeMax = 300;
        }

        if (nextPanel != null)
        {
            nextPanel.SetActive(true);
        }
    }
}

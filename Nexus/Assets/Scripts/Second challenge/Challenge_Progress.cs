using UnityEngine.UI;
using TMPro;
using UnityEngine;
using System.Collections;

public class Challenge_Progress : MonoBehaviour
{
    [SerializeField] private Timer_2 timer;

    [SerializeField] private Transform itemsContent;

    [SerializeField] private ScrollRect itemsScrollView;

    [SerializeField] private GameObject nextPanel;

    [Header("Nono")]
    public Transform nonoDestination;

    [Header("Elevator Sequence")]
    [SerializeField] private Transform elevatorRef;
    [SerializeField] private Transform boardingPointRef;
    [SerializeField] private Transform elevatorTopPointRef;
    [SerializeField] private Transform finalDestinationRef;

    [Header("Audios")]
    public AudioSource Explain_Challenge_2;
    [SerializeField] private AudioSource Challenge_Complete_Sound;
    public AudioSource Indicator_Challenge_3;

    private const string TimeoutMessage = "Tiempo\nAgotado";

    private bool audioPlayed = false;

    private int remainingItems;
    private int failedItems;

    
    public void Initialize()
    {
        remainingItems = itemsContent.childCount;
    }

    public void StartChallenge()
    {
        if (audioPlayed) return;
        audioPlayed = true;

        if (itemsScrollView != null)
            itemsScrollView.enabled = false;

        StartCoroutine(Play_Intro_And_Start());
    }

    private IEnumerator Play_Intro_And_Start()
    {
        if (Explain_Challenge_2 != null)
        {
            Explain_Challenge_2.Play();
            yield return new WaitWhile(() => Explain_Challenge_2.isPlaying);
        }

        if (itemsScrollView != null)
            itemsScrollView.enabled = true;

        if (timer != null)
            timer.Start_Timer();
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
        BlockItemsPanel("Completado");
    }

    public void TimeExpired()
    {
        failedItems = remainingItems;

        Debug.Log(
            $"Quedaron {failedItems} items sin clasificar"
        );

        BlockItemsPanel(TimeoutMessage, false);
        TryStartElevatorSequence();
    }

    private void BlockItemsPanel(string mensaje, bool waitForIndicator = true)
    {
        if (Challenge_Complete_Sound != null)
            Challenge_Complete_Sound.Play();
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
            var msgGO = new GameObject("MsgFaseCompletada");
            msgGO.transform.SetParent(viewport, false);
            var msgRT = msgGO.AddComponent<RectTransform>();
            msgRT.anchorMin = Vector2.zero; msgRT.anchorMax = Vector2.one;
            msgRT.offsetMin = msgRT.offsetMax = Vector2.zero;
            msgGO.AddComponent<Image>().color = new Color(0f, 0.03f, 0.08f, 0.95f);

            var txtGO = new GameObject("Texto");
            txtGO.transform.SetParent(msgGO.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = new Vector2(0.05f, 0.05f); txtRT.anchorMax = new Vector2(0.95f, 0.95f);
            txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
            var tmp = txtGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = mensaje;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10; tmp.fontSizeMax = 300;
            tmp.color = new Color(1f, 1f, 1f, 1f);
            tmp.fontStyle = TMPro.FontStyles.Bold;
        }

        if (nextPanel != null)
        {
            nextPanel.SetActive(true);
            if (Indicator_Challenge_3 != null)
                Indicator_Challenge_3.Play();
        }

        if (waitForIndicator)
            StartCoroutine(EsperarAudioYVolar());
    }

    private void MoveNonoToBoardingPointAfterTimeout()
    {
        if (Nono_Guide.Instance == null)
        {
            Debug.LogWarning("[Challenge_Progress] Nono_Guide.Instance es null al terminar el tiempo.");
            return;
        }

        if (boardingPointRef == null)
        {
            Debug.LogWarning("[Challenge_Progress] boardingPointRef es null al terminar el tiempo.");
            return;
        }

        if (Nono_Guide.Instance.IsElevatorSequenceActive)
        {
            Debug.LogWarning("[Challenge_Progress] Nono ya tiene una secuencia de ascensor activa.");
            return;
        }

        Transform destination = nonoDestination != null ? nonoDestination : boardingPointRef;
        if (destination == null)
        {
            Debug.LogWarning("[Challenge_Progress] No hay destino configurado para Nono al terminar el tiempo.");
            return;
        }

        Nono_Guide.Instance.DisableAutoListen();
        Nono_Guide.Instance.FlyTo(destination);
        Debug.Log($"[Challenge_Progress] Tiempo agotado: Nono volando directamente a {destination.name}.");
    }

    private void TryStartElevatorSequence()
    {
        if (Nono_Guide.Instance == null)
        {
            Debug.LogWarning("[Challenge_Progress] Nono_Guide.Instance is null, cannot start elevator sequence.");
            return;
        }

        if (Nono_Guide.Instance.IsElevatorSequenceActive)
        {
            Debug.LogWarning("[Challenge_Progress] Elevator sequence already active.");
            return;
        }

        if (elevatorRef == null || boardingPointRef == null || elevatorTopPointRef == null || finalDestinationRef == null)
        {
            Debug.LogWarning("[Challenge_Progress] Cannot start elevator sequence: one or more references are null.");
            return;
        }

        Debug.Log("[Challenge_Progress] Iniciando secuencia de Nono hacia ReferenciaMomoAscensorP1.");
        Nono_Guide.Instance.StartElevatorSequence(elevatorRef, boardingPointRef, elevatorTopPointRef, finalDestinationRef);
    }

    private IEnumerator EsperarAudioYVolar()
    {
        if (Indicator_Challenge_3 != null)
            yield return new WaitWhile(() => Indicator_Challenge_3.isPlaying);

        if (Nono_Guide.Instance != null && nonoDestination != null)
            // Nono_Guide.Instance.FlyTo(nonoDestination);
            TryStartElevatorSequence();
    }
}

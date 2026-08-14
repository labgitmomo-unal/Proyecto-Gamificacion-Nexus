using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PanelCronometro : MonoBehaviour
{
    private const float ExampleStartDelaySeconds = 2f;
    private const string StartLabel = "Iniciar";
    private const string StopLabel = "Detener";
    private const string InitialTimeLabel = "00:00";

    [SerializeField] private GraphExampleSequence graphExampleSequence;
    [SerializeField] private AudioSource startAudio;
    [SerializeField] private AudioSource demonstrationAudio;
    [SerializeField] private AudioSource completionAudio;
    [SerializeField] private Nono_Guide nonoGuide;
    [SerializeField] private Transform elevatorRef;
    [SerializeField] private Transform boardingPointRef;
    [SerializeField] private Transform elevatorTopPointRef;

    private Button toggleButton;
    private Button resetButton;
    private RectTransform toggleButtonRect;
    private RectTransform resetButtonRect;
    private TMP_Text toggleButtonText;
    private TMP_Text timerText;
    private TMP_Text scoreText;
    private Coroutine exampleStartCoroutine;
    private bool isRunning;
    private float elapsedSeconds;
    private int score;

    private void Awake()
    {
        toggleButton = transform.Find("BotonControl")?.GetComponent<Button>()
            ?? GetComponentInChildren<Button>(true);
        resetButton = transform.Find("BotonReiniciar")?.GetComponent<Button>();
        toggleButtonRect = toggleButton != null ? toggleButton.GetComponent<RectTransform>() : null;
        resetButtonRect = resetButton != null ? resetButton.GetComponent<RectTransform>() : null;
        timerText = FindText("Cronometro");
        scoreText = FindText("Puntaje");
        toggleButtonText = toggleButton != null ? toggleButton.GetComponentInChildren<TMP_Text>(true) : null;

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(HandleButtonPressed);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(HandleResetPressed);
        }

        elapsedSeconds = 0f;
        isRunning = false;
        UpdateDisplay();
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        elapsedSeconds += Time.deltaTime;
        UpdateDisplay();
    }

    /// <summary>Starts or pauses the timer and controls the configured graph example sequence.</summary>
    public void ToggleTimer()
    {
        HandleButtonPressed();
    }

    private void HandleButtonPressed()
    {
        isRunning = !isRunning;

        if (isRunning)
        {
            PlayAudio(startAudio);
            exampleStartCoroutine = StartCoroutine(StartExampleAfterDelay());
        }
        else
        {
            CancelPendingExample();
            startAudio?.Stop();
            demonstrationAudio?.Stop();
            PlayAudio(completionAudio);
            StartNonoReturnSequence();
        }

        UpdateDisplay();
    }

    private void StartNonoReturnSequence()
    {
        Nono_Guide guide = nonoGuide != null ? nonoGuide : Nono_Guide.Instance;
        guide?.StartReturnElevatorSequence(elevatorRef, boardingPointRef, elevatorTopPointRef);
    }

    private IEnumerator StartExampleAfterDelay()
    {
        if (startAudio != null)
        {
            yield return new WaitWhile(() => startAudio.isPlaying);
        }

        yield return new WaitForSeconds(ExampleStartDelaySeconds);

        if (!isRunning)
        {
            yield break;
        }

        PlayAudio(demonstrationAudio);
        graphExampleSequence?.StartSequence();
        exampleStartCoroutine = null;
    }

    /// <summary>Restores playable graph nodes and removes player-created edges without changing the stop flow.</summary>
    public void ResetGraph()
    {
        var nodes = FindObjectsByType<GraphNode3D>(FindObjectsSortMode.None);
        var sockets = new List<GraphSocket3D>(FindObjectsByType<GraphSocket3D>(FindObjectsSortMode.None));

        foreach (var socket in sockets)
        {
            if (socket == null || (graphExampleSequence != null && graphExampleSequence.IsExampleNode(socket.OriginalOwnerNode)))
            {
                continue;
            }

            socket.ResetToOriginalState();
        }

        foreach (var node in nodes)
        {
            if (node == null || (graphExampleSequence != null && graphExampleSequence.IsExampleNode(node)))
            {
                continue;
            }

            node.ResetToInitialPose();
        }

        var edges = FindObjectsByType<GraphEdge>(FindObjectsSortMode.None);
        foreach (var edge in edges)
        {
            if (edge == null)
            {
                continue;
            }

            bool isExampleEdge = graphExampleSequence != null && graphExampleSequence.IsExampleEdge(edge);
            if (!isExampleEdge)
            {
                Destroy(edge.gameObject);
            }
        }

        CancelPendingExample();
        startAudio?.Stop();
        demonstrationAudio?.Stop();
        isRunning = false;
        elapsedSeconds = 0f;
        score = 0;
        UpdateDisplay();
    }

    private void HandleResetPressed()
    {
        ResetGraph();
    }


    private void CancelPendingExample()
    {
        if (exampleStartCoroutine == null)
        {
            return;
        }

        StopCoroutine(exampleStartCoroutine);
        exampleStartCoroutine = null;
    }

    private static void PlayAudio(AudioSource audioSource)
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    private TMP_Text FindText(string objectName)
    {
        Transform textTransform = transform.Find(objectName);
        return textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
    }

    private void UpdateDisplay()
    {
        if (timerText != null)
        {
            int totalSeconds = Mathf.FloorToInt(elapsedSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.text = totalSeconds == 0 ? InitialTimeLabel : $"{minutes:00}:{seconds:00}";
        }

        if (scoreText != null)
        {
            scoreText.text = $"Puntaje: {score}";
        }

        if (toggleButtonText != null)
        {
            toggleButtonText.text = isRunning ? StopLabel : StartLabel;
        }

        UpdateButtonLayout();
    }

    private void UpdateButtonLayout()
    {
        if (toggleButtonRect != null)
        {
            toggleButtonRect.anchoredPosition = isRunning
                ? new Vector2(-170f, -250f)
                : new Vector2(0f, -250f);
            toggleButtonRect.sizeDelta = isRunning
                ? new Vector2(300f, 140f)
                : new Vector2(500f, 140f);
        }

        if (resetButtonRect != null)
        {
            resetButtonRect.anchoredPosition = new Vector2(170f, -250f);
            resetButtonRect.sizeDelta = new Vector2(300f, 140f);
        }

        if (resetButton != null)
        {
            resetButton.gameObject.SetActive(isRunning);
        }
    }
}

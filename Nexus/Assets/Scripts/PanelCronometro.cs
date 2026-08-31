using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    [SerializeField] private GraphPlacementScoreManager graphPlacementScoreManager;
    [SerializeField] private AudioSource startAudio;
    [SerializeField] private AudioSource demonstrationAudio;
    [SerializeField] private AudioSource completionAudio;
    [SerializeField] private Nono_Guide nonoGuide;
    [SerializeField] private Transform elevatorRef;
    [SerializeField] private Transform boardingPointRef;
    [SerializeField] private Transform elevatorTopPointRef;

    private Button toggleButton;
    private Button resetButton;
    private Button finalizeButton;
    private RectTransform toggleButtonRect;
    private RectTransform resetButtonRect;
    private RectTransform finalizeButtonRect;
    private TMP_Text toggleButtonText;
    private TMP_Text timerText;
    private TMP_Text scoreText;
    private Coroutine exampleStartCoroutine;
    private bool sessionActive;
    private bool isRunning;
    private bool showStopOptions;
    private float elapsedSeconds;
    private float score;

    private void Awake()
    {
        toggleButton = transform.Find("BotonControl")?.GetComponent<Button>()
            ?? GetComponentInChildren<Button>(true);
        resetButton = transform.Find("BotonReiniciar")?.GetComponent<Button>();
        finalizeButton = transform.Find("BotonFinalizar")?.GetComponent<Button>();
        toggleButtonRect = toggleButton != null ? toggleButton.GetComponent<RectTransform>() : null;
        resetButtonRect = resetButton != null ? resetButton.GetComponent<RectTransform>() : null;
        finalizeButtonRect = finalizeButton != null ? finalizeButton.GetComponent<RectTransform>() : null;
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

        if (finalizeButton != null)
        {
            finalizeButton.onClick.AddListener(HandleFinalizePressed);
        }

        if (graphExampleSequence != null)
        {
            graphExampleSequence.SequenceCompleted += HandleExampleCompleted;
        }

        if (graphPlacementScoreManager != null)
        {
            graphPlacementScoreManager.ScoreChanged += HandleScoreChanged;
        }
        else
        {
            Debug.LogWarning("PanelCronometro no tiene GraphPlacementScoreManager asignado; se usará Puntaje: 0/0.", this);
        }

        elapsedSeconds = 0f;
        sessionActive = false;
        isRunning = false;
        showStopOptions = false;
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        if (graphExampleSequence != null)
        {
            graphExampleSequence.SequenceCompleted -= HandleExampleCompleted;
        }

        if (graphPlacementScoreManager != null)
        {
            graphPlacementScoreManager.ScoreChanged -= HandleScoreChanged;
        }
    }

    private void Start()
    {
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

    /// <summary>Starts the demonstration or opens the stop options for the current session.</summary>
    public void ToggleTimer()
    {
        HandleButtonPressed();
    }

    private void HandleButtonPressed()
    {
        if (sessionActive)
        {
            StopSession();
            return;
        }

        StartSession();
    }

    private void StartSession()
    {
        sessionActive = true;
        isRunning = false;
        showStopOptions = false;
        PlayAudio(startAudio);
        exampleStartCoroutine = StartCoroutine(StartExampleAfterDelay());
        UpdateDisplay();
    }

    private void StopSession()
    {
        sessionActive = false;
        isRunning = false;
        showStopOptions = true;
        graphPlacementScoreManager?.EndEvaluation();
        CancelPendingExample();
        startAudio?.Stop();
        demonstrationAudio?.Stop();
        PlayAudio(completionAudio);
        UpdateDisplay();
    }

    private void HandleExampleCompleted()
    {
        if (!sessionActive)
        {
            return;
        }

        isRunning = true;
        graphPlacementScoreManager?.BeginEvaluation();
        UpdateDisplay();
    }

    private void HandleScoreChanged(float currentScore, float maximumScore)
    {
        score = currentScore;
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

        if (!sessionActive)
        {
            yield break;
        }

        PlayAudio(demonstrationAudio);
        graphExampleSequence?.StartSequence();
        exampleStartCoroutine = null;
    }

    /// <summary>Restores playable graph nodes and removes player-created edges.</summary>
    public void ResetGraph()
    {
        graphExampleSequence?.CancelSequence();
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
        sessionActive = false;
        isRunning = false;
        showStopOptions = false;
        elapsedSeconds = 0f;
        graphPlacementScoreManager?.ResetEvaluation();
        UpdateDisplay();
    }

    private void HandleResetPressed()
    {
        ResetGraph();
    }

    private void HandleFinalizePressed()
    {
        sessionActive = false;
        isRunning = false;
        showStopOptions = false;
        graphPlacementScoreManager?.EndEvaluation();
        UpdateDisplay();
        StartNonoReturnSequence();
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
            if (graphPlacementScoreManager != null)
            {
                scoreText.text = $"Puntaje: {FormatScore(graphPlacementScoreManager.CurrentScore)}/{FormatScore(graphPlacementScoreManager.MaximumScore)}";
            }
            else
            {
                scoreText.text = "Puntaje: 0/0";
            }
        }

        if (toggleButtonText != null)
        {
            toggleButtonText.text = sessionActive ? StopLabel : StartLabel;
        }

        UpdateButtonLayout();
    }

    private static string FormatScore(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }


    private void UpdateButtonLayout()
    {
        bool showSessionControls = sessionActive;
        bool showDecisionControls = showStopOptions;

        if (toggleButton != null)
        {
            toggleButton.gameObject.SetActive(!showDecisionControls);
        }

        if (toggleButtonRect != null)
        {
            toggleButtonRect.anchoredPosition = showSessionControls
                ? new Vector2(-170f, -250f)
                : new Vector2(0f, -250f);
            toggleButtonRect.sizeDelta = showSessionControls
                ? new Vector2(300f, 140f)
                : new Vector2(500f, 140f);
        }

        if (resetButton != null)
        {
            resetButton.gameObject.SetActive(showSessionControls || showDecisionControls);
        }

        if (resetButtonRect != null)
        {
            resetButtonRect.anchoredPosition = showDecisionControls
                ? new Vector2(-170f, -250f)
                : new Vector2(170f, -250f);
            resetButtonRect.sizeDelta = new Vector2(300f, 140f);
        }

        if (finalizeButton != null)
        {
            finalizeButton.gameObject.SetActive(showDecisionControls);
        }

        if (finalizeButtonRect != null)
        {
            finalizeButtonRect.anchoredPosition = new Vector2(170f, -250f);
            finalizeButtonRect.sizeDelta = new Vector2(300f, 140f);
        }
    }
}
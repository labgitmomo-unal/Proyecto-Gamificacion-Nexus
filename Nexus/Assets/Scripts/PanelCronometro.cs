using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PanelCronometro : MonoBehaviour
{
    private const string StartLabel = "Iniciar";
    private const string StopLabel = "Detener";
    private const string InitialTimeLabel = "00:00";
    private const string ScoreLabel = "Puntaje: 0";

    [SerializeField] private GraphExampleSequence graphExampleSequence;
    [SerializeField] private AudioSource startAudio;
    [SerializeField] private AudioSource completionAudio;

    private Button toggleButton;
    private TMP_Text toggleButtonText;
    private TMP_Text timerText;
    private TMP_Text scoreText;
    private bool isRunning;
    private float elapsedSeconds;

    private void Awake()
    {
        toggleButton = GetComponentInChildren<Button>(true);
        timerText = FindText("Cronometro");
        scoreText = FindText("Puntaje");
        toggleButtonText = toggleButton != null ? toggleButton.GetComponentInChildren<TMP_Text>(true) : null;

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(HandleButtonPressed);
        }

        if (graphExampleSequence != null)
        {
            graphExampleSequence.SequenceCompleted += HandleSequenceCompleted;
        }

        elapsedSeconds = 0f;
        isRunning = false;
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        if (graphExampleSequence != null)
        {
            graphExampleSequence.SequenceCompleted -= HandleSequenceCompleted;
        }
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

    /// <summary>Starts or pauses the timer and launches the graph example when starting from the panel.</summary>
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
            graphExampleSequence?.StartSequence();
        }

        UpdateDisplay();
    }

    private void HandleSequenceCompleted()
    {
        PlayAudio(completionAudio);
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
            scoreText.text = ScoreLabel;
        }

        if (toggleButtonText != null)
        {
            toggleButtonText.text = isRunning ? StopLabel : StartLabel;
        }
    }
}

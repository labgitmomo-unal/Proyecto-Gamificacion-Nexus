using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PanelCronometro : MonoBehaviour
{
    private const float ExampleStartDelaySeconds = 2f;
    private const string StartLabel = "Iniciar";
    private const string StopLabel = "Detener";
    private const string InitialTimeLabel = "00:00";
    private const string ScoreLabel = "Puntaje: 0";

    [SerializeField] private GraphExampleSequence graphExampleSequence;
    [SerializeField] private AudioSource startAudio;
    [SerializeField] private AudioSource demonstrationAudio;
    [SerializeField] private AudioSource completionAudio;
    [SerializeField] private Nono_Guide nonoGuide;
    [SerializeField] private Transform elevatorRef;
    [SerializeField] private Transform boardingPointRef;
    [SerializeField] private Transform elevatorTopPointRef;

    private Button toggleButton;
    private TMP_Text toggleButtonText;
    private TMP_Text timerText;
    private TMP_Text scoreText;
    private Coroutine exampleStartCoroutine;
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
            scoreText.text = ScoreLabel;
        }

        if (toggleButtonText != null)
        {
            toggleButtonText.text = isRunning ? StopLabel : StartLabel;
        }
    }
}

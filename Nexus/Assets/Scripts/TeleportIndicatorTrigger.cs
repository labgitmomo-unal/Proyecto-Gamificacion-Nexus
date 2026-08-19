using System.Collections;
using UnityEngine;

public class TeleportIndicatorTrigger : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject sm_TeleportIndicator;
    public CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    [Header("Canvas Visibility")]
    [SerializeField] private bool hideCanvasOutsideTrigger = true;

    private Coroutine fadeCoroutine;

    [SerializeField] private ProgresoAbstraccion progreso;

    private void Start()
    {
        if (canvasGroup == null)
            return;

        if (hideCanvasOutsideTrigger)
        {
            canvasGroup.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
            return;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.gameObject.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (sm_TeleportIndicator != null)
            sm_TeleportIndicator.SetActive(false);

        if (canvasGroup != null)
        {
            if (hideCanvasOutsideTrigger)
            {
                if (fadeCoroutine != null)
                    StopCoroutine(fadeCoroutine);

                canvasGroup.gameObject.SetActive(true);
                fadeCoroutine = StartCoroutine(Fade(0f, 1f));
            }

            progreso?.StartChallenge();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (sm_TeleportIndicator != null)
            sm_TeleportIndicator.SetActive(true);

        if (!hideCanvasOutsideTrigger || canvasGroup == null)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(Fade(1f, 0f, true));
    }

    private IEnumerator Fade(float from, float to, bool deactivateOnEnd = false)
    {
        var elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = to;

        if (deactivateOnEnd)
            canvasGroup.gameObject.SetActive(false);
    }
}

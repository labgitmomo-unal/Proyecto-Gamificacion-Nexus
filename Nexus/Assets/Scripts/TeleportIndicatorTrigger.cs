using System.Collections;
using UnityEngine;

public class TeleportIndicatorTrigger : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject sm_TeleportIndicator;   // El mesh 3D (SM_Teleport_Indicator o hijo)
    public CanvasGroup canvasGroup;            // CanvasGroup del Canvas para fade

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    private Coroutine fadeCoroutine;

void Start()
    {
        if (canvasGroup != null)
        {
            // Asegurar escala correcta para VR WorldSpace (0.001 = 1px por mm)
            canvasGroup.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Desactivar el mesh 3D
        if (sm_TeleportIndicator != null)
            sm_TeleportIndicator.SetActive(false);

        // Mostrar canvas con fade in
        if (canvasGroup != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            canvasGroup.gameObject.SetActive(true);
            fadeCoroutine = StartCoroutine(Fade(0f, 1f));
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Reactivar el mesh 3D
        if (sm_TeleportIndicator != null)
            sm_TeleportIndicator.SetActive(true);

        // Ocultar canvas con fade out
        if (canvasGroup != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(Fade(1f, 0f, deactivateOnEnd: true));
        }
    }

    IEnumerator Fade(float from, float to, bool deactivateOnEnd = false)
    {
        float elapsed = 0f;
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

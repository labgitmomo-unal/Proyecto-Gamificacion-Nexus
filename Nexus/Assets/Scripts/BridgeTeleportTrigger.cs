using UnityEngine;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine.UI;

public class BridgeTeleportTrigger : MonoBehaviour
{
    [Header("Destino")]
    public Transform teleportDestination;

    [Header("Fade")]
    public float fadeDuration = 1f;

    [Header("Activar tras teletransporte")]
    public GameObject bridgePatternManager;

    private CanvasGroup _fadeCanvasGroup;
    private XROrigin _xrOrigin;
    private bool _teleported = false;

    void Awake()
    {
        _fadeCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        if (_fadeCanvasGroup == null)
            _fadeCanvasGroup = CreateFadeOverlay();

        if (_xrOrigin == null)
            _xrOrigin = FindObjectOfType<XROrigin>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_teleported) return;
        if (!other.CompareTag("Player")) return;

        _teleported = true;
        StartCoroutine(TeleportSequence());
    }

    IEnumerator TeleportSequence()
    {
        // Ocultar el indicador visual
        if (transform.parent != null)
        {
            var indicator = transform.parent.Find("SM_Teleport_Indicator");
            if (indicator != null)
                indicator.gameObject.SetActive(false);
        }

        // Fade out
        yield return StartCoroutine(Fade(0f, 1f));

        // Teleport
        if (_xrOrigin != null && teleportDestination != null)
            _xrOrigin.transform.position = teleportDestination.position;

        yield return new WaitForSeconds(0.2f);

        // Fade in
        yield return StartCoroutine(Fade(1f, 0f));

        // Activar el pattern manager
        if (bridgePatternManager != null)
            bridgePatternManager.SetActive(true);

        // Desactivar este GameObject (ya no necesario)
        gameObject.SetActive(false);
    }

    IEnumerator Fade(float from, float to)
    {
        if (_fadeCanvasGroup == null) yield break;

        float elapsed = 0f;
        _fadeCanvasGroup.alpha = from;
        _fadeCanvasGroup.blocksRaycasts = to > 0.5f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        _fadeCanvasGroup.alpha = to;
    }

    private CanvasGroup CreateFadeOverlay()
    {
        var canvasGO = new GameObject("TeleportFadeCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.SetActive(false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform, false);
        var image = imageGO.AddComponent<Image>();
        image.color = Color.black;

        var rect = imageGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var cg = canvasGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        canvasGO.SetActive(true);
        return cg;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgresoAbstraccion : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image fillImage;
    public TextMeshProUGUI textoporcentaje;
    public ScrollRect scrollView;

    [Header("Colores de progreso")]
    public Color colorInicio    = new Color(0f, 1f, 1f, 1f);
    public Color colorFinal     = new Color(0f, 1f, 0.4f, 1f);
    public Color colorBloqueado = new Color(1f, 0.8f, 0f, 1f);

    // Color gris que se aplica al Viewport/Content cuando se bloquea
    private static readonly Color colorGrisBloqueado = new Color(0.35f, 0.35f, 0.35f, 1f);

    // Total leido automaticamente desde ButtonSpawner al parsear el JSON
    private int _totalObjetivo = 0;
    private int _eliminados    = 0;

    public static event Action OnBotonObjetivoEliminado;
    public static event Action OnFaseCompletada;
    // Flag para que paneles que arrancan tarde sepan si la fase ya se completo
    public static bool FaseCompletada { get; private set; }


	void OnEnable()
	{
		FaseCompletada = false; // Resetear al inicio de sesion
		OnBotonObjetivoEliminado += HandleBotonEliminado;

		// Resetear el PanelPonderacionCero antes de empezar la ronda
		// para evitar que aparezca "Completado" de una sesion anterior
		var panel = UnityEngine.Object.FindObjectOfType<PanelPonderacionCero>();
		if (panel != null)
			panel.Resetear();

		ActualizarUI();
	}

    void OnDisable()
    {
        OnBotonObjetivoEliminado -= HandleBotonEliminado;
    }

    // ButtonSpawner llama esto justo despues de parsear el JSON
    public static void SetTotalObjetivo(int total)
    {
        // Propagar a la instancia activa via evento no es necesario;
        // la instancia lo recibe directo porque ButtonSpawner la busca con FindObjectOfType
        // -- ver ButtonSpawner.SpawnBotones()
    }

    // Recibe el total directamente desde ButtonSpawner
    public void InicializarConTotal(int total)
    {
        _totalObjetivo = total;
        _eliminados    = 0;

        // Desbloquear scroll visualmente por si se reinicia
        DesbloquearScrollView();
        ActualizarUI();
        Debug.Log($"[ProgresoAbstraccion] Total botones objetivo leido del JSON: {_totalObjetivo}");
    }

    private void HandleBotonEliminado()
    {
        if (_totalObjetivo <= 0) return;

        _eliminados = Mathf.Min(_eliminados + 1, _totalObjetivo);
        ActualizarUI();

        if (_eliminados >= _totalObjetivo)
            BloquearScrollView();
    }

    public void Reiniciar()
    {
        _eliminados = 0;
        DesbloquearScrollView();
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        float t = _totalObjetivo > 0 ? Mathf.Clamp01((float)_eliminados / _totalObjetivo) : 0f;

        if (fillImage != null)
        {
            fillImage.fillAmount = t;
            fillImage.color = _eliminados >= _totalObjetivo && _totalObjetivo > 0
                ? colorBloqueado
                : Color.Lerp(colorInicio, colorFinal, t);
        }

        if (textoporcentaje != null)
        {
            textoporcentaje.text = _eliminados >= _totalObjetivo && _totalObjetivo > 0
                ? "COMPLETO"
                : $"{Mathf.RoundToInt(t * 100f)}%";
        }
    }

    private void BloquearScrollView()
    {
        if (scrollView == null) return;

        // 1. Aplicar gris a todos los Graphic del ScrollView (aspecto bloqueado)
        scrollView.velocity = Vector2.zero;
        foreach (var graphic in scrollView.GetComponentsInChildren<Graphic>(true))
            graphic.color = colorGrisBloqueado;

        // 2. Destruir todos los botones del Content
        Transform content = scrollView.content;
        if (content != null)
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

        // 3. Mostrar mensaje "Fase Completada" dentro del Viewport
        Transform viewport = scrollView.viewport;
        if (viewport != null)
        {
            // Contenedor del mensaje
            var msgGO = new GameObject("MsgFaseCompletada");
            msgGO.transform.SetParent(viewport, false);
            var msgRT = msgGO.AddComponent<RectTransform>();
            msgRT.anchorMin = Vector2.zero; msgRT.anchorMax = Vector2.one;
            msgRT.offsetMin = msgRT.offsetMax = Vector2.zero;
            msgGO.AddComponent<UnityEngine.UI.Image>().color = new Color(0f, 0.03f, 0.08f, 0.95f);

            // Texto en hijo separado (TMP no puede compartir GO con otros componentes UI)
            var txtGO = new GameObject("Texto");
            txtGO.transform.SetParent(msgGO.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = new Vector2(0.05f, 0.05f); txtRT.anchorMax = new Vector2(0.95f, 0.95f);
            txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
            var tmp = txtGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "Fase\nCompletada";
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10; tmp.fontSizeMax = 300;
            tmp.color = new Color(1f, 1f, 1f, 1f);
            tmp.fontStyle = TMPro.FontStyles.Bold;
        }

        // 4. Actualizar fill y texto del panel de progreso
        if (fillImage != null) fillImage.color = colorBloqueado;
        if (textoporcentaje != null) textoporcentaje.text = "COMPLETO";

        // 5. Notificar al panel de la segunda mesa
        FaseCompletada = true;
        OnFaseCompletada?.Invoke();
Debug.Log("[ProgresoAbstraccion] ScrollView bloqueado al 100%.");
    }

    private void DesbloquearScrollView()
    {
        if (scrollView == null) return;
        scrollView.enabled = true;
    }

    public static void NotificarEliminacion()
    {
        OnBotonObjetivoEliminado?.Invoke();
    }
}
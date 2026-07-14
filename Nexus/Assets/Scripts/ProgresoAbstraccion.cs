using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ProgresoAbstraccion : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Timer timer;
    public Image fillImage;
    public TextMeshProUGUI textoporcentaje;
    public ScrollRect scrollView;
    public Transform Target_2; // Referencia al Target del Panel 2 para el vuelo de Nono

    [Header("Audios")]
    public AudioSource Explain_Challenge_1;
    public AudioSource Indicator_Challenge_2;
    public AudioSource Challenge_Complete_Sound;


    [Header("Teleport al completar Panel 1")]
    public GameObject teleportIndicatorSource;
    [Tooltip("Transform del Box019 del Panel_2, usado como referencia de posición")]
    public GameObject Collider_Destino_Teleport;

    [Header("Colores de progreso")]
    public Color colorInicio    = new Color(0f, 1f, 1f, 1f);
    public Color colorFinal     = new Color(0f, 1f, 0.4f, 1f);
    public Color colorBloqueado = new Color(1f, 0.8f, 0f, 1f);

    private static readonly Color colorGrisBloqueado = new Color(0.35f, 0.35f, 0.35f, 1f);

    private int _totalObjetivo = 0;
    private int _eliminados    = 0;

    private bool Audio_Played = false;

    public static event Action OnBotonObjetivoEliminado;
    public static event Action OnFaseCompletada;
    public static bool FaseCompletada { get; private set; }

    void OnEnable()
    {
        
        FaseCompletada = false;
        OnBotonObjetivoEliminado += HandleBotonEliminado;

    }

    void OnDisable()
    {
        OnBotonObjetivoEliminado -= HandleBotonEliminado;
    }

    public void InicializarConTotal(int total)
    {
        _totalObjetivo = total;
        _eliminados    = 0;
        DesbloquearScrollView();
        ActualizarUI();
        Debug.Log($"[ProgresoAbstraccion] InicializarConTotal | total={_totalObjetivo}, fillImage={fillImage != null}, textoporcentaje={textoporcentaje != null}, scrollView={scrollView != null}");
    }

    private void HandleBotonEliminado()
    {
        Debug.Log($"[ProgresoAbstraccion] HandleBotonEliminado | _totalObjetivo={_totalObjetivo}, _eliminados={_eliminados}, fillImage={fillImage != null}, textoporcentaje={textoporcentaje != null}");
        if (_totalObjetivo <= 0) return;
        _eliminados = Mathf.Min(_eliminados + 1, _totalObjetivo);
        ActualizarUI();
        if (_eliminados >= _totalObjetivo)
            BloquearScrollView("Fase\nCompletada");
            
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

        Debug.Log($"[ProgresoAbstraccion] ActualizarUI | t={t:F2}, fillImage={fillImage != null}, textoporcentaje={textoporcentaje != null}");

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

    private void BloquearScrollView(string mensaje)
    {
        timer.StopTimer();
        if (Challenge_Complete_Sound != null)
            Challenge_Complete_Sound.Play();
        if (scrollView == null) return;

        // 1. Gris en todos los Graphic del ScrollView
        scrollView.velocity = Vector2.zero;
        foreach (var graphic in scrollView.GetComponentsInChildren<Graphic>(true))
            graphic.color = colorGrisBloqueado;

        // 2. Destruir botones del Content
        Transform content = scrollView.content;
        if (content != null)
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

        // 3. Mensaje "Fase Completada" dentro del Viewport
        Transform viewport = scrollView.viewport;
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

        // 4. Actualizar fill y texto
        if (fillImage != null) fillImage.color = colorBloqueado;
        if (textoporcentaje != null) textoporcentaje.text = "COMPLETO";

        // 5. Activar Panel 2
        if (teleportIndicatorSource != null)
        {
            teleportIndicatorSource.SetActive(true);
            if (Collider_Destino_Teleport != null)
                Collider_Destino_Teleport.SetActive(true);
        }

        // 7. Notificar
        FaseCompletada = true;
        OnFaseCompletada?.Invoke();
        Debug.Log("[ProgresoAbstraccion] ScrollView bloqueado al 100%. Panel 2 activado.");
        if (Indicator_Challenge_2 != null)
        {
            Indicator_Challenge_2.Play();
            StartCoroutine(EsperarAudioYVolar());
        }
        else if (Nono_Guide.Instance != null)
        {
            Nono_Guide.Instance.FlyTo(Target_2);
        }
    }

    private System.Collections.IEnumerator EsperarAudioYVolar()
    {
        yield return new WaitWhile(() => Indicator_Challenge_2.isPlaying);
        Nono_Guide.Instance.FlyTo(Target_2);
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

    public void TiempoAgotado()
    {
        if (FaseCompletada)
            return;

        BloquearScrollView("Tiempo\nAgotado");
    }

    private IEnumerator Play_Intro()
    {
        Explain_Challenge_1.Play();
        yield return new WaitWhile(() => Explain_Challenge_1.isPlaying);
        timer.Start_Timer();
        // Aquí podrías agregar animaciones o efectos introductorios si lo deseas
    }

    public void StartChallenge()
    {
        if (!Audio_Played)
        {
            StartCoroutine(Play_Intro());
            Audio_Played = true;
        }
    }
}

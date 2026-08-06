using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// RETO 2 - "¿Qué avenida termina en flujo BAJO?"
/// Muestra en un panel un conjunto de avenidas, cada una con su secuencia de niveles
/// de flujo vehicular (Muy alto / Alto / Medio / Bajo). El jugador debe elegir qué avenida
/// tiene el nivel "Bajo" en su secuencia. Solo una es correcta.
///
/// Mecánica:
///  - Acierto  -> dispara onChallengeCompleted()  (para que el avance del tráfico ocurra
///                igual que en el Reto 1, ver RetoTraficoLinker).
///  - Fallo    -> feedback visual y se permite reintentar.
///
/// Configuración:
///  - En el Inspector, definir las avenidas con su nombre y su secuencia de niveles.
///  - Indicar cuál es la correcta (regla: contiene o termina en el nivel objetivo).
///  - Asignar el textoProblema y un contenedor (botonesContainer) donde se generan los botones.
/// </summary>
public class Reto2_Avenidas : MonoBehaviour
{
    public enum LevelType
    {
        MuyAlto,
        Alto,
        Medio,
        Bajo
    }

    public enum AnswerRule
    {
        ContainsLevel,   // La avenida correcta es la que contiene el nivel indicado
        EndsWithLevel    // La avenida correcta es la que termina en el nivel indicado
    }

    [Serializable]
    public class AvenidaData
    {
        [Tooltip("Nombre visible de la avenida (aparece en el botón).")]
        public string nombre;

        [Tooltip("Secuencia de niveles de flujo de izquierda a derecha.")]
        public List<LevelType> secuencia = new List<LevelType>();
    }

    [Header("Enunciado (problema)")]
    [Tooltip("Texto del enunciado, admite {0} con el nivel objetivo (ej: '…termina en flujo {0}?').")]
    [SerializeField] private string preguntaPlantilla = "¿Cuál avenida termina en flujo {0}?";

    [Tooltip("Nivel objetivo de la pregunta (la correcta es la que contiene/termina en este).")]
    [SerializeField] private LevelType nivelCorreccion = LevelType.Bajo;

    [Tooltip("Cómo se considera correcta una avenida.")]
    [SerializeField] private AnswerRule reglaRespuesta = AnswerRule.ContainsLevel;

    [Header("Avenidas")]
    [SerializeField] private List<AvenidaData> avenidas = new List<AvenidaData>();

    [Header("UI")]
    [Tooltip("TextMeshPro donde se muestra el enunciado con las secuencias.")]
    [SerializeField] private TextMeshProUGUI textoProblema;

    [Tooltip("Contenedor (RectTransform) donde se instancia un botón por avenida, en orden.")]
    [SerializeField] private RectTransform botonesContainer;

    [Tooltip("Ancho en unidades determinadas para cada botón generado.")]
    [SerializeField] private Vector2 botonSize = new Vector2(600f, 120f);

    [Tooltip("Gap vertical entre botones generados.")]
    [SerializeField] private float botonSpacing = 40f;

    [Tooltip("Prefab de botón opcional. Si se asigna, se clona en vez de crear una imagen/button genérica.")]
    [SerializeField] private Button botonPrefab;

    [Tooltip("Feedback visual al fallar (opcional).")]
    [SerializeField] private GameObject feedbackFallo;

    [Header("Colores de respuesta")]
    [Tooltip("Color del botón al acertar la avenida correcta.")]
    [SerializeField] private Color _colorCorrecto = new Color(0f, 1f, 0.1f);

    [Tooltip("Color del botón al elegir una avenida incorrecta.")]
    [SerializeField] private Color _colorIncorrecto = new Color(1f, 0.05f, 0.1f);

    [Header("Tráfico")]
    [Tooltip("Opcional: al acertar también se avanza el tráfico del puente. Si se deja vacío se busca solo.")]
    [SerializeField] private BridgeControlManager bridgeControl;

    [Header("Resultado")]
    [Tooltip("Se dispara cuando el jugador acierta la avenida correcta.")]
    public UnityEvent onChallengeCompleted;

    [Tooltip("Se dispara cuando el jugador elige una avenida incorrecta.")]
    public UnityEvent onChallengeFallido;

    // Resultado expuesto para lectura (diagnóstico / HUD).
    public int AvenidaCorrectaIndex { get; private set; }
    public int Intentos { get; private set; }
    public bool Completado { get; private set; }

    private List<Button> _generatedButtons = new List<Button>();

    private void Reset()
    {
        InitializeDefaults();
    }

    private void Awake()
    {
        if (avenidas == null || avenidas.Count == 0)
            InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        avenidas = new List<AvenidaData>
        {
            new AvenidaData { nombre = "Avenida Norte", secuencia = new List<LevelType> { LevelType.Alto, LevelType.Alto, LevelType.Medio, LevelType.Alto, LevelType.Alto } },
            new AvenidaData { nombre = "Avenida Centro", secuencia = new List<LevelType> { LevelType.Medio, LevelType.Bajo, LevelType.Medio, LevelType.Bajo, LevelType.Medio } },
            new AvenidaData { nombre = "Avenida Sur",   secuencia = new List<LevelType> { LevelType.Alto, LevelType.MuyAlto, LevelType.Alto } },
            new AvenidaData { nombre = "Avenida Oeste", secuencia = new List<LevelType> { LevelType.Medio, LevelType.Alto, LevelType.Medio } }
        };
        if (string.IsNullOrEmpty(preguntaPlantilla))
            preguntaPlantilla = "¿Qué avenida termina en flujo {0}?";
    }

    private void Start()
    {
        if (bridgeControl == null)
            bridgeControl = FindAnyObjectByType<BridgeControlManager>(FindObjectsInactive.Include);

        AvenidaCorrectaIndex = ComputeCorrectAvenida();
        GenerateButtons();
        WireButtons();
        RenderProblem();
    }

    private void GenerateButtons()
    {
        _generatedButtons.Clear();
        if (botonesContainer == null || avenidas.Count == 0) return;

        // Limpia hijos previos del contenedor.
        for (int i = botonesContainer.childCount - 1; i >= 0; i--)
        {
            var child = botonesContainer.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        float startY = (avenidas.Count - 1) * 0.5f * (botonSize.y + botonSpacing);

        for (int i = 0; i < avenidas.Count; i++)
        {
            Button btn;

            if (botonPrefab != null)
            {
                btn = Instantiate(botonPrefab, botonesContainer);
                btn.name = $"Btn_{avenidas[i].nombre}";
            }
            else
            {
                btn = CreateGenericButton(avenidas[i].nombre, i, startY);
            }

            _generatedButtons.Add(btn);

            var rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = botonSize;
            float posY = startY - i * (botonSize.y + botonSpacing);
            rt.anchoredPosition = new Vector2(0f, posY);

            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = avenidas[i].nombre;
        }
    }

    private Button CreateGenericButton(string nombre, int index, float startY)
    {
        var go = new GameObject($"Btn_{nombre}", typeof(RectTransform));
        go.transform.SetParent(botonesContainer, false);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.05f, 0.15f, 0.3f, 0.95f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = image;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = nombre;
        tmp.fontSize = 60f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 24f;
        tmp.fontSizeMax = 72f;

        return btn;
    }

    private void WireButtons()
    {
        for (int i = 0; i < _generatedButtons.Count; i++)
        {
            var btn = _generatedButtons[i];
            if (btn == null) continue;
            int idx = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnAvenidaSeleccionada(idx));
        }
    }

    private void RenderProblem()
    {
        if (textoProblema == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("Secuencias de tráfico:");
        sb.AppendLine();
        for (int i = 0; i < avenidas.Count; i++)
        {
            sb.AppendLine(avenidas[i].nombre + ":");
            sb.AppendLine("    " + SecuenciaComoTexto(avenidas[i].secuencia));
        }

        textoProblema.text = sb.ToString();
    }

    private string SecuenciaComoTexto(List<LevelType> secuencia)
    {
        var parts = new List<string>();
        foreach (var lv in secuencia)
            parts.Add(NivelConvertido(lv));
        return string.Join(" → ", parts);
    }

    private string NivelConvertido(LevelType level)
    {
        switch (level)
        {
            case LevelType.MuyAlto: return "Muy alto";
            case LevelType.Alto:    return "Alto";
            case LevelType.Medio:   return "Medio";
            case LevelType.Bajo:    return "Bajo";
            default:                return level.ToString();
        }
    }

    private int ComputeCorrectAvenida()
    {
        for (int i = 0; i < avenidas.Count; i++)
        {
            foreach (var lv in avenidas[i].secuencia)
            {
                if (lv != nivelCorreccion) continue;

                if (reglaRespuesta == AnswerRule.ContainsLevel)
                    return i;

                if (reglaRespuesta == AnswerRule.EndsWithLevel &&
                    avenidas[i].secuencia.Count > 0 &&
                    avenidas[i].secuencia[avenidas[i].secuencia.Count - 1] == nivelCorreccion)
                    return i;
            }
        }
        Debug.LogWarning("[Reto2_Avenidas] Ninguna avenida cumple la regla; se asigna 0.", this);
        return 0;
    }

    public void OnAvenidaSeleccionada(int index)
    {
        if (Completado) return;

        Intentos++;
        bool correcto = index == AvenidaCorrectaIndex;

        LockPanel();

        if (index >= 0 && index < _generatedButtons.Count && _generatedButtons[index] != null)
            SetButtonColor(_generatedButtons[index], correcto ? _colorCorrecto : _colorIncorrecto);

        if (correcto)
        {
            Completado = true;
            Debug.Log($"[Reto2] ¡Correcto! {avenidas[index].nombre} ({NivelConvertido(nivelCorreccion)}) a los {Intentos} intentos.", this);
            onChallengeCompleted?.Invoke();
            if (bridgeControl != null)
                bridgeControl.RetoCompletado();
        }
        else
        {
            Debug.Log($"[Reto2] Fallo (intento {Intentos}): {avenidas[index].nombre} no es la correcta. Panel bloqueado.", this);
            onChallengeFallido?.Invoke();
            if (feedbackFallo != null)
            {
                feedbackFallo.SetActive(false);
                feedbackFallo.SetActive(true);
            }
        }
    }

    private void LockPanel()
    {
        for (int i = 0; i < _generatedButtons.Count; i++)
        {
            if (_generatedButtons[i] != null)
                _generatedButtons[i].interactable = false;
        }
    }

    private void SetButtonColor(Button btn, Color color)
    {
        btn.transition = Selectable.Transition.None;
        var img = btn.targetGraphic;
        if (img != null) img.color = color;
    }
}
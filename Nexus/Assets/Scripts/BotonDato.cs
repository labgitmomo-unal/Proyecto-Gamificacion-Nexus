using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Boton de dato en el panel mezclado (ponderacion == 0).
///
/// Mecanica: dos toques selectivos sin agarre.
///   1. Tocar/Seleccionar el boton -> se resalta AMARILLO (queda seleccionado)
///   2. Tocar/Seleccionar una categoria -> CategoriaHeader evalua el match:
///        Coincide   -> boton VERDE -> destruye -> notifica progreso
///        No coincide -> boton ROJO -> deselecciona
///
/// Sin fisica: el boton NO se mueve al tocarlo; solo cambia de color.
/// El controlador XR detecta el boton por Collider + XRBaseInteractable.
/// </summary>
[RequireComponent(typeof(XRBaseInteractable), typeof(Image))]
public class BotonDato : MonoBehaviour
{
    [Header("Categoria de este boton")]
    public string categoria;

    [Header("Colores de feedback")]
    public Color colorNormal      = new Color(0f, 0.12f, 0.22f, 1f);
    public Color colorTextoNormal = new Color(0f, 0.85f, 1f, 1f);
    public Color colorSeleccionado= new Color(1f, 0.82f, 0f, 1f);   // amarillo brillante
    public Color colorCorrecto    = new Color(0f, 1f, 0.30f, 1f);    // verde
    public Color colorIncorrecto  = new Color(0.9f, 0.1f, 0.1f, 1f);  // rojo

    private XRBaseInteractable _xr;
    private Image              _img;
    private TextMeshProUGUI    _tmp;

    public bool EstaSeleccionado { get; private set; }

    void Awake()
    {
        _xr = GetComponent<XRBaseInteractable>();
        _img = GetComponent<Image>();
        _tmp = GetComponentInChildren<TextMeshProUGUI>(true);

        if (_img != null)
            _img.color = colorNormal;
    }

    void OnEnable()
    {
        // Escuchar eventos de seleccion XR (controlador presiona trigger sobre el boton)
        _xr.selectEntered.AddListener(OnSelectEntered);

        // Seleccion multiple: varios controladores pueden tocar a la vez
        _xr.selectMode = InteractableSelectMode.Multiple;
    }

    void OnDisable()
    {
        _xr.selectEntered.RemoveListener(OnSelectEntered);
    }

    /// <summary>
    /// El controlador XR presiona el boton sobre este GO.
    /// Pasa la referencia al panel para que quede seleccionado.
    /// </summary>
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Notificar al panel: este boton fue tocado y presionado
        PanelPonderacionCero.OnBotonTocado?.Invoke(this);
    }

    /// <summary>
    /// Marca el boton como seleccionado (llamado por el panel al procesar el evento).
    /// Si ya habia un boton seleccionado, se deselecciona primero.
    /// </summary>
    public void Seleccionar()
    {
        if (EstaSeleccionado) return;

        EstaSeleccionado = true;
        if (_img != null) _img.color = colorSeleccionado;
        if (_tmp != null) _tmp.color = Color.white;

        Debug.Log($"[BotonDato] Seleccionado: '{categoria}' - '{name}'");
    }

    /// <summary>
    /// Devuelve el boton al estado normal sin seleccion.
    /// </summary>
    public void Deseleccionar()
    {
        if (!EstaSeleccionado) return;

        EstaSeleccionado = false;
        if (_img != null) _img.color = colorNormal;
        if (_tmp != null) _tmp.color = colorTextoNormal;
    }

    /// <summary>
    /// Feedback verde antes de destruir.
    /// </summary>
    public void MostrarCorrecto()
    {
        if (_img != null) _img.color = colorCorrecto;
        if (_tmp != null) _tmp.color = Color.white;
    }

    /// <summary>
    /// Flash rojo durante un breve periodo, luego deselecciona automaticamente.
    /// </summary>
    public void MostrarIncorrecto()
    {
        if (_img != null) _img.color = colorIncorrecto;
        if (_tmp != null) _tmp.color = Color.white;

        // Autodeseleccionar despues del feedback
        Invoke(nameof(Deseleccionar), 0.5f);
    }

    /// <summary>
    /// Reseteo completo sin feedback, llamado al start de ronda.
    /// </summary>
    public void ResetearVisual()
    {
        EstaSeleccionado = false;
        if (_img != null) _img.color = colorNormal;
        if (_tmp != null) _tmp.color = colorTextoNormal;
    }
}

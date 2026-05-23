using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// CategoriaHeader — reemplaza a CategoriaDropHandler + CategoriaGrabbableZone.
///
/// Coloca este script en el mismo GameObject que CategoriaDropHandler/CategoriaGrabbableZone
/// estaban antes: el header visual de categoria en la zona superior del PanelPonderacionCero.
///
/// Mecanica: XR select event (controlador presiona trigger sobre el header).
///   Requiere XRBaseInteractable + BoxCollider en el header.
///   Requiere XRBaseInteractable + SphereCollider(Trigger) en cada boton de la zona inferior.
///
/// Flujo:
///   1. Jugador toca un dato -> BotonDato.Seleccionar() [amarillo]
///   2. Jugador toca categoria -> CategoriaHeader.ProcesarSeleccion()
///        match    -> BotonDato.MostrarCorrecto() [verde] -> destruir -> notificar progreso
///        no match -> BotonDato.MostrarIncorrecto() [rojo] -> deseleccionar
/// </summary>
[RequireComponent(typeof(XRBaseInteractable), typeof(Image), typeof(BoxCollider))]
public class CategoriaHeader : MonoBehaviour
{
    public string categoria;

    [Header("Referencia texto TMP del titulo (hijo del header)")]
    public TextMeshProUGUI textoCategoria;

    [Header("Colores de feedback")]
    public Color colorNormal  = new Color(0f, 0.15f, 0.22f, 1f);
    public Color colorHover   = new Color(0f, 0.30f, 0.38f, 1f);
    public Color colorAcierto = new Color(0f, 0.55f, 0.12f, 1f);
    public Color colorError   = new Color(0.75f, 0.10f, 0.05f, 1f);

    private XRBaseInteractable _xr;
    private Image _img;
    private float _tiempoFlash;

    void Awake()
    {
        _xr      = GetComponent<XRBaseInteractable>();
        _img     = GetComponent<Image>();
        _img.color = colorNormal;
    }

    void OnEnable()
    {
        _xr.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDisable()
    {
        _xr.selectEntered.RemoveListener(OnSelectEntered);
    }

    void Update()
    {
        if (_tiempoFlash <= 0f) return;

        _tiempoFlash -= Time.deltaTime;
        if (_tiempoFlash <= 0f)
            _img.color = colorNormal;
    }

    /// <summary>
    /// El controlador XR presiona (select) sobre este header.
    /// </summary>
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        ProcesarSeleccion();
    }

    /// <summary>
    /// Evalua el match entre el boton seleccionado y esta categoria.
    /// </summary>
    public void ProcesarSeleccion()
    {
        // Obtener el boton seleccionado por el panel
        BotonDato boton = PanelPonderacionCero.BotonSeleccionado;

        // Sin boton: solo feedback visual (parpadeo breve)
        if (boton == null)
        {
            _img.color = colorHover;
            _tiempoFlash = 0.25f;
            Debug.Log($"[CategoriaHeader] Tocado '{categoria}' sin boton seleccionado.");
            return;
        }

        bool coincide = boton.categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase);

        if (coincide)
        {
            // ===== ACIERTO =====
            boton.MostrarCorrecto();
            boton.Deseleccionar(); // quitar estado seleccionado
            PanelPonderacionCero.BotonSeleccionado = null;

            // feedback header
            _img.color = colorAcierto;
            _tiempoFlash = 0.70f;

            // notificar progreso (destruccion se hara despues del feedback visual)
            ProgresoAbstraccion.NotificarEliminacion();

            Debug.Log($"[CategoriaHeader] ACIERTO: '{boton.categoria}' -> '{categoria}'. Eliminando boton.");

            Destroy(boton.gameObject, 0.5f);
        }
        else
        {
            // ===== ERROR =====
            boton.MostrarIncorrecto();
            PanelPonderacionCero.BotonSeleccionado = null;

            _img.color = colorError;
            _tiempoFlash = 0.50f;

            Debug.Log($"[CategoriaHeader] ERROR: '{boton.categoria}' NO es '{categoria}'. Boton deseleccionado.");
        }
    }

    /// <summary>Resetea colores al cerrar/reiniciar el panel.</summary>
    public void ResetearVisual()
    {
        _tiempoFlash = 0f;
        _img.color   = colorNormal;
    }
}

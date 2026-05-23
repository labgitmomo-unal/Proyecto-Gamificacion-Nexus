using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Physics-based XR grab behavior for UI buttons in the PanelPonderacionCero.
///
/// Lifecycle:
///   Grabbed  → button lerps toward the XR controller position at configurable speed
///              using the controller's own velocity as the target seed.
///   Released → checks nearest CategoriaGrabbableZone via OverlapSphere
///              Correct zone → destroy gameObject
///              Wrong zone  → lerp back to original spawn transform, flash red, then reset
///              No zone     → lerp back to original spawn transform, restore normal color
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class AbductionGrabbable : MonoBehaviour
{
    [Header("Grab Follow")]
    [Tooltip("Lerp factor toward the XR controller each frame while grabbed (0 = no follow, 1 = instant snap).")]
    [Range(1f, 40f)]
    public float followSpeed = 20f;

    [Tooltip("Lerp factor when returning the button to its original spot after a wrong/non-match drop (0 = no lerp, 1 = teleport).")]
    [Range(1f, 40f)]
    public float returnSpeed = 10f;

    [Header("Drop Detection")]
    [Tooltip("Radius of the sphere used to find category zone colliders on release.")]
    public float pickupSphereRadius = 0.25f;

    [Tooltip("LayerMask for the CategoriaGrabbableZone layer. Defaults to Everything if not set.")]
    public LayerMask zoneLayerMask = Physics.DefaultRaycastLayers;

    [Header("Visual Feedback")]
    public Color normalColor   = new Color(0f, 0.85f, 1f, 1f);
    public Color retornoPingColor = new Color(1f, 0.25f, 0.15f, 1f);   // rojo breve al soltar mal
    public float retornoTiempo = 0.5f;  // segundos hasta volver a posicion original

    // ── references ──────────────────────────────────────────────────────────
    XRGrabInteractable  _xrg;
    Rigidbody           _rb;
    Image               _img;
    Vector3             _lastControllerPos;   // controller position sampled last frame
    float               _followLerp;          // lerp per-frame value (followSpeed * DT)

    // grab / return state
    Transform           _originalParent;
    int                 _originalSiblingIndex;
    Vector3             _originalLocalPos;

    bool                _isGrabbed;
    bool                _isReturning;
    Coroutine           _returnRoutine;

    void Awake()
    {
        _xrg = GetComponent<XRGrabInteractable>();
        _rb  = GetComponent<Rigidbody>();

        // We only need kinematic while being physically driven by XRI teleport
        _rb.isKinematic    = true;
        _rb.useGravity     = false;
        _rb.interpolation  = RigidbodyInterpolation.Interpolate;

        // Try to cache the main Image for color feedback
        _img = GetComponent<Image>();
        if (_img == null) _img = GetComponentInChildren<Image>();

        // ── Pad XRGrabInteractable para comportamiento de agarre suave ──────
        _xrg.trackPosition             = false;
        _xrg.trackRotation             = false;
        _xrg.snapToColliderVolume      = true;
        _xrg.throwVelocityScale        = 1.2f;
        _xrg.throwAngularVelocityScale = 0.8f;
        _xrg.matchAttachRotation       = false;

        // Kinematic off: XR grab usa MovePosition interno; physics activa al soltar
        _rb.isKinematic = false;
    }

    void OnEnable()
    {
        _xrg.selectEntered.AddListener(OnGrabBegin);
        _xrg.selectExited .AddListener(OnGrabEnd);
    }

    void OnDisable()
    {
        _xrg.selectEntered.RemoveListener(OnGrabBegin);
        _xrg.selectExited .RemoveListener(OnGrabEnd);
    }

    void FixedUpdate()
    {
        if (!_isGrabbed) return;

        // ── read the first active selecting interactor (controller that holds this button)
        if (_xrg.interactorsSelecting == null || _xrg.interactorsSelecting.Count == 0) return;
        var interactor = _xrg.interactorsSelecting[0];
        if (interactor == null) return;

        Vector3 ctrlPos = interactor.transform.position;
        _lastControllerPos = ctrlPos;

        // ── lerp button toward controller ──────────────────────────────────
        _followLerp = 1f - Mathf.Exp(-followSpeed * Time.fixedDeltaTime);
        transform.position = Vector3.Lerp(transform.position, ctrlPos, _followLerp);
    }

    void OnGrabBegin(SelectEnterEventArgs args)
    {
        _isGrabbed = true;
        _isReturning = false;

        // stop any pending return animation
        if (_returnRoutine != null)
        {
            StopCoroutine(_returnRoutine);
            _returnRoutine = null;
        }

        // freeze the button visually so UI elements (e.g. TextMeshPro) don't flicker
        SetKinematic(true);
        ResetColor();

        // remember where this button is supposed to go back to
        _originalParent       = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();
        _originalLocalPos     = transform.localPosition;

        // reset velocity tracking
        _lastControllerPos = args.interactorObject.transform.position;
    }

    void OnGrabEnd(SelectExitEventArgs args)
    {
        _isGrabbed = false;
        SetKinematic(false);

        Vector3 btnWorldPos = transform.position;
        Collider[] hits = Physics.OverlapSphere(btnWorldPos, pickupSphereRadius, zoneLayerMask);

        // Find all CategoriaGrabbableZone hits
        // Use the NEAREST zone for the decision
        CategoriaGrabbableZone nearestZone = null;
        float nearestDistSq = float.MaxValue;

        foreach (var hit in hits)
        {
            var zone = hit.GetComponent<CategoriaGrabbableZone>();
            if (zone == null) continue;
            float dSq = (hit.transform.position - btnWorldPos).sqrMagnitude;
            if (dSq < nearestDistSq)
            {
                nearestDistSq = dSq;
                nearestZone   = zone;
            }
        }

        if (nearestZone != null)
        {
            var dh = nearestZone.GetComponent<CategoriaDropHandler>();
            if (dh != null && dh.categoria == categoria)
            {
                // ── Correct category ──────────────────────────────────────
                // Reportar eliminacion antes de destruir (si aplica)
                if (ponderacionEsUno)
                    ProgresoAbstraccion.NotificarEliminacion();

                // Don't parent-subtract here — we're about to destroy
                Destroy(gameObject);
                return;
            }

            // Wrong zone: flash red once and return to origin
            StartReturn(normalColor, retornoPingColor, true);
        }
        else
        {
            // No zone inside radius: return to origin at normal speed without color change
            StartReturn(normalColor, normalColor, false);
        }
    }

    void StartReturn(Color from, Color to, bool flashRed)
    {
        if (_returnRoutine != null)
            StopCoroutine(_returnRoutine);

        _returnRoutine = StartCoroutine(ReturnToOrigin(from, to, flashRed));
    }

    IEnumerator ReturnToOrigin(Color colorStart, Color colorEnd, bool flashRed)
    {
        _isReturning = true;

        // Flash wrong indication
        if (colorStart != colorEnd)
            SetTmpColor(colorStart);
        else
            SetTmpColor(normalColor);

        // Return to the spawn transform
        transform.SetParent(_originalParent, false);
        transform.SetSiblingIndex(_originalSiblingIndex);
        transform.localPosition = _originalLocalPos;

        // Brief color feedback before returning to normal
        if (flashRed)
        {
            yield return new WaitForSeconds(retornoTiempo * 0.4f);
            SetTmpColor(colorEnd);       // back to normal
            yield return new WaitForSeconds(retornoTiempo * 0.6f);
        }
        else
        {
            yield return new WaitForSeconds(retornoTiempo);
        }

        _isReturning = false;
        _returnRoutine = null;
        SetTmpColor(normalColor);
    }

    void SetKinematic(bool on)
    {
        if (_rb != null)
        {
            _rb.isKinematic = on;
            _rb.velocity   = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    void SetTmpColor(Color c)
    {
        var tmp = GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.color = c;
    }

    void ResetColor()
    {
        SetTmpColor(normalColor);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the pickup sphere in the Scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupSphereRadius);
    }

    // ── populated by PanelPonderacionCero.CrearBoton ────────────────────────
    public string categoria;

    /// <summary>
    /// Si es true y la categoria OUTPUT coincide, se dispara
    /// <see cref="ProgresoAbstraccion.NotificarEliminacion"/> antes de destruir
    /// el boton — mantiene sincronizado el progreso del panel principal.
    /// </summary>
    public bool ponderacionEsUno = false;
}

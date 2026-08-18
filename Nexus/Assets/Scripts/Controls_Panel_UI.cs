using UnityEngine;
using UnityEngine.InputSystem;

public class Controls_Panel_UI : MonoBehaviour
{
    [Tooltip("Canvas (world space) que se muestra al alternar con el botón B del mando derecho.")]
    public GameObject panel;
    [Tooltip("Cámara a la que el panel debe seguir (por defecto Camera.main).")]
    public Camera targetCamera;
    [Tooltip("Distancia en metros frente a la cámara donde se coloca el panel.")]
    public float panelDistance = 1.35f;
    [Tooltip("Desplazamiento vertical adicional sobre el centro de la vista (metros).")]
    public float verticalOffset = 0f;

    [Header("Debug")]
    public bool debugSimulateKeyB = true;
    [Tooltip("Imprime en consola cada cambio de estado del panel.")]
    public bool debugLog = true;

    private InputAction bButtonAction;
    private bool previousPressed;
    private bool isVisible;
    private Camera cachedCamera;

    private void Awake()
    {
        bButtonAction = new InputAction("B_Right_Toggle_Panel", InputActionType.Button);
        bButtonAction.AddBinding("<XRController>{RightHand}/secondaryButton");
        bButtonAction.Enable();
    }

    private void Start()
    {
        cachedCamera = targetCamera != null ? targetCamera : Camera.main;
    }

    private void OnDestroy()
    {
        if (bButtonAction != null)
        {
            bButtonAction.Disable();
            bButtonAction.Dispose();
        }
    }

    private void Update()
    {
        bool pressed = false;
        if (bButtonAction != null)
            pressed = bButtonAction.ReadValue<float>() > 0.5f;
        if (debugSimulateKeyB && Keyboard.current != null)
            pressed = pressed || Keyboard.current[Key.B].isPressed;

        if (pressed && !previousPressed)
            Toggle();
        previousPressed = pressed;
    }

    private void LateUpdate()
    {
        if (panel == null || !panel.activeSelf)
            return;

        if (cachedCamera == null)
            cachedCamera = targetCamera != null ? targetCamera : Camera.main;
        if (cachedCamera == null)
            return;

        Transform cam = cachedCamera.transform;
        Vector3 desired = cam.position + cam.forward * panelDistance + cam.up * verticalOffset;
        panel.transform.position = desired;
        panel.transform.rotation = Quaternion.LookRotation(panel.transform.position - cam.position, cam.up);
    }

    public void Toggle()
    {
        isVisible = !isVisible;
        if (panel != null)
            panel.SetActive(isVisible);
        if (debugLog)
            Debug.Log("[Controls_Panel_UI] Panel " + (isVisible ? "ABIERTO/frente a la camara" : "cerrado"));
    }

    public void Show()
    {
        if (!isVisible)
            Toggle();
    }

    public void Hide()
    {
        if (isVisible)
            Toggle();
    }
}
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class BridgeStart_Button : MonoBehaviour
{
    public BridgeControlManager bridgeManager;
    public MeshRenderer buttonRenderer;
    public Color idleColor = Color.blue;
    public Color pressedColor = Color.green;
    public Color disabledColor = Color.gray;

    private XRSimpleInteractable _interactable;
    private Material _material;

    void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();

        if (buttonRenderer != null)
        {
            _material = buttonRenderer.material;
            _material.color = idleColor;
        }
    }

    void OnEnable()
    {
        if (_interactable != null)
            _interactable.selectEntered.AddListener(OnPressed);
    }

    void OnDisable()
    {
        if (_interactable != null)
            _interactable.selectEntered.RemoveListener(OnPressed);
    }

    void OnPressed(SelectEnterEventArgs args)
    {
        if (bridgeManager == null) return;
        if (bridgeManager.IsActive || bridgeManager.IsComplete) return;

        bridgeManager.StartChallenge();

        if (_material != null)
            _material.color = pressedColor;
    }

    void Update()
    {
        if (_material == null) return;
        if (bridgeManager == null) return;

        if (bridgeManager.IsComplete || bridgeManager.IsActive)
            _material.color = bridgeManager.IsComplete ? pressedColor : disabledColor;
    }

    void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }
}

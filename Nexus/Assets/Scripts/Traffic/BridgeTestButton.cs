using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class BridgeTestButton : MonoBehaviour
{
    private BridgeControlManager bridgeControl;

    void Start()
    {
        bridgeControl = FindAnyObjectByType<BridgeControlManager>(FindObjectsInactive.Include);

        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnPressed);
    }

    private void OnPressed(SelectEnterEventArgs args)
    {
        if (bridgeControl == null) return;

        if (!bridgeControl.IsActive)
            bridgeControl.FreezeBridge();

        bridgeControl.ReleaseStep();
    }
}

using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

public class AutoKeyboardLink : MonoBehaviour
{
    private void Start()
    {
        var fields = FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var field in fields)
        {
            if (field.gameObject.scene != gameObject.scene) continue;
            var display = field.GetComponent<XRKeyboardDisplay>();
            if (display == null)
                display = field.gameObject.AddComponent<XRKeyboardDisplay>();
            display.inputField = field;
            display.useSceneKeyboard = false;
            display.updateOnKeyPress = true;
            display.monitorInputFieldCharacterLimit = field.characterLimit > 0;
            field.shouldHideSoftKeyboard = true;
        }
    }
}

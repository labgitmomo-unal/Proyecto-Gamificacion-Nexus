using UnityEngine;
using System.Collections.Generic;


[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class TrafficCar_Interactible : MonoBehaviour
{
    [HideInInspector] public int colorIndex;
    [HideInInspector] public ZonePatternManager patternManager;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable _interactable;
    private List<Material> _materials = new List<Material>();
    private List<Color> _originalColors = new List<Color>();
    private bool _touched = false;
    private bool _isCorrect = false;
    private bool _ready = false;

    void Awake()
    {
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        var renderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i].material;
            if (mat != null)
            {
                _materials.Add(mat);
                _originalColors.Add(mat.color);
            }
        }

        if (_materials.Count == 0)
        {
            Debug.LogWarning($"[TrafficCar_Interactible] {name}: no materials found.", this);
            return;
        }

        if (_interactable != null)
            _interactable.selectEntered.AddListener(_ => OnTouched());

        _ready = true;
    }

    void OnTouched()
    {
        if (_touched || patternManager == null) return;
        _touched = true;
        patternManager.OnCarTouched(this);
    }

    public void AssignColor(int index, Color color)
    {
        if (!_ready) return;
        colorIndex = index;
        _touched = false;
        _isCorrect = false;
        for (int i = 0; i < _materials.Count; i++)
            _materials[i].color = color;
    }

    public void HighlightCorrect()
    {
        if (!_ready) return;
        _isCorrect = true;
        for (int i = 0; i < _materials.Count; i++)
            _materials[i].color = Color.green;
    }

    public void HighlightWrong()
    {
        if (!_ready) return;
        for (int i = 0; i < _materials.Count; i++)
            _materials[i].color = Color.red;
    }

    public void ResetVisual()
    {
        if (!_ready) return;
        _touched = false;
        _isCorrect = false;
        for (int i = 0; i < _materials.Count; i++)
            _materials[i].color = _originalColors[i];
    }

    void OnDestroy()
    {
        if (_interactable != null)
            _interactable.selectEntered.RemoveAllListeners();
    }
}

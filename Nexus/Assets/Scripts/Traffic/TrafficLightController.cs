using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public BridgeControlManager bridgeManager;

    [ColorUsage(true, true)]
    public Color redEmission = new Color(2f, 0f, 0f);

    [ColorUsage(true, true)]
    public Color greenEmission = new Color(0f, 2f, 0f);

    public Color redBase = new Color(0.15f, 0.01f, 0.01f);
    public Color greenBase = new Color(0.01f, 0.15f, 0.01f);
    public Color normalBase = new Color(0.10f, 0.10f, 0.09f);
    public Color normalEmission = new Color(0.40f, 0.40f, 0.40f);

    private Material _mat;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColor = Shader.PropertyToID("_Color");

    void Start()
    {
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) _mat = mr.material;

        if (bridgeManager == null)
            bridgeManager = FindFirstObjectByType<BridgeControlManager>();
    }

    void Update()
    {
        if (_mat == null || bridgeManager == null) return;

        if (bridgeManager.IsComplete)
        {
            _mat.SetColor(EmissionColor, greenEmission);
            _mat.SetColor(BaseColor, greenBase);
        }
        else if (bridgeManager.IsActive)
        {
            bool anyReleased = false;
            var freezers = FindObjectsByType<BridgeCarFreezer>(FindObjectsSortMode.None);
            foreach (var f in freezers)
            {
                if (f != null && !f.IsFrozen)
                {
                    anyReleased = true;
                    break;
                }
            }

            if (anyReleased)
            {
                _mat.SetColor(EmissionColor, greenEmission);
                _mat.SetColor(BaseColor, greenBase);
            }
            else
            {
                _mat.SetColor(EmissionColor, redEmission);
                _mat.SetColor(BaseColor, redBase);
            }
        }
        else
        {
            _mat.SetColor(EmissionColor, normalEmission);
            _mat.SetColor(BaseColor, normalBase);
        }
    }

    void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }
}

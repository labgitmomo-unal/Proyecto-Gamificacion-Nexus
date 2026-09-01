using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BridgeTrafficLight : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private BridgeControlManager bridgeControl;
    private Renderer myRenderer;
    private Material sharedMaterial;
    private MaterialPropertyBlock propertyBlock;
    private Color colorOriginal;
    private int currentState = -1;

    private void Start()
    {
        bridgeControl = BridgeControlManager.Instance;
        myRenderer = GetComponent<Renderer>();
        if (myRenderer == null)
            return;

        sharedMaterial = myRenderer.sharedMaterial;
        if (sharedMaterial == null)
            return;

        propertyBlock = new MaterialPropertyBlock();
        colorOriginal = sharedMaterial.GetColor(EmissionColorId);
        sharedMaterial.EnableKeyword("_EMISSION");
        ActualizarLuces(0);
    }

    private void Update()
    {
        if (bridgeControl == null)
        {
            bridgeControl = BridgeControlManager.Instance;
            if (bridgeControl == null)
                return;
        }

        int nuevoEstado = 0;
        if (bridgeControl.IsComplete)
            nuevoEstado = 2;
        else if (bridgeControl.IsActive)
            nuevoEstado = bridgeControl.IsReleased ? 2 : 1;

        if (nuevoEstado != currentState)
            ActualizarLuces(nuevoEstado);
    }

    private void ActualizarLuces(int estado)
    {
        if (myRenderer == null || propertyBlock == null || estado == currentState)
            return;

        Color color = estado switch
        {
            1 => Color.red,
            2 => Color.green,
            _ => colorOriginal
        };

        myRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(EmissionColorId, color);
        myRenderer.SetPropertyBlock(propertyBlock);
        currentState = estado;
    }
}

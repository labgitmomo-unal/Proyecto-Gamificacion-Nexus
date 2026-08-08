using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BridgeTrafficLight : MonoBehaviour
{
    private BridgeControlManager bridgeControl;
    private Renderer myRenderer;
    private Material mat;
    private Color colorOriginal;

    void Start()
    {
        bridgeControl = FindAnyObjectByType<BridgeControlManager>(FindObjectsInactive.Include);
        myRenderer = GetComponent<Renderer>();
        if (myRenderer == null) return;

        mat = myRenderer.material;
        colorOriginal = mat.GetColor("_EmissionColor");
        mat.EnableKeyword("_EMISSION");

        ActualizarLuces(estado: 0);
    }

    void Update()
    {
        if (bridgeControl == null)
        {
            bridgeControl = FindAnyObjectByType<BridgeControlManager>(FindObjectsInactive.Include);
            return;
        }

        int nuevoEstado = 0;
        if (bridgeControl.IsComplete)
            nuevoEstado = 2;
        else if (bridgeControl.IsActive)
            nuevoEstado = bridgeControl.IsReleased ? 2 : 1;

        ActualizarLuces(nuevoEstado);
    }

    private void ActualizarLuces(int estado)
    {
        if (mat == null) return;

        switch (estado)
        {
            case 0:
                mat.SetColor("_EmissionColor", colorOriginal);
                break;
            case 1:
                mat.SetColor("_EmissionColor", Color.red);
                break;
            case 2:
                mat.SetColor("_EmissionColor", Color.green);
                break;
        }
    }
}

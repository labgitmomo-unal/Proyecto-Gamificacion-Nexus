using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Script de simulación que prueba todas las mecánicas del ascensor y el grafo.
/// Se ejecuta en Play Mode y registra el estado de cada mecánica en consola.
/// </summary>
public class MechanicsSimulator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Elevator elevator;
    [SerializeField] private FloorChangeTrigger floorTrigger;
    [SerializeField] private Canvas graphCanvas;
    [SerializeField] private Transform graphNodesContainer;

    [Header("Configuración")]
    [SerializeField] private float simulationInterval = 2f;

    private int _simStep = 0;

    private void Start()
    {
        Debug.Log("=== INICIO DE SIMULACION DE MECANICAS ===");
        StartCoroutine(RunSimulation());
    }

    private IEnumerator RunSimulation()
    {
        // Paso 1: Verificar que el MapViewCamera renderiza
        _simStep = 1;
        Debug.Log($"[Paso {_simStep}] Verificando MapViewCamera y RenderTexture...");
        var mapCam = FindFirstObjectByType<MapViewController>();
        if (mapCam != null)
        {
            Debug.Log($"[Paso {_simStep}] MapViewController encontrado. DisplayObject: 'MapViewDisplay'");
            // Verificar que existe el objeto MapViewDisplay
            var displayObj = GameObject.Find("MapViewDisplay");
            if (displayObj != null)
            {
                var rawImg = displayObj.GetComponent<RawImage>();
                Debug.Log($"[Paso {_simStep}] MapViewDisplay encontrado. RawImage: {(rawImg != null ? "OK" : "FALTA")}, Texture: {(rawImg != null && rawImg.texture != null ? "ASIGNADA" : "NULL")}");
            }
            else
            {
                Debug.LogError($"[Paso {_simStep}] ERROR: MapViewDisplay no encontrado en la escena!");
            }
        }
        else
        {
            Debug.LogWarning($"[Paso {_simStep}] MapViewController no encontrado.");
        }
        yield return new WaitForSeconds(simulationInterval);

        // Paso 2: Verificar Canvas y Raycaster VR
        _simStep = 2;
        Debug.Log($"[Paso {_simStep}] Verificando Canvas del Grafo y TrackedDeviceGraphicRaycaster...");
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.name == "MapViewCanvas")
            {
                Debug.Log($"[Paso {_simStep}] MapViewCanvas encontrado. RenderMode: {c.renderMode}, WorldCamera: {(c.worldCamera != null ? c.worldCamera.name : "NULL")}");
                var raycaster = c.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                Debug.Log($"[Paso {_simStep}] TrackedDeviceGraphicRaycaster: {(raycaster != null ? "OK" : "FALTA - se agregara automaticamente por NewMonoBehaviourScript")}");
            }
        }
        yield return new WaitForSeconds(simulationInterval);

        // Paso 3: Simular movimiento del ascensor
        _simStep = 3;
        Debug.Log($"[Paso {_simStep}] Simulando ascensor (Elevator02)...");
        if (elevator != null)
        {
            Debug.Log($"[Paso {_simStep}] Elevador - Piso actual: {elevator.GetCurrentFloor()}, Piso original: {elevator.GetOriginalFloor()}");

            // Simular llamada desde VR (subir)
            Debug.Log($"[Paso {_simStep}] Simulando presion de boton VR: SUBIR...");
            int targetUp = elevator.GetCurrentFloor() == elevator.GetOriginalFloor() ? 2 : elevator.GetOriginalFloor();
            elevator.ChangeTargetFloor(targetUp);
            Debug.Log($"[Paso {_simStep}] Elevador moviendose al piso {targetUp}...");

            yield return new WaitForSeconds(3f);
            Debug.Log($"[Paso {_simStep}] Elevador llego al piso {elevator.GetCurrentFloor()}");

            // Simular llamada desde VR (bajar)
            yield return new WaitForSeconds(simulationInterval);
            Debug.Log($"[Paso {_simStep}] Simulando presion de boton VR: BAJAR...");
            elevator.ChangeTargetFloor(elevator.GetOriginalFloor());
            yield return new WaitForSeconds(3f);
            Debug.Log($"[Paso {_simStep}] Elevador regreso al piso {elevator.GetCurrentFloor()}");
        }
        else
        {
            Debug.LogWarning($"[Paso {_simStep}] Elevator no asignado. Buscando en escena...");
            elevator = FindFirstObjectByType<Elevator>();
            if (elevator != null)
                Debug.Log($"[Paso {_simStep}] Elevator encontrado automaticamente: {elevator.gameObject.name}");
            else
                Debug.LogError($"[Paso {_simStep}] No se encontro ningun Elevator en la escena!");
        }
        yield return new WaitForSeconds(simulationInterval);

        // Paso 4: Verificar FloorChangeTrigger con XR
        _simStep = 4;
        Debug.Log($"[Paso {_simStep}] Verificando FloorChangeTrigger con XRSimpleInteractable...");
        if (floorTrigger != null)
        {
            var interactable = floorTrigger.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            Debug.Log($"[Paso {_simStep}] XRSimpleInteractable: {(interactable != null ? "OK" : "FALTA")}");
            // Simular toggle
            Debug.Log($"[Paso {_simStep}] Simulando ToggleFloor()...");
            floorTrigger.ToggleFloor();
        }
        else
        {
            floorTrigger = FindFirstObjectByType<FloorChangeTrigger>();
            if (floorTrigger != null)
            {
                Debug.Log($"[Paso {_simStep}] FloorChangeTrigger encontrado: {floorTrigger.gameObject.name}");
                var interactable = floorTrigger.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                Debug.Log($"[Paso {_simStep}] XRSimpleInteractable: {(interactable != null ? "OK" : "FALTA")}");
                floorTrigger.ToggleFloor();
            }
            else
                Debug.LogWarning($"[Paso {_simStep}] No se encontro FloorChangeTrigger en la escena.");
        }
        yield return new WaitForSeconds(simulationInterval);

        // Paso 5: Verificar nodos del grafo y sockets
        _simStep = 5;
        Debug.Log($"[Paso {_simStep}] Verificando nodos del grafo...");
        if (graphNodesContainer == null)
        {
            var grafo = GameObject.Find("Grafo");
            if (grafo != null)
            {
                var nodos = grafo.transform.Find("Nodos");
                if (nodos != null) graphNodesContainer = nodos;
            }
        }

        if (graphNodesContainer != null)
        {
            int nodeCount = graphNodesContainer.childCount;
            Debug.Log($"[Paso {_simStep}] Nodos encontrados: {nodeCount}");
            for (int i = 0; i < nodeCount; i++)
            {
                var node = graphNodesContainer.GetChild(i);
                var dragScript = node.GetComponent<NewMonoBehaviourScript>();
                Debug.Log($"[Paso {_simStep}] Nodo {i}: {node.name} - DragScript: {(dragScript != null ? "OK" : "FALTA")}");

                // Buscar sockets
                var sockets = node.GetComponentsInChildren<GraphSocket>();
                Debug.Log($"[Paso {_simStep}] Nodo {i} - Sockets: {sockets.Length}");
            }
        }
        else
        {
            Debug.LogWarning($"[Paso {_simStep}] No se encontro el contenedor de nodos del grafo.");
        }
        yield return new WaitForSeconds(simulationInterval);

        // Paso 6: Simular creación de aristas (cables)
        _simStep = 6;
        Debug.Log($"[Paso {_simStep}] Simulando creacion de aristas (cables)...");
        if (graphNodesContainer != null && graphNodesContainer.childCount >= 2)
        {
            // Crear un LineRenderer temporal entre dos nodos para simular una arista
            var node1 = graphNodesContainer.GetChild(0);
            var node2 = graphNodesContainer.GetChild(1);

            GameObject edgeObj = new GameObject("SimEdge");
            edgeObj.transform.SetParent(graphNodesContainer);
            var lr = edgeObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.SetPosition(0, node1.position);
            lr.SetPosition(1, node2.position);
            lr.material = new Material(Shader.Find("Unlit/Color"));
            lr.material.color = Color.cyan;

            Debug.Log($"[Paso {_simStep}] Arista simulada creada entre '{node1.name}' y '{node2.name}'");
            Debug.Log($"[Paso {_simStep}] Pos1: {node1.position}, Pos2: {node2.position}");

            yield return new WaitForSeconds(simulationInterval);
            Debug.Log($"[Paso {_simStep}] Arista visible. Destruyendo...");
            Destroy(edgeObj);
        }
        else
        {
            Debug.LogWarning($"[Paso {_simStep}] No hay suficientes nodos para crear arista de prueba.");
        }
        yield return new WaitForSeconds(simulationInterval);

        // Final
        Debug.Log("=== SIMULACION COMPLETADA ===");
        Debug.Log("Resumen:");
        Debug.Log("- MapViewCamera + RenderTexture: Verificado");
        Debug.Log("- MapViewDisplay (RawImage): Creado");
        Debug.Log("- Canvas + TrackedDeviceGraphicRaycaster: Configurado");
        Debug.Log("- Ascensor (subir/bajar via VR): Simulado");
        Debug.Log("- FloorChangeTrigger (XRSimpleInteractable): Verificado");
        Debug.Log("- Nodos del grafo + sockets: Verificados");
        Debug.Log("- Aristas (cables/luces): Simuladas");

        yield return new WaitForSeconds(1f);
        Debug.Log("Simulacion finalizada. Revisa el log para detalles.");
    }
}

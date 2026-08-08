using UnityEngine;

/// <summary>
/// Conecta la finalizacion de un reto con el avance del trafico.
/// Sustituye al boton de desarrollo: cuando el reto (PatternPuzzleManager) termina
/// la validacion (ultima base iluminada en verde), dispara BridgeControlManager.ReleaseStep(),
/// que pone el semaforo en VERDE y deja avanzar el trafico un tiempo determinado.
/// </summary>
public class RetoTraficoLinker : MonoBehaviour
{
    [Header("Reto que dispara el avance")]
    [Tooltip("Si se deja vacio, se busca automaticamente en la escena.")]
    [SerializeField] private PatternPuzzle.PatternPuzzleManager reto;

    [Header("Control del trafico")]
    [Tooltip("Si se deja vacio, se busca automaticamente en la escena.")]
    [SerializeField] private BridgeControlManager bridgeControl;

    private void Start()
    {
        if (reto == null)
            reto = FindAnyObjectByType<PatternPuzzle.PatternPuzzleManager>(FindObjectsInactive.Include);
        if (bridgeControl == null)
            bridgeControl = FindAnyObjectByType<BridgeControlManager>(FindObjectsInactive.Include);

        if (reto == null)
        {
            Debug.LogWarning("[RetoTraficoLinker] No se encontro PatternPuzzleManager.", this);
            return;
        }

        reto.onChallengeCompleted.RemoveListener(OnRetoCompletado);
        reto.onChallengeCompleted.AddListener(OnRetoCompletado);
    }

    private void OnRetoCompletado()
    {
        if (bridgeControl == null)
        {
            Debug.LogWarning("[RetoTraficoLinker] No hay BridgeControlManager para avanzar el trafico.", this);
            return;
        }

        bridgeControl.RetoCompletado();
    }
}
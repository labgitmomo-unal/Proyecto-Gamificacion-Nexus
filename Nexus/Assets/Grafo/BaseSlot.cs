using UnityEngine;

/// <summary>
/// Componente ligero que expone el estado de ocupación de una base del grafo.
/// Marca la base como ocupada cuando una bola con el tag configurado entra en
/// contacto con la base. Funciona con MeshCollider normal, sin activar IsTrigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BaseSlot : MonoBehaviour
{
    [Header("Tags")]
    [SerializeField] private string _tagBola = "Ball";

    private readonly System.Collections.Generic.Dictionary<int, Collider> _colisionadoresDentro = new();

    /// <summary>True si hay una bola tocando esta base.</summary>
    public bool IsOccupied => _colisionadoresDentro.Count > 0;

    /// <summary>
    /// Intenta obtener la posición del primer collider que está tocando la base.
    /// Devuelve true si hay un contacto y out contiene la posición mundial del collider.
    /// </summary>
    public bool TryGetContactPosition(out Vector3 position)
    {
        foreach (var kv in _colisionadoresDentro)
        {
            if (kv.Value != null)
            {
                position = kv.Value.transform.position;
                return true;
            }
        }

        position = transform.position;
        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || !collision.collider.CompareTag(_tagBola)) return;

        _colisionadoresDentro[collision.collider.GetInstanceID()] = collision.collider;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision == null || !collision.collider.CompareTag(_tagBola)) return;

        _colisionadoresDentro.Remove(collision.collider.GetInstanceID());
    }
}

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

    private readonly System.Collections.Generic.HashSet<int> _colisionadoresDentro = new();

    /// <summary>True si hay una bola tocando esta base.</summary>
    public bool IsOccupied => _colisionadoresDentro.Count > 0;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || !collision.collider.CompareTag(_tagBola)) return;

        _colisionadoresDentro.Add(collision.collider.GetInstanceID());
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision == null || !collision.collider.CompareTag(_tagBola)) return;

        _colisionadoresDentro.Remove(collision.collider.GetInstanceID());
    }
}

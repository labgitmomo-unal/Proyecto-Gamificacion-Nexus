using UnityEngine;
using System.Collections.Generic;

public class TrafficZone : MonoBehaviour
{
    [Header("Identificador")]
    public string zoneId = "Zona_1";

    [Header("Patrón de colores (opcional)")]
    public ZonePatternManager patternManager;

    private List<MovementController> _carsInZone = new List<MovementController>();

    public int CarCount => _carsInZone.Count;

    void OnTriggerEnter(Collider other)
    {
        var mc = other.GetComponent<MovementController>();
        if (mc == null || _carsInZone.Contains(mc)) return;

        _carsInZone.Add(mc);

        var interactible = other.GetComponent<TrafficCar_Interactible>();
        if (interactible == null)
            interactible = other.gameObject.AddComponent<TrafficCar_Interactible>();

        if (patternManager != null)
            patternManager.RegisterCar(interactible);
    }

    void OnTriggerExit(Collider other)
    {
        var mc = other.GetComponent<MovementController>();
        if (mc == null) return;

        var interactible = other.GetComponent<TrafficCar_Interactible>();
        if (interactible != null && patternManager != null)
            patternManager.UnregisterCar(interactible);

        _carsInZone.Remove(mc);
    }

    private void LimpiarNulos()
    {
        for (int i = _carsInZone.Count - 1; i >= 0; i--)
        {
            if (_carsInZone[i] == null)
                _carsInZone.RemoveAt(i);
        }
    }

    void OnDrawGizmosSelected()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}

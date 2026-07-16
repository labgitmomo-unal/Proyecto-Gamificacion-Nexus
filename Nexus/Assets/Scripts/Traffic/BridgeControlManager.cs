using UnityEngine;
using System.Collections.Generic;
public class BridgeControlManager : MonoBehaviour
{
    [Header("Plantilla del Spawner (1) - arrastrar Script Move")]
    public MovementController spawnerTemplate;

    [Header("Patrones (1 por zona, en mismo orden)")]
    public ZonePatternManager[] patternManagers;

    public int CurrentZoneIndex { get; private set; }
    public bool IsComplete { get; private set; }
    public bool IsActive { get; private set; }

    public static event System.Action OnAllZonesComplete;

    void Start()
    {
        CurrentZoneIndex = 0;
        IsComplete = false;
        IsActive = false;

        if (spawnerTemplate == null)
            Debug.LogError("[BridgeControl] No hay spawnerTemplate asignado.");
    }

    public void StartChallenge()
    {
        if (IsActive || IsComplete) return;
        IsActive = true;
        CurrentZoneIndex = 0;

        if (TrafficManager.Instance != null)
            TrafficManager.Instance.SetMultiplicadorPorPlantilla(spawnerTemplate, 0f);

        Debug.Log("[BridgeControl] Tráfico del puente CONGELADO vía per-spawner.");

        AsignarAutosAZonasVirtuales();

        if (patternManagers != null && patternManagers.Length > 0 && patternManagers[0] != null)
            patternManagers[0].ActivateZone();
    }

    private void AsignarAutosAZonasVirtuales()
    {
        var clones = TrafficManager.Instance.ObtenerClones();

        var clonesFiltrados = new List<MovementController>();
        float targetY = spawnerTemplate.transform.position.y;
        float targetZ = spawnerTemplate.transform.position.z;

        foreach (var mc in clones)
        {
            if (mc == null) continue;
            Vector3 pos = mc.transform.position;
            if (Mathf.Abs(pos.y - targetY) < 10f && Mathf.Abs(pos.z - targetZ) < 20f)
                clonesFiltrados.Add(mc);
        }

        clonesFiltrados.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        if (patternManagers == null || patternManagers.Length == 0) return;

        int totalCars = clonesFiltrados.Count;
        int zonas = patternManagers.Length;

        for (int z = 0; z < zonas; z++)
        {
            if (patternManagers[z] == null) continue;
            patternManagers[z].LimpiarAutos();

            int startIdx = (totalCars * z) / zonas;
            int endIdx = (z == zonas - 1) ? totalCars : (totalCars * (z + 1)) / zonas;

            for (int i = startIdx; i < endIdx; i++)
            {
                var interactible = clonesFiltrados[i].GetComponent<TrafficCar_Interactible>();
                if (interactible == null)
                    interactible = clonesFiltrados[i].gameObject.AddComponent<TrafficCar_Interactible>();
                patternManagers[z].RegisterCar(interactible);
            }

            Debug.Log($"[BridgeControl] Zona virtual {z + 1}: {endIdx - startIdx} autos asignados.");
        }
    }

    public void CompleteCurrentZone()
    {
        if (IsComplete || !IsActive) return;

        Debug.Log($"[BridgeControl] Zona {CurrentZoneIndex + 1} completada.");

        CurrentZoneIndex++;

        if (CurrentZoneIndex < patternManagers.Length)
        {
            if (patternManagers[CurrentZoneIndex] != null)
                patternManagers[CurrentZoneIndex].ActivateZone();
        }
        else
        {
            CompleteChallenge();
        }
    }

    private void CompleteChallenge()
    {
        IsComplete = true;
        IsActive = false;

        if (TrafficManager.Instance != null)
            TrafficManager.Instance.SetMultiplicadorPorPlantilla(spawnerTemplate, 1f);

        Debug.Log("[BridgeControl] ¡Desafío completado! Tráfico del puente restaurado.");
        OnAllZonesComplete?.Invoke();
    }

    public void Reiniciar()
    {
        if (TrafficManager.Instance != null)
            TrafficManager.Instance.SetMultiplicadorPorPlantilla(spawnerTemplate, 1f);

        CurrentZoneIndex = 0;
        IsComplete = false;
        IsActive = false;

        foreach (var pm in patternManagers)
        {
            if (pm != null)
                pm.ResetZone();
        }

        Debug.Log("[BridgeControl] Reiniciado.");
    }
}

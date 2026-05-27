using UnityEngine;
using System.Collections.Generic;

public static class Read_Json
{
    public static List<string> GetUniqueCategories()
    {
        HashSet<string> categoriasUnicas =
            new HashSet<string>();

        string json = DriveDataLoader.ReadLocalJson();

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[CategoryDetector] JSON vacío.");
            return new List<string>();
        }

        BotonDataList lista;

        try
        {
            lista =
                JsonUtility.FromJson<BotonDataList>(json);
        }
        catch
        {
            Debug.LogError("[Read_Json] Error parseando JSON.");
            return new List<string>();
        }

        if (lista == null || lista.botones == null)
        {
            Debug.LogError("[Read_Json] Lista o botones null.");
            return new List<string>();
        }

        foreach (BotonData boton in lista.botones)
        {
            if (string.IsNullOrWhiteSpace(boton.categoria))
                continue;

            categoriasUnicas.Add(
                boton.categoria.Trim()
            );
        }

        return new List<string>(categoriasUnicas);
    }

}

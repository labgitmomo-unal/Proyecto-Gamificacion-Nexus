using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class UpgradeTrafficLights
{
    [MenuItem("Tools/Upgrade Tarbo TrafficLights to URP")]
    static void Upgrade()
    {
        string folder = "Assets/Tarbo-CITY-TrafficLights";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogError($"Carpeta no encontrada: {folder}");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
        var paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();

        if (paths.Length == 0)
        {
            Debug.Log("No se encontraron materiales en Tarbo-CITY-TrafficLights.");
            return;
        }

        int count = 0;
        foreach (var path in paths)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if (mat.shader != null && mat.shader.name.Contains("Universal Render Pipeline"))
                continue;

            UpgradeMaterial(mat);
            EditorUtility.SetDirty(mat);
            count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Convertidos {count} materiales de {folder}");
    }

    static void UpgradeMaterial(Material mat)
    {
        var oldShader = mat.shader;
        bool isTransparent = oldShader != null &&
            (oldShader.name.Contains("Transparent") || mat.HasProperty("_Mode") && mat.GetFloat("_Mode") == 2f);

        string newShader = isTransparent
            ? "Universal Render Pipeline/Lit"
            : "Universal Render Pipeline/Lit";

        if (oldShader != null)
        {
            CopyProperty(mat, "_Color", "_BaseColor");
            CopyProperty(mat, "_MainTex", "_BaseMap");
            CopyProperty(mat, "_Glossiness", "_Smoothness");
            CopyProperty(mat, "_Metallic", "_Metallic");
            CopyProperty(mat, "_BumpMap", "_BumpMap");
            CopyProperty(mat, "_NormalMap", "_BumpMap");
            CopyProperty(mat, "_EmissionMap", "_EmissionMap");
            CopyProperty(mat, "_EmissionColor", "_EmissionColor");
            CopyProperty(mat, "_Cutoff", "_Cutoff");
        }

        mat.shader = Shader.Find(newShader);
        if (mat.shader == null)
            mat.shader = Shader.Find("Universal Render Pipeline/Lit");

        if (isTransparent)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_Cull", 2f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }

    static void CopyProperty(Material mat, string oldName, string newName)
    {
        if (!mat.HasProperty(oldName)) return;

        var type = ShaderUtil.GetPropertyType(mat.shader, ShaderUtil.GetPropertyCount(mat.shader) - 1);
        // Try to get the property type from the old shader
        if (mat.GetTexture(oldName) != null)
            mat.SetTexture(newName, mat.GetTexture(oldName));
        else if (mat.GetColor(oldName) != Color.clear)
            mat.SetColor(newName, mat.GetColor(oldName));
        else if (mat.GetFloat(oldName) != 0f)
            mat.SetFloat(newName, mat.GetFloat(oldName));
    }
}

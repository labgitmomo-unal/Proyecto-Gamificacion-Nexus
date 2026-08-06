using UnityEngine;
using UnityEditor;

public static class ConvertMaterialsToURP
{
    [MenuItem("Tools/Convert Materials to URP")]
    public static void Convert()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;
            if (mat.shader != null && mat.shader.name == "Standard")
            {
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit != null)
                {
                    mat.shader = urpLit;
                    count++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Conversión URP", $"Se convirtieron {count} materiales a URP/Lit.", "OK");
        Debug.Log($"[ConvertMaterialsToURP] {count} materiales convertidos a URP/Lit.");
    }

}

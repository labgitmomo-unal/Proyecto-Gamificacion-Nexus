using UnityEngine;
using UnityEditor;

public class FixMirrorMeshesPostprocessor : AssetPostprocessor
{
    void OnPostprocessModel(GameObject go)
    {
        if (assetPath != "Assets/Prefabs/Nono.fbx") return;

        string[] mirrorMeshes = { "Sphere.011_mir", "Cylinder.010_mir" };
        bool changed = false;

        foreach (string meshName in mirrorMeshes)
        {
            Transform t = FindChild(go.transform, meshName);
            if (t == null) continue;

            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Mesh mesh = mf.sharedMesh;
            Vector3[] normals = mesh.normals;
            if (normals.Length == 0) continue;

            for (int i = 0; i < normals.Length; i++)
                normals[i] = -normals[i];

            mesh.normals = normals;
            changed = true;
        }

        if (changed)
            Debug.Log($"[FixMirrorMeshes] Fixed normals on mirror meshes in {assetPath}");
    }

    Transform FindChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChild(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}

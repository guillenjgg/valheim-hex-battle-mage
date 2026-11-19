using System;
using System.IO;
using System.Linq;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ConnectedTunicAssetBuilder
{
    private const string Folder = "Assets/_CustomItems/HexBattleMage/Armor/Meadows/Chest_Meadows_01";
    private const string References = "Assets/Editor/UtilityClearanceReference";

    [Serializable]
    private sealed class Surface
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uv;
        public MeadowsCloak01AssetBuilder.Influence[] influences;
    }

    [MenuItem("HexBattleMage/Armor/Import Connected Starter Tunic")]
    public static void Build()
    {
        foreach (string gender in new[] { "Male", "Female" })
        {
            var surface = JsonUtility.FromJson<Surface>(File.ReadAllText(References + "/ConnectedTunic_" + gender + ".json"));
            var target = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/HexBattleMage_Chest_Meadows_01_" + gender + "_Cloth.asset");
            var mesh = new Mesh { name = target.name };

            mesh.vertices = surface.vertices;
            mesh.triangles = surface.triangles;
            mesh.uv = surface.uv;
            mesh.bindposes = target.bindposes;
            mesh.boneWeights = surface.influences.Select(influence => new BoneWeight { boneIndex0 = influence.indices[0], weight0 = influence.weights[0], boneIndex1 = influence.indices[1], weight1 = influence.weights[1], boneIndex2 = influence.indices[2], weight2 = influence.weights[2], boneIndex3 = influence.indices[3], weight3 = influence.weights[3] }).ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            EditorUtility.CopySerialized(mesh, target);
            EditorUtility.SetDirty(target);
            Object.DestroyImmediate(mesh);
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/HexBattleMage_Chest_Meadows_01_Definition.prefab");
        var profile = prefab.GetComponent<HexBattleMageArmorProfile>();
        profile.UtilityVisualPadding = 0.025f;
        EditorUtility.SetDirty(profile);
        PrefabUtility.SavePrefabAsset(prefab);
        var source = profile.MaleVisual.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
        var drop = profile.WorldModel.GetComponent<MeshFilter>().sharedMesh;
        var copy = Object.Instantiate(source);

        copy.vertices = copy.vertices.Select(point => (point - source.bounds.center) * 0.25f).ToArray();
        copy.name = drop.name;
        copy.RecalculateBounds();
        EditorUtility.CopySerialized(copy, drop);
        EditorUtility.SetDirty(drop);
        Object.DestroyImmediate(copy);
        AssetDatabase.SaveAssets();

        foreach (string icon in Directory.GetFiles(Folder + "/Icons", "WorldDrop_v2_*.png"))
        {
            File.Delete(icon);
        }

        TunicFastenerAssetPreparation.Prepare();
        GearIconAssetBuilder.Build();

    }
}

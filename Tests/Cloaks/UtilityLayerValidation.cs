using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class UtilityLayerValidation
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const float Padding = 0.025f;

    public static void Run()
    {
        var mod = typeof(HexBattleMageArmorProfile).Assembly;
        var layerType = mod.GetType("HexBattleMage.Patches.UtilityLayerVisual", true);
        var patchType = mod.GetType("HexBattleMage.Patches.UtilityVisualPatches", true);
        var harmony = new Harmony("HexBattleMage.UtilityLayerValidation");
        var root = new GameObject("Native utility reference");
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE"));

        try
        {
            harmony.CreateClassProcessor(patchType).Patch();
            var definition = bundle.LoadAsset<GameObject>("HexBattleMage_Chest_Meadows_01_Definition");

            Check(Mathf.Approximately(definition.GetComponent<HexBattleMageArmorProfile>().UtilityVisualPadding, Padding), "Utility padding is baked into the tunic prefab.");
            root.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            root.transform.localScale = Vector3.one * 100f;
            var renderer = root.AddComponent<SkinnedMeshRenderer>();
            var source = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Validation/UtilityBelt.asset");
            var originalVertices = source.vertices;

            renderer.sharedMesh = source;
            var layer = root.AddComponent(layerType);
            var configure = layerType.GetMethod("Configure", Members);

            configure.Invoke(layer, new object[] { renderer, Padding, Vector3.up });
            var first = renderer.sharedMesh;

            Check(first != source, "Only an instance mesh is modified.");
            Check(source.vertices.SequenceEqual(originalVertices), "Native shared vertices remain unchanged.");
            Check(first.bindposes.SequenceEqual(source.bindposes) && first.boneWeights.SequenceEqual(source.boneWeights), "Native skinning is preserved.");
            ValidateOffsets(root.transform, originalVertices, first.vertices, Padding);
            configure.Invoke(layer, new object[] { renderer, Padding, Vector3.up });
            Check(renderer.sharedMesh == first, "Repeated visual updates reuse the mesh.");
            configure.Invoke(layer, new object[] { renderer, Padding * 0.5f, Vector3.up });
            ValidateOffsets(root.transform, originalVertices, renderer.sharedMesh.vertices, Padding * 0.5f);
            configure.Invoke(layer, new object[] { renderer, 0f, Vector3.up });
            Check(renderer.sharedMesh == source, "Removing the armor restores the original utility mesh.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "utility-layer-report.txt"), "PASS: authored tunic padding; installed native hook signature; native belt reference expands radially by the configured world-space amount; source mesh, bind poses and weights unchanged; no cumulative expansion; original restored at zero padding. Live animated equipment remains an in-game check.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            bundle.Unload(true);
            harmony.UnpatchSelf();
        }
    }

    private static void ValidateOffsets(Transform transform, Vector3[] source, Vector3[] expanded, float expected)
    {
        for (int index = 0; index < source.Length; index++)
        {
            Vector3 offset = transform.TransformVector(expanded[index] - source[index]);

            Check(Mathf.Abs(offset.magnitude - expected) < 0.00001f, "Configured radial offset is applied.");
            Check(Mathf.Abs(Vector3.Dot(offset, Vector3.up)) < 0.00001f, "Utility height is unchanged.");
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

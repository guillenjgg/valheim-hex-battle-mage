using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HexBattleMage.Core.Managers;
using MagicaCloth2;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ReimportNarrowWrap
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01";
    private const string Identity = "HexBattleMage_Cloak_Meadows_01";

    public static void Run()
    {

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var reference = JsonUtility.FromJson<MeadowsCloak01AssetBuilder.RigReference>(File.ReadAllText("Assets/Editor/HexBattleMage/CloakReference/NativeCapeRigReference.json"));
        var create = typeof(MeadowsCloak01AssetBuilder).GetMethod("CreateVisual", BindingFlags.Static | BindingFlags.NonPublic);
        var clothMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/MeadowsCloth.mat");
        var claspMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/MeadowsClasp.mat");

        foreach (string gender in new[] { "Male", "Female" })
        {
            string path = Root + "/Visuals/" + Identity + "_" + gender + ".prefab";
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var cloth = visual.GetComponentInChildren<MagicaCloth>(true);
            var mesh = cloth.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            var original = Object.Instantiate(AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Editor/NarrowWrapOriginal/" + Identity + "_" + gender + "_Cloth.asset"));
            original.name = Identity + "_" + gender + "_Cloth";
            var vertices = original.vertices;
            var uv = original.uv;
            var weights = original.boneWeights;
            string temporary = Root + "/Visuals/CollarReimport_" + gender + ".prefab";

            try
            {
                create.Invoke(null, new object[] { gender, reference, clothMaterial, claspMaterial, temporary });
                var importedVertices = mesh.vertices;
                var importedUv = mesh.uv;
                var groups = Enumerable.Range(0, importedUv.Length).GroupBy(index => UvKey(importedUv[index])).ToDictionary(group => group.Key, group => group.ToArray());
                var updated = new Vector3[vertices.Length];

                for (int index = 0; index < vertices.Length; index++)
                {
                    if (!groups.TryGetValue(UvKey(uv[index]), out var matches))
                    {
                        throw new InvalidOperationException("Reimport lost a source UV coordinate.");
                    }

                    updated[index] = importedVertices[matches.OrderBy(candidate => (importedVertices[candidate] - vertices[index]).sqrMagnitude).First()];
                }

                original.vertices = updated;
                original.RecalculateNormals();
                original.RecalculateTangents();
                original.RecalculateBounds();
                EditorUtility.CopySerialized(original, mesh);
                EditorUtility.SetDirty(mesh);
                Object.DestroyImmediate(original);

                for (int index = 0; index < vertices.Length; index++)
                {
                    if (vertices[index].y <= 1.20f && Vector3.Distance(vertices[index], updated[index]) > 0.001f)
                    {
                        throw new InvalidOperationException("Front-wrap fit changed lower drape: " + gender);
                    }
                }

                Debug.Log("PASS: " + gender + " collar mesh reimport preserves topology, UV order, skin weights and lower drape.");
            }
            finally
            {
                AssetDatabase.DeleteAsset(temporary);
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_CustomItems/HexBattleMage/Cloaks" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (source.transform.Find("attach_skin") == null)
            {
                continue;
            }

            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var profile = root.GetComponentInChildren<HexBattleMageCloakCollisionProfile>(true);

                foreach (var cloth in root.GetComponentsInChildren<MagicaCloth>(true))
                {
                    CloakMotionSettings.Apply(profile, cloth.SerializeData);
                    var renderer = cloth.GetComponent<SkinnedMeshRenderer>();
                    var vertices = renderer.sharedMesh.vertices;
                    var data = cloth.GetSerializeData2();

                    if (data.selectionData.positions.Length != vertices.Length)
                    {
                        throw new InvalidOperationException("Paint count mismatch: " + path);
                    }

                    for (int index = 0; index < vertices.Length; index++)
                    {
                        data.selectionData.positions[index] = vertices[index];
                    }

                    using (var setup = new RenderSetupData(null, renderer))
                    {
                        bool weighted = setup.hasBoneWeight;

                        setup.hasBoneWeight = false;

                        if (!data.initData.Serialize(cloth.SerializeData, new TransformRecord(cloth.transform, true), new TransformRecord(cloth.transform, true), new List<RenderSetupData> { setup }))
                        {
                            throw new InvalidOperationException("Collar cloth initialization failed: " + path);
                        }

                        setup.hasBoneWeight = weighted;
                        data.initData.clothSetupDataList[0].hasBoneWeight = weighted;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        var host = new GameObject("Inactive collar bake");

        host.SetActive(false);
        var male = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Visuals/" + Identity + "_Male.prefab");
        var instance = Object.Instantiate(male, host.transform, false);

        try
        {
            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = new Mesh { name = Identity + "_Drop_" + renderer.name };

                renderer.BakeMesh(mesh);
                mesh.vertices = mesh.vertices.Select(position => (position - Vector3.up * 1.05f) * 0.3f).ToArray();
                mesh.RecalculateBounds();
                var target = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Meshes/" + mesh.name + ".asset");

                EditorUtility.CopySerialized(mesh, target);
                EditorUtility.SetDirty(target);
                Object.DestroyImmediate(mesh);
            }
        }
        finally
        {
            Object.DestroyImmediate(host);
        }

        AssetDatabase.SaveAssets();
        MeadowsCloak01AssetBuilder.Validate();
        HexBattleMageBundlePolicy.ValidateDependencies();
        var manifest = BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

        if (manifest == null)
        {
            throw new InvalidOperationException("Collar mesh bundle build failed.");
        }

        File.Copy("Assets/AssetBundles/hexbattlemage", "C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/hexbattlemage", true);
    }
    private static string UvKey(Vector2 uv)
    {
        return Mathf.RoundToInt(uv.x * 100000f) + ":" + Mathf.RoundToInt(uv.y * 100000f);
    }

}


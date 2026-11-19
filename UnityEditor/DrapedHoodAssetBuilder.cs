using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class DrapedHoodAssetBuilder
{
    private const string SharedRoot = "Assets/_CustomItems/HexBattleMage/Armor/Shared/Hoods/PlainClothHood";
    private const string ItemRoot = "Assets/_CustomItems/HexBattleMage/Armor/Meadows/Helmet_Meadows_01";
    private const string Identity = "HexBattleMage_Helmet_Meadows_01";
    private const string ReferenceRoot = "Assets/Editor/HexBattleMage/CloakReference";
    private const int Segments = 40;
    private const float HemWidth = 0.003f;

    [Serializable]
    private sealed class SourceMesh
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uv;
        public BoneWeight[] weights;
        public string[] bones;
    }

    [MenuItem("HexBattleMage/Armor/Apply Draped Cloth Hood and Build Bundle")]
    public static void Build()
    {
        Directory.CreateDirectory(SharedRoot);
        AssetDatabase.Refresh();

        var reference = JsonUtility.FromJson<MeadowsCloak01AssetBuilder.RigReference>(File.ReadAllText(ReferenceRoot + "/NativeCapeRigReference.json"));
        var material = PrepareMaterial();
        string definitionPath = ItemRoot + "/" + Identity + "_Definition.prefab";
        var root = PrefabUtility.LoadPrefabContents(definitionPath);

        try
        {
            var profile = root.GetComponent<HexBattleMageArmorProfile>();

            foreach (string gender in new[] { "Male", "Female" })
            {
                string sharedPath = SharedRoot + "/PlainClothHood_" + gender + ".prefab";

                if (AssetDatabase.LoadAssetAtPath<GameObject>(sharedPath) == null)
                {
                    CreateVisual(reference, material, gender, sharedPath);
                }

                string variantPath = ItemRoot + "/" + Identity + "_Draped_" + gender + ".prefab";

                if (AssetDatabase.LoadAssetAtPath<GameObject>(variantPath) == null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(sharedPath));

                    PrefabUtility.SaveAsPrefabAsset(instance, variantPath);
                    Object.DestroyImmediate(instance);
                }

                if (gender == "Male")
                {
                    profile.MaleVisual = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
                }
                else
                {
                    profile.FemaleVisual = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
                }
            }

            profile.WorldModel = MeadowsClothingVisualBuilder.PrepareDrop(profile.MaleVisual, ItemRoot, Identity + "_Draped", material);

            string iconFolder = ItemRoot + "/Draped";

            Directory.CreateDirectory(iconFolder);
            AssetDatabase.Refresh();
            profile.Icon = MeadowsClothingVisualBuilder.PrepareIcon(profile.WorldModel, iconFolder);
            profile.ReplaceNativeVisuals = true;

            var styles = root.GetComponent<MageArmorStyleProfile>();

            styles.Styles[0].Material = material;
            styles.Styles[0].Icon = profile.Icon;
            ArmorStyleAssetPreparation.Prepare(root, definitionPath);
            PrefabUtility.SaveAsPrefabAsset(root, definitionPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        MeadowsClothingVisualBuilder.CreatePreview();
        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }

    private static Material PrepareMaterial()
    {
        string path = SharedRoot + "/WornLinen.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (existing != null)
        {
            return existing;
        }

        var material = new Material(AssetDatabase.LoadAssetAtPath<Material>(ItemRoot + "/PlainCloth.mat"));

        material.name = "PlainClothHood_WornLinen";
        material.color = new Color(0.74f, 0.71f, 0.65f);
        material.SetColor("_EmissionColor", Color.black);
        material.SetFloat("_Glossiness", 0f);
        material.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void CreateVisual(MeadowsCloak01AssetBuilder.RigReference reference, Material material, string gender, string path)
    {
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var weights = new List<BoneWeight>();
        var triangles = new List<int>();
        var heights = new[] { 1.51f, 1.55f, 1.60f, 1.66f, 1.72f, 1.78f, 1.83f, 1.87f, 1.90f, 1.925f, 1.94f };
        var widths = new[] { 0.285f, 0.245f, 0.190f, 0.150f, 0.151f, 0.156f, 0.162f, 0.155f, 0.137f, 0.100f, 0.045f };
        var depths = new[] { 0.285f, 0.260f, 0.225f, 0.20f, 0.18f, 0.185f, 0.190f, 0.180f, 0.155f, 0.120f, 0.065f };
        var openings = new[] { 0.68f, 0.73f, 0.80f, 0.81f, 0.80f, 0.78f, 0.74f, 0.65f, 0.40f, 0f, 0f };
        int head = Array.FindIndex(reference.bones, bone => bone.name == "Head");
        int neck = Array.FindIndex(reference.bones, bone => bone.name == "Neck");
        int chest = Array.FindIndex(reference.bones, bone => bone.name == "Spine2");
        float size = gender == "Female" ? 0.97f : 1f;

        for (int row = 0; row < heights.Length; row++)
        {
            float height = heights[row];
            float headWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.56f, 1.72f, height));
            float chestWeight = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.51f, 1.60f, height));

            for (int column = 0; column <= Segments; column++)
            {
                float u = column / (float)Segments;
                float angle = Mathf.Lerp(openings[row], Mathf.PI * 2f - openings[row], u);
                float drape = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.58f, 1.84f, height));
                float sideFold = Mathf.Sin(angle * 5f + row * 0.30f) * 0.026f + Mathf.Sin(angle * 3f - row * 0.42f) * 0.015f;
                float fold = sideFold * drape * Mathf.Sin(angle) * Mathf.Sin(angle);
                float edgeSlack = Mathf.Pow(Mathf.Abs(2f * u - 1f), 10f) * drape;
                float radiusX = widths[row] * size + fold;
                float radiusZ = depths[row] + fold;
                float y = height + Mathf.Cos(angle * 3f + 0.4f) * 0.009f * drape;
                float z = -0.015f + Mathf.Cos(angle) * radiusZ + edgeSlack * Mathf.Sin(row * 0.9f) * 0.008f;

                vertices.Add(new Vector3(Mathf.Sin(angle) * radiusX, y, z));
                uv.Add(new Vector2(u, (height - heights[0]) / (heights.Last() - heights[0])));
                weights.Add(new BoneWeight { boneIndex0 = head, weight0 = headWeight, boneIndex1 = neck, weight1 = (1f - headWeight) * (1f - chestWeight), boneIndex2 = chest, weight2 = (1f - headWeight) * chestWeight });

                if (row > 0 && column > 0)
                {
                    int current = row * (Segments + 1) + column;

                    triangles.AddRange(new[] { current - Segments - 2, current - Segments - 1, current, current - Segments - 2, current, current - 1 });
                }
            }
        }

        int crown = vertices.Count;

        vertices.Add(new Vector3(0f, 1.947f, -0.015f));
        uv.Add(new Vector2(0.5f, 1f));
        weights.Add(new BoneWeight { boneIndex0 = head, weight0 = 1f });

        for (int column = 0; column < Segments; column++)
        {
            int edge = (heights.Length - 1) * (Segments + 1) + column;

            triangles.AddRange(new[] { edge, edge + 1, crown });
        }

        // The face edge is a narrow cloth fold, without a padded tube around the opening.
        foreach (int column in new[] { 0, Segments })
        {
            int start = vertices.Count;

            for (int row = 0; row < heights.Length - 2; row++)
            {
                int edge = row * (Segments + 1) + column;
                Vector3 position = vertices[edge];
                float direction = column == 0 ? 1f : -1f;

                vertices.Add(position + new Vector3(direction * HemWidth, 0f, -HemWidth * 0.5f));
                uv.Add(uv[edge] + Vector2.right * direction * 0.006f);
                weights.Add(weights[edge]);

                if (row > 0)
                {
                    int previous = edge - Segments - 1;
                    int folded = start + row;

                    triangles.AddRange(column == 0 ? new[] { previous, edge, folded, previous, folded, folded - 1 } : new[] { previous, folded, edge, previous, folded - 1, folded });
                }
            }
        }

        var mesh = new Mesh { name = "PlainClothHood_" + gender };

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.boneWeights = weights.ToArray();
        mesh.bindposes = reference.bones.Select(bone => MeadowsCloak01AssetBuilder.Matrix(bone.nativeWorld).inverse).ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, SharedRoot + "/" + mesh.name + ".asset");

        var root = new GameObject("PlainClothHood_" + gender);
        var attach = new GameObject("attach_skin");
        var rig = new GameObject("ReferenceRig");
        var cloth = new GameObject("Cloth");
        var cosmetics = new GameObject("CosmeticDetails");

        attach.transform.SetParent(root.transform, false);
        rig.transform.SetParent(attach.transform, false);
        cloth.transform.SetParent(attach.transform, false);
        cosmetics.transform.SetParent(attach.transform, false);

        var bones = MeadowsCloak01AssetBuilder.CreateRig(reference, rig.transform, false);
        var renderer = cloth.AddComponent<SkinnedMeshRenderer>();

        renderer.sharedMesh = mesh;
        renderer.sharedMaterial = material;
        renderer.bones = bones;
        renderer.rootBone = bones[0];
        renderer.updateWhenOffscreen = true;
        renderer.localBounds = new Bounds(new Vector3(0f, 1.73f, 0f), new Vector3(0.7f, 0.7f, 0.7f));
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        string export = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/PlainClothHood_" + gender + ".json";

        File.WriteAllText(export, JsonUtility.ToJson(new SourceMesh { vertices = mesh.vertices, triangles = mesh.triangles, uv = mesh.uv, weights = mesh.boneWeights, bones = reference.bones.Select(bone => bone.name).ToArray() }, true));
    }
}

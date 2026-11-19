using System;
using System.IO;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ApprovedHoodAssetBuilder
{
    private const string SharedRoot = "Assets/_CustomItems/HexBattleMage/Armor/Shared/Hoods/PlainClothHood";
    private const string ItemRoot = "Assets/_CustomItems/HexBattleMage/Armor/Meadows/Helmet_Meadows_01";
    private const string Identity = "HexBattleMage_Helmet_Meadows_01";
    private const string ReviewRoot = "Assets/Editor/HoodReview/RecessedFaceHood14";

    [MenuItem("HexBattleMage/Armor/Apply Approved Face-Fitted Hood and Build Bundle")]
    public static void Build()
    {
        var palette = PreparePalette();
        foreach (string gender in new[] { "Male", "Female" })
        {
            string meshPath = SharedRoot + "/HexBattleMage_Hood_Cloth_RecessedFace_" + gender + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

            if (mesh == null)
            {
                var approved = AssetDatabase.LoadAssetAtPath<Mesh>(ReviewRoot + "/RecessedFaceHood14_" + gender + ".asset");

                if (approved == null)
                {
                    throw new InvalidOperationException("Missing approved hood mesh for " + gender);
                }

                mesh = Object.Instantiate(approved);
                mesh.name = "HexBattleMage_Hood_Cloth_RecessedFace_" + gender;
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            if (mesh.vertexCount == 0 || mesh.bindposes.Length != 53)
            {
                throw new InvalidOperationException("Invalid approved hood geometry or native binding: " + gender);
            }

            string basePath = SharedRoot + "/PlainClothHood_" + gender + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(basePath);

            try
            {
                var renderer = root.GetComponentInChildren<SkinnedMeshRenderer>(true);

                renderer.sharedMesh = mesh;
                renderer.sharedMaterial = palette[0].Material;
                renderer.localBounds = new Bounds(new Vector3(0f, 1.74f, 0f), new Vector3(0.7f, 0.8f, 0.8f));
                PrefabUtility.SaveAsPrefabAsset(root, basePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        string definitionPath = ItemRoot + "/" + Identity + "_Definition.prefab";
        var definition = PrefabUtility.LoadPrefabContents(definitionPath);

        try
        {
            var profile = definition.GetComponent<HexBattleMageArmorProfile>();
            var material = palette[0].Material;

            profile.WorldModel = MeadowsClothingVisualBuilder.PrepareDrop(profile.MaleVisual, ItemRoot, Identity + "_ApprovedRecessedFace", material);
            profile.Icon = palette[0].Icon;

            var styles = definition.GetComponent<MageArmorStyleProfile>();

            if (styles.Styles.Length <= 1)
            {
                styles.Styles = palette;
            }
            ArmorStyleAssetPreparation.Prepare(definition, definitionPath);
            PrefabUtility.SaveAsPrefabAsset(definition, definitionPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(definition);
        }

        AssetDatabase.SaveAssets();
        MeadowsClothingVisualBuilder.CreatePreview();
        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }
    private static MageArmorStyle[] PreparePalette()
    {
        string folder = SharedRoot + "/Styles";
        var names = new[] { "LightBrown", "BlackGrey", "Grey", "Brown" };
        var tokens = new[] { "$hexbattlemage_hood_style_light_brown", "$hexbattlemage_hood_style_black_grey", "$hexbattlemage_hood_style_grey", "$hexbattlemage_hood_style_brown" };
        var styles = new MageArmorStyle[names.Length];

        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();

        for (int index = 0; index < names.Length; index++)
        {
            string materialPath = folder + "/" + names[index] + ".mat";
            string iconPath = folder + "/Icon_RecessedFace_" + names[index] + ".png";

            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
            {
                if (!AssetDatabase.CopyAsset(ReviewRoot + "/Style_" + names[index] + ".mat", materialPath))
                {
                    throw new InvalidOperationException("Missing authored hood material: " + names[index]);
                }
            }

            if (AssetDatabase.LoadAssetAtPath<Sprite>(iconPath) == null)
            {
                if (!AssetDatabase.CopyAsset(ReviewRoot + "/Icon_" + names[index] + ".png", iconPath))
                {
                    throw new InvalidOperationException("Missing authored hood icon: " + names[index]);
                }
            }

            styles[index] = new MageArmorStyle { DisplayName = tokens[index], Material = AssetDatabase.LoadAssetAtPath<Material>(materialPath), Icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath) };
        }

        return styles;
    }

}

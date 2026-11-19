using System;
using System.IO;
using System.Linq;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CloakTextureTestBuilder
{
    private const string Source = "Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01";
    private const string Destination = "Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01_TextureTests";
    private const string Identity = "HexBattleMage_Cloak_Meadows_01";
    private const string BundleName = "hexbattlemage";

    [MenuItem("HexBattleMage/Cloak/Prepare Texture Tests and Build Bundle")]
    public static void Build()
    {
        Prepare(1);
        Prepare(2);
        AssetDatabase.SaveAssets();
        Validate();
        HexBattleMageBundlePolicy.ValidateDependencies();
        Directory.CreateDirectory("Assets/AssetBundles");
        var manifest = BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

        if (manifest == null)
        {
            throw new InvalidOperationException("Cloak texture test bundle build failed.");
        }

        File.Copy("Assets/AssetBundles/" + BundleName, "C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/" + BundleName, true);
    }

    private static void Prepare(int index)
    {
        string suffix = "Test" + index.ToString("00");
        string folder = Destination + "/" + suffix;
        string identity = Identity + "_" + suffix;
        string rootPath = folder + "/" + identity + ".prefab";

        if (File.Exists(rootPath))
        {
            return;
        }

        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        string texturePath = folder + "/BaseColor.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.maxTextureSize = 256;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.SaveAndReimport();
        var material = new Material(AssetDatabase.LoadAssetAtPath<Material>(Source + "/Materials/MeadowsCloth.mat"));

        material.name = identity + "_Cloth";
        material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        material.SetFloat("_Glossiness", 0f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_BumpScale", 0.1f);
        AssetDatabase.CreateAsset(material, folder + "/Cloth.mat");
        var male = CopyVisual(Source + "/Visuals/" + Identity + "_Male.prefab", folder + "/" + identity + "_Male.prefab", material);
        var female = CopyVisual(Source + "/Visuals/" + Identity + "_Female.prefab", folder + "/" + identity + "_Female.prefab", material);
        var drop = CopyVisual(Source + "/" + Identity + "_Drop.prefab", folder + "/" + identity + "_Drop.prefab", material);

        AssetDatabase.CopyAsset(Source + "/" + Identity + ".prefab", rootPath);
        var root = PrefabUtility.LoadPrefabContents(rootPath);

        try
        {
            root.name = identity;
            var profile = root.GetComponent<HexBattleMageCloakProfile>();

            profile.ItemPrefabName = identity;
            profile.DisplayName = "$item_hexbattlemage_cloak_meadows_01_" + suffix.ToLowerInvariant();
            profile.Description = "$item_hexbattlemage_cloak_meadows_01_texturetest_description";
            profile.MaleVisual = male;
            profile.FemaleVisual = female;
            profile.WorldModel = drop;

            for (int childIndex = root.transform.childCount - 1; childIndex >= 0; childIndex--)
            {
                Object.DestroyImmediate(root.transform.GetChild(childIndex).gameObject);
            }

            var malePreview = (GameObject)PrefabUtility.InstantiatePrefab(male, root.transform);
            var femalePreview = (GameObject)PrefabUtility.InstantiatePrefab(female, root.transform);

            malePreview.name = "Male";
            femalePreview.name = "Female";
            femalePreview.SetActive(false);
            var recipe = root.GetComponent<MageRecipeProfile>();

            recipe.Resources = new MageRecipeResource[0];
            string recipePath = folder + "/Recipe.json";

            File.WriteAllText(recipePath, recipe.GetBakedData());
            AssetDatabase.ImportAsset(recipePath);
            recipe.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(recipePath);
            PrefabUtility.SaveAsPrefabAsset(root, rootPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetImporter.GetAtPath(rootPath).SetAssetBundleNameAndVariant(BundleName, "");
    }

    private static GameObject CopyVisual(string source, string destination, Material material)
    {
        AssetDatabase.CopyAsset(source, destination);
        var root = PrefabUtility.LoadPrefabContents(destination);

        try
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;

                for (int index = 0; index < materials.Length; index++)
                {
                    if (materials[index] != null && materials[index].name == "MeadowsCloth")
                    {
                        materials[index] = material;
                    }
                }

                renderer.sharedMaterials = materials;
            }

            PrefabUtility.SaveAsPrefabAsset(root, destination);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(destination);
    }

    public static void Validate()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Destination }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var profile = root.GetComponent<HexBattleMageCloakProfile>();

            if (profile == null)
            {
                continue;
            }

            foreach (var fit in new[] { profile.MaleVisual, profile.FemaleVisual })
            {
                if (fit.transform.Find("attach_skin") == null)
                {
                    throw new InvalidOperationException("Missing direct attach_skin: " + fit.name);
                }

                foreach (var skin in fit.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (skin.rootBone == null || skin.bones.Length != 53 || skin.sharedMesh.bindposes.Length != 53 || skin.bones.Any(bone => bone == null))
                    {
                        throw new InvalidOperationException("Invalid native skin mapping: " + skin.name);
                    }
                }

                if (!fit.GetComponentsInChildren<Renderer>(true).Any(renderer => renderer.sharedMaterials.Any(material => material != null && AssetDatabase.GetAssetPath(material) == Path.GetDirectoryName(path).Replace('\\', '/') + "/Cloth.mat")))
                {
                    throw new InvalidOperationException("Test cloth material not assigned: " + fit.name);
                }
            }

            if (profile.Armor != 2f || root.GetComponent<MageEitrProfile>().MaximumEitr != 5f || root.GetComponent<MageRecipeProfile>().GetResources().Length != 0)
            {
                throw new InvalidOperationException("Unexpected test stats or recipe: " + profile.ItemPrefabName);
            }
        }

        Debug.Log("PASS: both copied cloak texture tests retain male/female attach_skin, 53 valid bones and bindposes, own materials, armor 2, Eitr 5, spawn-only recipes.");
    }
}


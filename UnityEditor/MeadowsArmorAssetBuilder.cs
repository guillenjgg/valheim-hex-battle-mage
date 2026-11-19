using System;
using System.IO;
using System.Text.RegularExpressions;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MeadowsArmorAssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/Armor/Meadows";
    private const string ReferenceRoot = "C:/Users/Jonathan/Documents/valheim_mods/Unity/ValheimRIP/Assets";
    private const string CloakRoot = "Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01";
    private const string BundleName = "hexbattlemage";

    [MenuItem("HexBattleMage/Armor/Prepare Meadows Armor and Build Bundle")]
    public static void Build()
    {
        Prepare();
        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }

    public static void Prepare()
    {
        PrepareItem("Helmet", "HelmetFenring", "HelmetFenris", MageEquipmentCategory.Helmet, MageEquipmentSlot.Helmet);
        PrepareItem("Chest", "ArmorLeatherChest", "ArmorLeatherChest", MageEquipmentCategory.Chest, MageEquipmentSlot.Chest);
        PrepareItem("Legs", "ArmorLeatherLegs", "ArmorLeatherLegs", MageEquipmentCategory.Legs, MageEquipmentSlot.Legs);
        AssetDatabase.SaveAssets();
    }

    private static void PrepareItem(string category, string baseline, string nativeIcon, MageEquipmentCategory equipmentCategory, MageEquipmentSlot slot)
    {
        string identity = "HexBattleMage_" + category + "_Meadows_01";
        string folder = Root + "/" + category + "_Meadows_01";
        string path = folder + "/" + identity + "_Definition.prefab";

        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var root = existing != null ? PrefabUtility.LoadPrefabContents(path) : new GameObject(identity + "_Definition");

        try
        {
            var profile = root.GetComponent<HexBattleMageArmorProfile>();

            if (profile == null)
            {
                profile = root.AddComponent<HexBattleMageArmorProfile>();
                profile.ItemPrefabName = identity;
                profile.BaseItemPrefab = baseline;
                profile.DisplayName = "$item_hexbattlemage_" + category.ToLowerInvariant() + "_meadows_01";
                profile.Description = profile.DisplayName + "_description";
                profile.Icon = PrepareIcon(folder, nativeIcon, category == "Chest");

                if (category == "Chest")
                {
                    profile.BodyTexture = PrepareChestTexture(folder);
                    profile.BodyTextureProperty = "_ChestTex";
                    profile.ReplaceMeshTexture = true;
                }
            }

            if (root.GetComponent<MageEquipmentDefinition>() == null)
            {
                var equipment = root.AddComponent<MageEquipmentDefinition>();

                equipment.Category = equipmentCategory;
                equipment.Slot = slot;
                equipment.ProgressionTier = "Meadows";
            }

            if (root.GetComponent<MageEitrProfile>() == null)
            {
                var eitr = root.AddComponent<MageEitrProfile>();

                eitr.MaximumEitr = 5f;
                eitr.RegenerationModifier = 0f;
            }

            var recipe = root.GetComponent<MageRecipeProfile>();

            if (recipe == null)
            {
                recipe = root.AddComponent<MageRecipeProfile>();
                recipe.Resources = new[] { new MageRecipeResource { ItemPrefab = "DeerHide", Amount = 6 }, new MageRecipeResource { ItemPrefab = "Resin", Amount = 2 } };
            }

            if (root.GetComponent<MageArmorStyleProfile>() == null)
            {
                var styles = root.AddComponent<MageArmorStyleProfile>();
                string materialPath = folder + "/Default.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material == null)
                {
                    var source = AssetDatabase.LoadAssetAtPath<Material>(CloakRoot + "/Styles/White.mat");

                    material = new Material(source);
                    material.color = Color.white;
                    material.mainTexture = profile.BodyTexture;
                    AssetDatabase.CreateAsset(material, materialPath);
                }

                styles.Styles = new[] { new MageArmorStyle { DisplayName = "$hexbattlemage_armor_style_default", Material = material, Icon = profile.Icon } };
            }

            string recipePath = folder + "/Recipe_" + root.name + ".json";

            File.WriteAllText(recipePath, recipe.GetBakedData());
            AssetDatabase.ImportAsset(recipePath);
            recipe.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(recipePath);
            AssetImporter.GetAtPath(recipePath).SetAssetBundleNameAndVariant(BundleName, string.Empty);
            ArmorStyleAssetPreparation.Prepare(root, path);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant(BundleName, string.Empty);
        }
        finally
        {
            if (existing != null)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    private static Texture2D PrepareChestTexture(string folder)
    {
        string path = folder + "/Chest_GreyWhite.png";

        if (!File.Exists(path))
        {
            var native = LoadPng(ReferenceRoot + "/Texture2D/LeatherArmourChest_d.png");
            var fabric = AssetDatabase.LoadAssetAtPath<Texture2D>(CloakRoot + "/Styles/White_Heathered_BaseColor.png");
            var output = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            var colors = new Color[256 * 256];

            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    float u = (x + 0.5f) / 256f;
                    float v = (y + 0.5f) / 256f;
                    Color original = native.GetPixelBilinear(u, v);
                    Color woven = fabric.GetPixelBilinear(u, v);
                    float shading = Mathf.Lerp(0.62f, 1f, original.grayscale);

                    colors[y * 256 + x] = new Color(woven.r * 0.8f * shading, woven.g * 0.8f * shading, woven.b * 0.78f * shading, original.a);
                }
            }

            output.SetPixels(colors);
            File.WriteAllBytes(path, output.EncodeToPNG());
            Object.DestroyImmediate(output);
            Object.DestroyImmediate(native);
        }

        ImportTexture(path, false);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Sprite PrepareIcon(string folder, string name, bool greyWhite)
    {
        string path = folder + "/Icon.png";

        if (!File.Exists(path))
        {
            string sprite = File.ReadAllText(ReferenceRoot + "/Sprite/" + name + ".asset");
            var rect = Regex.Match(sprite, @"m_Rect:\s+serializedVersion: \d+\s+x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)");
            var atlas = LoadPng(ReferenceRoot + "/Texture2D/sactx-0-4096x4096-BC7-IconAtlas-7390861a.png");
            int width = int.Parse(rect.Groups[3].Value);
            int height = int.Parse(rect.Groups[4].Value);
            var pixels = atlas.GetPixels(int.Parse(rect.Groups[1].Value), int.Parse(rect.Groups[2].Value), width, height);

            if (greyWhite)
            {
                for (int index = 0; index < pixels.Length; index++)
                {
                    float value = Mathf.Clamp01(pixels[index].grayscale * 1.8f);

                    pixels[index] = new Color(value * 0.8f, value * 0.8f, value * 0.78f, pixels[index].a);
                }
            }

            var output = new Texture2D(width, height, TextureFormat.RGBA32, false);

            output.SetPixels(pixels);
            File.WriteAllBytes(path, output.EncodeToPNG());
            Object.DestroyImmediate(output);
            Object.DestroyImmediate(atlas);
        }

        ImportTexture(path, true);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Texture2D LoadPng(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!texture.LoadImage(File.ReadAllBytes(path)))
        {
            throw new InvalidOperationException("Cannot read texture: " + path);
        }

        return texture;
    }

    private static void ImportTexture(string path, bool icon)
    {
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);

        importer.maxTextureSize = 256;
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.crunchedCompression = true;
        importer.compressionQuality = 100;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;

        if (icon)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
        }

        importer.SaveAndReimport();
    }
}

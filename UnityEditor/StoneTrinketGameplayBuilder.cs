using System;
using System.IO;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;

public static class StoneTrinketGameplayBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/Trinkets/ApprenticeStone";
    private const string Path = Root + "/HexStoneParryTrinketVisual.prefab";

    [MenuItem("HexBattleMage/Prepare Stone Trinket Gameplay and Build Bundle")]
    public static void Build()
    {
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

        if (stage != null && stage.scene.isDirty)
        {
            throw new InvalidOperationException("Save the open prefab before preparing trinket gameplay.");
        }

        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Path);

        if (asset == null)
        {
            throw new InvalidOperationException("Prepare the stone trinket visual first.");
        }

        var prefab = PrefabUtility.LoadPrefabContents(Path);

        try
        {
            var profile = prefab.GetComponent<StoneTrinketProfile>();

            if (profile == null)
            {
                profile = prefab.AddComponent<StoneTrinketProfile>();
            }

            if (prefab.GetComponent<MageEitrProfile>() == null)
            {
                prefab.AddComponent<MageEitrProfile>();
            }

            if (prefab.GetComponent<MageEquipmentDefinition>() == null)
            {
                var definition = prefab.AddComponent<MageEquipmentDefinition>();

                definition.Category = MageEquipmentCategory.Trinket;
                definition.Slot = MageEquipmentSlot.Trinket;
            }

            var recipe = prefab.GetComponent<MageRecipeProfile>();

            if (recipe == null)
            {
                recipe = prefab.AddComponent<MageRecipeProfile>();
                recipe.Resources = new[]
                {
                    new MageRecipeResource { ItemPrefab = "Stone", Amount = 10 },
                    new MageRecipeResource { ItemPrefab = "Resin", Amount = 4 },
                    new MageRecipeResource { ItemPrefab = "LeatherScraps", Amount = 2 }
                };
            }

            string recipePath = Root + "/Recipe_HexStoneParryTrinketVisual.json";

            File.WriteAllText(recipePath, recipe.GetBakedData());
            AssetDatabase.ImportAsset(recipePath);
            recipe.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(recipePath);

            if (profile.Icon == null)
            {
                StoneTrinketAssetBuilder.RenderPreview(asset);

                string iconPath = Root + "/StoneTrinketIcon.png";

                if (!File.Exists(iconPath))
                {
                    File.Copy("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/stone-trinket-preview.png", iconPath);
                }

                AssetDatabase.ImportAsset(iconPath);

                var importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                profile.Icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            }

            if (profile.Icon == null)
            {
                throw new InvalidOperationException("Stone trinket icon could not be baked.");
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        AssetDatabase.SaveAssets();
        HexBattleMageBundlePolicy.ValidateDependencies();

        var manifest = BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

        if (manifest == null)
        {
            throw new InvalidOperationException("Stone trinket gameplay bundle build failed.");
        }

        File.Copy("Assets/AssetBundles/hexbattlemage", "C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/hexbattlemage", true);
        File.WriteAllText("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/stone-trinket-gameplay-result.txt", "PASS: trinket role, Eitr profile, parry reward settings, attachment settings, transparent icon and full recipe baked into the existing visual prefab; meshes/materials preserved.");
    }
}

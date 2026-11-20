using System;
using System.IO;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Trinkets;
using UnityEditor;
using UnityEngine;

public static class MeadowsTrinket01GameplayBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/Trinkets/Meadows/Trinket_Meadows_01";
    private const string Path = Root + "/HexBattleMage_Trinket_Meadows_01.prefab";

    [MenuItem("HexBattleMage/Prepare Meadows Trinket 01 Gameplay and Build Bundle")]
    public static void Build()
    {
        Prepare();
        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }

    public static void Prepare()
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
            var profile = prefab.GetComponent<HexBattleMageTrinketProfile>();

            if (profile == null)
            {
                profile = prefab.AddComponent<HexBattleMageTrinketProfile>();
                profile.ItemPrefabName = "HexBattleMage_Trinket_Meadows_01";
                profile.DisplayName = "$item_hexbattlemage_trinket_meadows_01";
                profile.Description = "$item_hexbattlemage_trinket_meadows_01_description";
                profile.RewardTrigger = TrinketRewardTrigger.SuccessfulParry;
                profile.WandDamageBonus = 0.25f;
                profile.BossParryDamageBonus = 0.10f;
                profile.BuffDisplayName = "$se_hexbattlemage_trinket_meadows_01";
            }

            if (prefab.GetComponent<MageEitrProfile>() == null)
            {
                prefab.AddComponent<MageEitrProfile>();
            }

            if (profile.PassiveVersion < 1)
            {
                var eitr = prefab.GetComponent<MageEitrProfile>();

                eitr.MaximumEitr = 0f;
                eitr.MaximumEitrPerQuality = 0f;
                eitr.RegenerationModifier = 0.10f;
                profile.StaminaRegenerationModifier = 0.10f;
                profile.PassiveVersion = 1;
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
                recipe.CraftingStation = HexBattleMageRuneBlockProfile.PrefabName;
                recipe.RepairStation = HexBattleMageRuneBlockProfile.PrefabName;
                recipe.Resources = new[]
                {
                    new MageRecipeResource { ItemPrefab = "Stone", Amount = 10 },
                    new MageRecipeResource { ItemPrefab = "TrophyBoar", Amount = 1 },
                    new MageRecipeResource { ItemPrefab = "BoneFragments", Amount = 5 },
                    new MageRecipeResource { ItemPrefab = "LeatherScraps", Amount = 2 }
                };
            }

            string recipePath = Root + "/Recipe_HexBattleMage_Trinket_Meadows_01.json";

            File.WriteAllText(recipePath, recipe.GetBakedData());
            AssetDatabase.ImportAsset(recipePath);
            recipe.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(recipePath);

            if (profile.Icon == null)
            {
                MeadowsTrinket01AssetBuilder.RenderPreview(asset);

                string iconPath = Root + "/HexBattleMage_Trinket_Meadows_01_Icon.png";

                if (!File.Exists(iconPath))
                {
                    File.Copy("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/trinket-meadows-01-preview.png", iconPath);
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

        TrinketAssetPreparation.PrepareAll();
        AssetDatabase.SaveAssets();
    }
}

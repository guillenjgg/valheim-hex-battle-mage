using System;
using System.IO;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class StarterWandAssetBuilder
{
    private const string Folder = "Assets/_CustomItems/HexBattleMage/Wands/Meadows/Wand_Meadows_01";
    private const string WandPath = Folder + "/HexBattleMage_Wand_Meadows_01.prefab";

    [MenuItem("HexBattleMage/Prepare Starter Wand Progression and Build Bundle")]
    public static void Build()
    {
        RuneBlockAssetBuilder.Build();
    }

    public static void Prepare()
    {
        var prefab = PrefabUtility.LoadPrefabContents(WandPath);

        try
        {
            var profile = prefab.GetComponent<HexBattleMageWandProfile>();

            PrepareCharacterAttunements();

            if (profile.StarterProgressionVersion >= 1)
            {
                PrepareStationCrafting(prefab, profile);
                PrepareClubsSkill(prefab, profile);
                PrepareStaminaCost(prefab, profile);
                return;
            }

            var upgrade = CreateSettings("HexBattleMage_Wand_Upgrade_Meadows_01", false);
            var fire = CreateSettings("HexBattleMage_Wand_Attunement_Meadows_01", true);

            profile.UpgradeRecipe = upgrade;
            profile.DevelopmentBypassAttunements = false;
            profile.StarterProgressionVersion = 1;
            var recipe = prefab.GetComponent<MageRecipeProfile>();

            recipe.CraftingStation = string.Empty;
            recipe.RepairStation = HexBattleMageRuneBlockProfile.PrefabName;
            recipe.Resources = new[]
            {
                new MageRecipeResource { ItemPrefab = "Wood", Amount = 10, AmountPerQuality = 5 },
                new MageRecipeResource { ItemPrefab = "Stone", Amount = 2, AmountPerQuality = 1 }
            };

            Bake(recipe);
            var shared = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;

            shared.m_damages.m_fire = 0f;
            shared.m_damages.m_frost = 0f;
            shared.m_damages.m_spirit = 0f;
            shared.m_damages.m_lightning = 0f;
            shared.m_damages.m_poison = 0f;
            shared.m_damagesPerLevel.m_fire = 0f;
            shared.m_damagesPerLevel.m_frost = 0f;
            shared.m_damagesPerLevel.m_spirit = 0f;
            shared.m_damagesPerLevel.m_lightning = 0f;
            shared.m_damagesPerLevel.m_poison = 0f;
            shared.m_attackStatusEffect = null;
            shared.m_attackStatusEffectChance = 0f;
            PrefabUtility.SaveAsPrefabAsset(prefab, WandPath);
            PrepareStationCrafting(prefab, profile);
            PrepareClubsSkill(prefab, profile);
            PrepareStaminaCost(prefab, profile);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        AssetDatabase.SaveAssets();
    }

    private static void PrepareStaminaCost(GameObject prefab, HexBattleMageWandProfile profile)
    {
        if (profile.StarterProgressionVersion >= 4)
        {
            return;
        }

        profile.AttackStamina = 5f;
        prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_attackStamina = profile.AttackStamina;
        profile.StarterProgressionVersion = 4;
        PrefabUtility.SaveAsPrefabAsset(prefab, WandPath);
    }

    private static void PrepareClubsSkill(GameObject prefab, HexBattleMageWandProfile profile)
    {
        if (profile.StarterProgressionVersion >= 3)
        {
            return;
        }

        profile.WeaponSkill = Skills.SkillType.Clubs;
        profile.SkillGain = 1f;
        var shared = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;

        shared.m_skillType = profile.WeaponSkill;
        shared.m_attack.m_raiseSkillAmount = profile.SkillGain;
        shared.m_attack.m_skillHitType = DestructibleType.Character;
        profile.StarterProgressionVersion = 3;
        PrefabUtility.SaveAsPrefabAsset(prefab, WandPath);
    }

    private static void PrepareCharacterAttunements()
    {
        const string runePath = "Assets/_CustomItems/HexBattleMage/Crafting/RuneBlock/HexBattleMage_RuneBlock.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(runePath);

        try
        {
            var profile = prefab.GetComponent<HexBattleMageRuneBlockProfile>();

            if (profile.AttunementVersion >= 2)
            {
                return;
            }

            if (profile.AttunementVersion < 1)
            {
                profile.MaximumActiveAttunements = 1;
                profile.Attunements = new[] { CreateSettings("HexBattleMage_Wand_Attunement_Meadows_01", true) };
            }

            string firePath = AssetDatabase.GetAssetPath(profile.Attunements[0]);
            var fire = PrefabUtility.LoadPrefabContents(firePath);

            try
            {
                var recipe = fire.GetComponent<MageRecipeProfile>();

                recipe.ApplyBakedDefaults();
                recipe.Resources = new[]
                {
                    new MageRecipeResource { ItemPrefab = "Resin", Amount = 2 },
                    new MageRecipeResource { ItemPrefab = "Feathers", Amount = 3 },
                    new MageRecipeResource { ItemPrefab = "Wood", Amount = 10 }
                };
                Bake(recipe);
                PrefabUtility.SaveAsPrefabAsset(fire, firePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(fire);
            }

            profile.AttunementVersion = 2;
            PrefabUtility.SaveAsPrefabAsset(prefab, runePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static void PrepareStationCrafting(GameObject wand, HexBattleMageWandProfile profile)
    {
        if (profile.StarterProgressionVersion >= 2)
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(profile.UpgradeRecipe);
        var settings = PrefabUtility.LoadPrefabContents(path);

        try
        {
            var recipe = settings.GetComponent<MageRecipeProfile>();

            recipe.ApplyBakedDefaults();

            foreach (var resource in recipe.Resources)
            {
                if (resource.ItemPrefab == "Wood")
                {
                    resource.Amount = 10;
                }

                if (resource.ItemPrefab == "Stone")
                {
                    resource.Amount = 2;
                }
            }

            Bake(recipe);
            PrefabUtility.SaveAsPrefabAsset(settings, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(settings);
        }

        profile.CanCraftAtStation = true;
        profile.StarterProgressionVersion = 2;
        PrefabUtility.SaveAsPrefabAsset(wand, WandPath);
    }

    private static GameObject CreateSettings(string identity, bool attunement)
    {
        string path = Folder + "/" + identity + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (existing != null)
        {
            return existing;
        }

        var root = new GameObject(identity);

        try
        {
            var recipe = root.AddComponent<MageRecipeProfile>();

            recipe.CraftingStation = HexBattleMageRuneBlockProfile.PrefabName;
            recipe.RepairStation = HexBattleMageRuneBlockProfile.PrefabName;
            recipe.MinimumStationLevel = 1;
            recipe.Resources = attunement ? new[]
            {
                new MageRecipeResource { ItemPrefab = "Resin", Amount = 2 },
                new MageRecipeResource { ItemPrefab = "Feathers", Amount = 3 },
                new MageRecipeResource { ItemPrefab = "Wood", Amount = 10 }
            } : new[]
            {
                new MageRecipeResource { ItemPrefab = "Wood", Amount = 10, AmountPerQuality = 5 },
                new MageRecipeResource { ItemPrefab = "Stone", Amount = 2, AmountPerQuality = 1 }
            };

            if (attunement)
            {
                root.AddComponent<HexBattleMageWandAttunementProfile>();
            }

            Bake(recipe);
            var result = PrefabUtility.SaveAsPrefabAsset(root, path);

            AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
            return result;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void Bake(MageRecipeProfile recipe)
    {
        string path = Folder + "/Recipe_" + recipe.gameObject.name + ".json";

        File.WriteAllText(path, recipe.GetBakedData());
        AssetDatabase.ImportAsset(path);
        recipe.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
    }
}

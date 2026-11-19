using System;
using System.IO;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Trinkets;
using UnityEditor;
using UnityEngine;

public static class MeadowsCostAssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage";

    [MenuItem("HexBattleMage/Apply Meadows Craft and Upgrade Costs")]
    public static void Build()
    {
        foreach (string category in new[] { "Helmet", "Chest", "Legs" })
        {
            Edit(Root + "/Armor/Meadows/" + category + "_Meadows_01/HexBattleMage_" + category + "_Meadows_01_Definition.prefab", prefab =>
            {
                FixedCosts(prefab);
                prefab.GetComponent<MageRecipeProfile>().Resources = new[] { Resource("DeerHide", 6, 3), Resource("Resin", 2, 1) };
            });
        }

        Edit(Root + "/Wands/Meadows/Wand_Meadows_01/HexBattleMage_Wand_Meadows_01.prefab", prefab =>
        {
            FixedCosts(prefab);
            prefab.GetComponent<MageRecipeProfile>().Resources = new[] { Resource("Wood", 20, 10), Resource("Resin", 2, 1) };
        });
        Edit(Root + "/Shields/Apprentice/HexMagicBracerVisual.prefab", prefab =>
        {
            FixedCosts(prefab);
            prefab.GetComponent<MageRecipeProfile>().Resources = new[] { Resource("Wood", 10, 5), Resource("Resin", 4, 2), Resource("LeatherScraps", 2, 1) };
        });
        Edit(Root + "/Trinkets/Meadows/Trinket_Meadows_01/HexBattleMage_Trinket_Meadows_01.prefab", prefab =>
        {
            prefab.GetComponent<HexBattleMageTrinketProfile>().MaximumQuality = 1;
        });

        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }

    private static void FixedCosts(GameObject prefab)
    {
        var upgrades = prefab.GetComponent<MageUpgradeProfile>();

        if (upgrades == null)
        {
            upgrades = prefab.AddComponent<MageUpgradeProfile>();
        }

        upgrades.FixedResourceCosts = true;
        upgrades.UseFixedStationLevel = false;
    }

    private static MageRecipeResource Resource(string identity, int amount, int upgradeAmount)
    {
        return new MageRecipeResource { ItemPrefab = identity, Amount = amount, AmountPerQuality = upgradeAmount };
    }

    private static void Edit(string path, Action<GameObject> change)
    {
        var prefab = PrefabUtility.LoadPrefabContents(path);

        try
        {
            change(prefab);
            var recipe = prefab.GetComponent<MageRecipeProfile>();
            string recipePath = AssetDatabase.GetAssetPath(recipe.BakedRecipe);

            File.WriteAllText(recipePath, recipe.GetBakedData());
            AssetDatabase.ImportAsset(recipePath);
            recipe.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(recipePath);
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }
}

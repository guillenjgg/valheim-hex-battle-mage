using System;
using System.IO;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;

public static class MeadowsQualityAssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage";

    [MenuItem("HexBattleMage/Apply Meadows Quality and Table Costs")]
    public static void Build()
    {
        foreach (string category in new[] { "Helmet", "Chest", "Legs" })
        {
            Edit(Root + "/Armor/Meadows/" + category + "_Meadows_01/HexBattleMage_" + category + "_Meadows_01_Definition.prefab", prefab =>
            {
                var armor = prefab.GetComponent<HexBattleMageArmorProfile>();
                var recipe = prefab.GetComponent<MageRecipeProfile>();

                var upgrades = prefab.GetComponent<MageUpgradeProfile>();

                if (upgrades == null)
                {
                    upgrades = prefab.AddComponent<MageUpgradeProfile>();
                }

                upgrades.FixedResourceCosts = true;
                upgrades.UseFixedStationLevel = false;
                armor.MaximumQuality = 4;
                armor.ArmorPerQuality = 2f;
                recipe.MinimumStationLevel = 1;
                recipe.Resources = new[] { Resource("DeerHide", 6, 3), Resource("Resin", 2, 1) };
            });
        }

        Edit(Root + "/Crafting/Extensions/HexMageRuneStone.prefab", prefab =>
        {
            prefab.GetComponent<MageRecipeProfile>().Resources = new[] { Resource("Wood", 10), Resource("Flint", 10) };
        });
        Edit(Root + "/Crafting/Extensions/HexMageReagentStand.prefab", prefab =>
        {
            prefab.GetComponent<MageRecipeProfile>().Resources = new[] { Resource("Wood", 10), Resource("Flint", 15), Resource("LeatherScraps", 20), Resource("DeerHide", 5) };
        });

        AssetDatabase.SaveAssets();
        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }

    private static MageRecipeResource Resource(string identity, int amount, int perQuality = 0)
    {
        return new MageRecipeResource { ItemPrefab = identity, Amount = amount, AmountPerQuality = perQuality };
    }

    private static void Edit(string path, Action<GameObject> change)
    {
        var prefab = PrefabUtility.LoadPrefabContents(path);

        try
        {
            change(prefab);
            var recipe = prefab.GetComponent<MageRecipeProfile>();
            string recipePath = recipe.BakedRecipe != null ? AssetDatabase.GetAssetPath(recipe.BakedRecipe) : Path.GetDirectoryName(path) + "/Recipe_" + prefab.name + ".json";

            File.WriteAllText(recipePath, recipe.GetBakedData());
            AssetDatabase.ImportAsset(recipePath);
            recipe.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(recipePath);
            AssetImporter.GetAtPath(recipePath).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }
}

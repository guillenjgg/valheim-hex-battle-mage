using System;
using System.IO;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Trinkets;
using UnityEditor;
using UnityEngine;

public static class TrinketAssetPreparation
{
    private const string TrinketDirectory = "Assets/_CustomItems/HexBattleMage/Trinkets";

    public static void PrepareAll()
    {
        if (!AssetDatabase.IsValidFolder(TrinketDirectory))
        {
            return;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { TrinketDirectory }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (asset.GetComponent<HexBattleMageTrinketProfile>() == null)
            {
                continue;
            }

            var prefab = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var profile = prefab.GetComponent<HexBattleMageTrinketProfile>();
                var recipe = prefab.GetComponent<MageRecipeProfile>();

                if (profile.Icon == null)
                {
                    throw new InvalidOperationException($"Trinket '{path}' requires an authored icon before bundling.");
                }

                if (recipe == null)
                {
                    throw new InvalidOperationException($"Trinket '{path}' requires an authored recipe profile.");
                }

                string recipePath = recipe.BakedRecipe != null ? AssetDatabase.GetAssetPath(recipe.BakedRecipe) : Path.GetDirectoryName(path).Replace('\\', '/') + "/Recipe_" + prefab.name + ".json";

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
}

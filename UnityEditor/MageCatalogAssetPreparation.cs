using System;
using System.IO;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;

public static class MageCatalogAssetPreparation
{
    private const string Root = "Assets/_CustomItems/HexBattleMage";

    public static void Prepare()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var definition = source.GetComponent<MageEquipmentDefinition>();

            if (definition == null)
            {
                continue;
            }

            if (definition.Category == MageEquipmentCategory.None)
            {
                continue;
            }

            if (File.ReadAllText(path).Contains("\n  CatalogRequiresBiomeDiscovery:"))
            {
                continue;
            }

            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var authored = root.GetComponent<MageEquipmentDefinition>();
                var biome = (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), authored.CatalogBiome);

                authored.CatalogBossDefeatKey = BossKey(biome);
                authored.CatalogRequiresBiomeDiscovery = string.IsNullOrEmpty(authored.CatalogBossDefeatKey);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static string BossKey(Heightmap.Biome biome)
    {
        switch (biome)
        {
            case Heightmap.Biome.BlackForest:
                return "defeated_eikthyr";
            case Heightmap.Biome.Swamp:
                return "defeated_gdking";
            case Heightmap.Biome.Mountain:
                return "defeated_bonemass";
            case Heightmap.Biome.Plains:
                return "defeated_dragon";
            case Heightmap.Biome.Mistlands:
                return "defeated_goblinking";
            case Heightmap.Biome.AshLands:
                return "defeated_queen";
            case Heightmap.Biome.DeepNorth:
                return "defeated_fader";
            default:
                return string.Empty;
        }
    }
}

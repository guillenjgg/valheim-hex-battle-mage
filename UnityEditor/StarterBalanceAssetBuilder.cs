using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;

public static class StarterBalanceAssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage";

    [MenuItem("HexBattleMage/Apply Starter Balance")]
    public static void Build()
    {
        Edit(Root + "/Wands/Meadows/Wand_Meadows_01/HexBattleMage_Wand_Meadows_01.prefab", prefab =>
        {
            var profile = prefab.GetComponent<HexBattleMageWandProfile>();
            var shared = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;

            profile.WandBluntDamage = 5f;
            profile.WandPierceDamage = 5f;
            profile.WandBluntDamagePerQuality = 1f;
            profile.WandPierceDamagePerQuality = 1f;
            shared.m_maxQuality = 4;
            shared.m_damages.m_blunt = 5f;
            shared.m_damages.m_pierce = 5f;
            shared.m_damages.m_fire = 15f;
            shared.m_damagesPerLevel.m_blunt = 1f;
            shared.m_damagesPerLevel.m_pierce = 1f;
            shared.m_damagesPerLevel.m_fire = 2f;
        });

        Edit(Root + "/Elements/HexWandElementFire.prefab", prefab =>
        {
            var profile = prefab.GetComponent<WandElementProfile>();

            profile.DirectDamage = 15f;
            profile.DamagePerQuality = 2f;
        });

        Edit(Root + "/Trinkets/Meadows/Trinket_Meadows_01/HexBattleMage_Trinket_Meadows_01.prefab", prefab =>
        {
            prefab.GetComponent<MageRecipeProfile>().Resources = new[]
            {
                Resource("TrophyBoar", 1), Resource("Stone", 10), Resource("BoneFragments", 5), Resource("LeatherScraps", 2)
            };
        });

        foreach (string category in new[] { "Helmet", "Chest", "Legs" })
        {
            Edit(Root + "/Armor/Meadows/" + category + "_Meadows_01/HexBattleMage_" + category + "_Meadows_01_Definition.prefab", prefab =>
            {
                var profile = Vitality(prefab);

                profile.MaximumHealth = 2f;
                profile.MaximumStamina = 0f;
            });
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root + "/Cloaks" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (source.GetComponent<HexBattleMageCloakProfile>() == null)
            {
                continue;
            }

            Edit(path, prefab =>
            {
                var profile = Vitality(prefab);

                profile.MaximumHealth = 0f;
                profile.MaximumStamina = 5f;
            });
        }

        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }

    private static HexBattleMageVitalityProfile Vitality(GameObject prefab)
    {
        var profile = prefab.GetComponent<HexBattleMageVitalityProfile>();

        return profile != null ? profile : prefab.AddComponent<HexBattleMageVitalityProfile>();
    }

    private static MageRecipeResource Resource(string name, int amount)
    {
        return new MageRecipeResource { ItemPrefab = name, Amount = amount };
    }

    private static void Edit(string path, System.Action<GameObject> edit)
    {
        var prefab = PrefabUtility.LoadPrefabContents(path);

        try
        {
            edit(prefab);
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }
}

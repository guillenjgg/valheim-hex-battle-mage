using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CloakStatsValidation
{
    private const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void Run()
    {
        var harmony = new Harmony("HexBattleMage.CloakStatsValidation");
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE"));
        var root = new GameObject("Cloak stats fixture");

        root.SetActive(false);

        try
        {
            var mod = typeof(MageEitrProfile).Assembly;

            foreach (string name in new[] { "MageGearMaximumEitrPatch", "MageGearEitrRegenerationPatch", "CloakEitrEquipPatch", "CloakEitrUnequipPatch" })
            {
                harmony.CreateClassProcessor(mod.GetType("HexBattleMage.Patches." + name)).Patch();
            }

            foreach (string name in new[] { "SetupEquipment", "TriggerEquipEffect" })
            {
                harmony.Patch(AccessTools.Method(typeof(Humanoid), name), prefix: new HarmonyMethod(typeof(CloakStatsValidation), "Skip"));
            }

            harmony.Patch(AccessTools.Method(typeof(Character), "GetZDOID"), prefix: new HarmonyMethod(typeof(CloakStatsValidation), "Identity"));
            var player = root.AddComponent<TrinketValidationPlayer>();
            var seman = new SEMan(player, root.AddComponent<ZNetView>());

            AccessTools.Field(typeof(Character), "m_seman").SetValue(player, seman);
            AccessTools.Field(typeof(SEMan), "m_statusEffectAttributesOld").SetValue(seman, 0);
            var visual = bundle.LoadAsset<GameObject>("HexBattleMage_Cloak_Meadows_01");
            var source = visual.GetComponent<HexBattleMageCloakProfile>();
            var sourceEitr = visual.GetComponent<MageEitrProfile>();

            Check(source.Armor == 2f && sourceEitr.MaximumEitr == 5f && sourceEitr.RegenerationModifier == 0f, "Prefab contains exactly 2 armor and +5 Eitr.");
            var itemRoot = new GameObject("Equippable cloak fixture");

            itemRoot.transform.SetParent(root.transform, false);
            itemRoot.AddComponent<HexBattleMageCloakProfile>();
            var eitr = itemRoot.AddComponent<MageEitrProfile>();

            eitr.MaximumEitr = sourceEitr.MaximumEitr;
            eitr.RegenerationModifier = sourceEitr.RegenerationModifier;
            var item = new ItemDrop.ItemData { m_dropPrefab = itemRoot, m_shared = new ItemDrop.ItemData.SharedData { m_name = source.DisplayName, m_itemType = ItemDrop.ItemData.ItemType.Shoulder, m_armor = source.Armor, m_maxStackSize = 1 } };

            harmony.Patch(AccessTools.Method(typeof(ItemDrop), "OnValidate"), prefix: new HarmonyMethod(typeof(CloakStatsValidation), "Skip"));
            itemRoot.AddComponent<ItemDrop>().m_itemData = item;
            player.GetInventory().GetAllItems().Add(item);
            Check(player.GetMaxEitr() == 0f, "Carrying the cloak does not grant a pool.");
            Check(player.EquipItem(item, false), "Native cape slot accepts the cloak.");
            Check(player.GetMaxEitr() == 5f && player.GetEitr() == 0f, "Equip immediately grants a five-point cap without refilling it or requiring food.");
            player.AddEitr(3f);
            player.UnequipItem(item, false);
            Check(player.GetMaxEitr() == 0f && player.GetEitr() == 0f, "Unequip removes and clamps the gear pool.");
            Check(player.EquipItem(item, false), "Re-equip succeeds.");
            Check(player.GetEitr() == 0f, "Re-equipping cannot refill Eitr.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "cloak-stats-report.txt"), "PASS: production prefab has armor 2 and Eitr 5 with no extra regeneration modifier. Actual native cape equip/unequip updates no-food Eitr cap immediately; inventory-only grants nothing; no refill exploit. Setup visuals/effects isolated; full regeneration ticks remain an in-game check.");
        }
        finally
        {
            harmony.UnpatchSelf();
            Object.DestroyImmediate(root);
            bundle.Unload(true);
        }
    }

    private static bool Identity(ref ZDOID __result)
    {
        __result = default(ZDOID);
        return false;
    }

    private static bool Skip()
    {
        return false;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

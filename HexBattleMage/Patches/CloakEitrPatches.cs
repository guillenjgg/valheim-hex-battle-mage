using HarmonyLib;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Trinkets;
using UnityEngine;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    internal static class CloakEitrEquipPatch
    {
        private static void Prefix(Humanoid __instance, out float __state)
        {
            var player = __instance as Player;

            __state = player != null ? player.GetEitr() : 0f;
        }

        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __result, float __state)
        {
            if (!__result)
            {
                return;
            }

            if (item?.m_dropPrefab == null)
            {
                return;
            }

            if (item.m_dropPrefab.GetComponent<HexBattleMageCloakProfile>() == null)
            {
                return;
            }

            TrinketEffects.RefreshGearEitr(__instance, __state);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    internal static class CloakEitrUnequipPatch
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (item?.m_dropPrefab == null)
            {
                return;
            }

            if (item.m_dropPrefab.GetComponent<HexBattleMageCloakProfile>() == null)
            {
                return;
            }

            TrinketEffects.RefreshGearEitr(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    internal static class CloakEitrTooltipPatch
    {
        private static void Postfix(ItemDrop.ItemData item, ref string __result)
        {
            if (item?.m_dropPrefab == null)
            {
                return;
            }

            if (item.m_dropPrefab.GetComponent<HexBattleMageCloakProfile>() == null)
            {
                return;
            }

            var eitr = item.m_dropPrefab.GetComponent<MageEitrProfile>();

            if (eitr == null)
            {
                return;
            }

            __result += "\n" + ModLocalization.Format("$hexbattlemage_gear_maximum_eitr", Mathf.Max(0f, eitr.MaximumEitr));
        }
    }
}

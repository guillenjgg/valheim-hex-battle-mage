using HarmonyLib;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(Recipe), nameof(Recipe.GetRequiredStationLevel))]
    internal static class MageUpgradeStationPatch
    {
        private static void Postfix(Recipe __instance, int quality, ref int __result)
        {
            if (quality <= 1)
            {
                return;
            }

            if (__instance.m_item == null)
            {
                return;
            }

            var profile = __instance.m_item.GetComponent<MageUpgradeProfile>();

            if (profile == null)
            {
                return;
            }

            if (!profile.UseFixedStationLevel)
            {
                return;
            }

            __result = Mathf.Max(1, profile.RequiredStationLevel);
        }
    }

    [HarmonyPatch(typeof(Piece.Requirement), nameof(Piece.Requirement.GetAmount))]
    internal static class MageUpgradeCostPatch
    {
        private static void Postfix(Piece.Requirement __instance, int qualityLevel, ref int __result)
        {
            if (qualityLevel <= 1)
            {
                return;
            }

            if (!GearUpgradeRules.UsesFixedCosts(__instance))
            {
                return;
            }

            __result = Mathf.Max(0, __instance.m_amountPerLevel);
        }
    }
}

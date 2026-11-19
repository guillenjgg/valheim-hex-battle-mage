using System.Text;
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

            if (item.m_dropPrefab.GetComponent<HexBattleMageCloakProfile>() == null && item.m_dropPrefab.GetComponent<HexBattleMageArmorProfile>() == null)
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

            if (item.m_dropPrefab.GetComponent<HexBattleMageCloakProfile>() == null && item.m_dropPrefab.GetComponent<HexBattleMageArmorProfile>() == null)
            {
                return;
            }

            TrinketEffects.RefreshGearEitr(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    internal static class MageGearTooltipPatch
    {
        private static void Postfix(ItemDrop.ItemData item, int qualityLevel, ref string __result)
        {
            if (item?.m_dropPrefab == null)
            {
                return;
            }

            if (item.m_dropPrefab.GetComponent<HexBattleMageTrinketProfile>() != null)
            {
                return;
            }

            var eitr = item.m_dropPrefab.GetComponent<MageEitrProfile>();

            var vitals = item.m_dropPrefab.GetComponent<HexBattleMageVitalityProfile>();

            if (eitr == null && vitals == null)
            {
                return;
            }

            var perks = new StringBuilder();

            if (vitals != null && vitals.MaximumHealth > 0f)
            {
                perks.AppendLine(ModLocalization.Format("$hexbattlemage_gear_maximum_health", vitals.MaximumHealth));
            }

            if (vitals != null && vitals.MaximumStamina > 0f)
            {
                perks.AppendLine(ModLocalization.Format("$hexbattlemage_gear_maximum_stamina", vitals.MaximumStamina));
            }

            if (eitr != null)
            {
                perks.AppendLine(ModLocalization.Format("$hexbattlemage_gear_maximum_eitr", eitr.GetMaximumEitr(qualityLevel)));
            }

            __result = GearTooltip.InsertPerks(item, __result, perks.ToString());
        }
    }
}

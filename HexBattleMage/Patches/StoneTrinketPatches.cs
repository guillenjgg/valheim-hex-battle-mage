using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
    internal static class MageGearMaximumEitrPatch
    {
        private static void Postfix(Player __instance, ref float __2)
        {
            __2 += StoneTrinketManager.EitrBonus(__instance, false);
        }
    }

    [HarmonyPatch(typeof(Player), "GetEquipmentEitrRegenModifier")]
    internal static class MageGearEitrRegenerationPatch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            __result += StoneTrinketManager.EitrBonus(__instance, true);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    internal static class StoneTrinketEquipPatch
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __result)
        {
            if (!__result)
            {
                return;
            }

            if (StoneTrinketManager.GetProfile(item) == null)
            {
                return;
            }

            StoneTrinketManager.RefreshEitr(__instance);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    internal static class StoneTrinketUnequipPatch
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (StoneTrinketManager.GetProfile(item) == null)
            {
                return;
            }

            StoneTrinketManager.RefreshEitr(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), "OnDeath")]
    internal static class StoneTrinketDeathPatch
    {
        private static void Prefix(Player __instance)
        {
            StoneTrinketManager.ClearReward(__instance);
        }
    }

    [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
    internal static class StoneTrinketParryPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var perfectBlockEffect = AccessTools.Field(typeof(Humanoid), "m_perfectBlockEffect");
            int matches = 0;

            foreach (var instruction in instructions)
            {
                yield return instruction;

                if (!instruction.LoadsField(perfectBlockEffect))
                {
                    continue;
                }

                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StoneTrinketManager), nameof(StoneTrinketManager.RewardParry)));
                matches++;
            }

            if (matches != 1)
            {
                throw new InvalidOperationException("Valheim successful-parry hook changed; expected one perfect-block effect branch, found " + matches + ".");
            }
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    internal static class StoneTrinketWandDamagePatch
    {
        private static void Prefix(Character owner, ref HitData hitData, ItemDrop.ItemData item)
        {
            if (!ItemRegistrationManager.IsWand(item))
            {
                return;
            }

            if (hitData == null)
            {
                return;
            }

            var player = owner as Player;

            if (player == null)
            {
                return;
            }

            float multiplier = StoneTrinketManager.DamageMultiplier(player);

            if (multiplier <= 1f)
            {
                return;
            }

            hitData = hitData.Clone();
            hitData.m_damage.Modify(multiplier);
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    internal static class StoneTrinketTooltipPatch
    {
        private static void Postfix(ItemDrop.ItemData item, ref string __result)
        {
            if (StoneTrinketManager.GetProfile(item) != null)
            {
                __result += StoneTrinketManager.Tooltip();
            }
        }
    }
}

using HarmonyLib;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Visuals;
using System;
using System.Text;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    internal static class MagicBracerTooltipPatch
    {
        private static readonly Action<ItemDrop.ItemData, int, StringBuilder> AddBlockTooltip = AccessTools.MethodDelegate<Action<ItemDrop.ItemData, int, StringBuilder>>(AccessTools.Method(typeof(ItemDrop.ItemData), "AddBlockTooltip"));

        private static void Postfix(ItemDrop.ItemData item, int qualityLevel, ref string __result)
        {
            if (!MagicShieldManager.SupportsBlocking(item))
            {
                return;
            }

            if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Hands)
            {
                return;
            }

            var text = new StringBuilder(__result);

            AddBlockTooltip(item, qualityLevel, text);
            __result = text.ToString();
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.IsEquipable))]
    internal static class MagicBracerEquipablePatch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref bool __result)
        {
            if (MagicShieldManager.IsBracer(__instance))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    internal static class MagicBracerEquipPatch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (!MagicShieldManager.IsBracer(item))
            {
                return true;
            }

            __result = MagicShieldManager.EquipBracer(__instance, item);
            return false;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    internal static class MagicBracerUnequipPatch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (!MagicShieldManager.IsBracer(item))
            {
                return true;
            }

            MagicShieldManager.UnequipBracer(__instance, item);
            return false;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
    internal static class MagicBracerEquippedPatch
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (MagicShieldManager.IsBracer(item))
            {
                __result = item.m_equipped && __instance.GetInventory().ContainsItem(item);
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemTypeEquiped))]
    internal static class MagicBracerEquippedTypePatch
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (MagicShieldManager.IsBracer(item))
            {
                __result = MagicShieldManager.GetEquippedBracer(__instance) != null;
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
    internal static class MagicBracerUnequipAllPatch
    {
        private static void Postfix(Humanoid __instance)
        {
            var bracer = MagicShieldManager.GetEquippedBracer(__instance);

            if (bracer != null)
            {
                MagicShieldManager.UnequipBracer(__instance, bracer);
            }
        }
    }

    [HarmonyPatch(typeof(Player), "Awake")]
    internal static class MagicBracerPlayerVisualPatch
    {
        private static void Postfix(Player __instance)
        {
            __instance.gameObject.AddComponent<MagicBracerVisualController>();
        }
    }
}

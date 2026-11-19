using HarmonyLib;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
    internal static class GearVitalityFoodValuesPatch
    {
        private static void Postfix(Player __instance, ref float __0, ref float __1)
        {
            __0 += GearVitals.Bonus(__instance, true);
            __1 += GearVitals.Bonus(__instance, false);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    internal static class GearVitalityEquipPatch
    {
        internal struct Snapshot
        {
            internal float Health;
            internal float Stamina;
        }

        private static void Prefix(Humanoid __instance, out Snapshot __state)
        {
            var player = __instance as Player;

            __state = new Snapshot { Health = player != null ? player.GetHealth() : 0f, Stamina = player != null ? player.GetStamina() : 0f };
        }

        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __result, Snapshot __state)
        {
            if (!__result)
            {
                return;
            }

            var player = __instance as Player;

            if (player == null)
            {
                return;
            }

            if (item?.m_dropPrefab == null)
            {
                return;
            }

            if (item.m_dropPrefab.GetComponent<HexBattleMageVitalityProfile>() == null)
            {
                return;
            }

            GearVitals.Refresh(player, __state.Health, __state.Stamina);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    internal static class GearVitalityUnequipPatch
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item)
        {
            var player = __instance as Player;

            if (player == null)
            {
                return;
            }

            if (item?.m_dropPrefab == null)
            {
                return;
            }

            if (item.m_dropPrefab.GetComponent<HexBattleMageVitalityProfile>() == null)
            {
                return;
            }

            GearVitals.Refresh(player, player.GetHealth(), player.GetStamina());
        }
    }
}

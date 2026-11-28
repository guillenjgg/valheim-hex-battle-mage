using HarmonyLib;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
    internal static class StaffProjectileHitPatch
    {
        private static void Prefix(Projectile __instance, ItemDrop.ItemData ___m_weapon, Collider collider, out bool __state)
        {
            __state = false;

            if (___m_weapon?.m_dropPrefab == null)
            {
                return;
            }

            var equipment = ___m_weapon.m_dropPrefab.GetComponent<MageEquipmentDefinition>();

            if (equipment == null || equipment.Category != MageEquipmentCategory.Staff)
            {
                return;
            }

            __state = true;
            DebugLog.LogInfo("Earth staff impact begin: target=" + (collider != null ? collider.name : "water/terrain") + "; cinder spawners=" + __instance.GetComponents<CinderSpawner>().Length + ".");
        }

        private static void Postfix(bool __state)
        {
            if (__state)
            {
                DebugLog.LogInfo("Earth staff impact completed.");
            }
        }
    }
}

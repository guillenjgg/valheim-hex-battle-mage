using HarmonyLib;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
    internal static class WandProjectileFalloffPatch
    {
        internal struct DamageSnapshot
        {
            internal bool Modified;
            internal HitData.DamageTypes OriginalDamage;
        }

        private static void Prefix(Projectile __instance, ItemDrop.ItemData ___m_weapon, Character ___m_owner, Vector3 hitPoint, out DamageSnapshot __state)
        {
            __state = default(DamageSnapshot);

            if (!ItemRegistrationManager.IsWand(___m_weapon))
            {
                return;
            }

            float distance = Vector3.Distance(__instance.m_startPoint, hitPoint);
            float multiplier = ProjectileDamageFalloff.CalculateMultiplier(distance, DevConfig.FullDamageRange.Value, DevConfig.DamageFalloffDistance.Value, DevConfig.MinimumDamageMultiplier.Value);

            if (___m_owner == Player.m_localPlayer && Player.m_localPlayer != null)
            {
                float damage = __instance.m_damage.GetTotalDamage() * multiplier;

                WandStats.RecordImpact(Player.m_localPlayer, ___m_weapon.m_dropPrefab.name, damage, __instance.m_aoe);
            }

            if (multiplier >= 1f)
            {
                return;
            }

            __state.OriginalDamage = __instance.m_damage;
            __state.Modified = true;
            __instance.m_damage.Modify(multiplier);
        }

        private static void Finalizer(Projectile __instance, DamageSnapshot __state)
        {
            if (!__state.Modified)
            {
                return;
            }

            __instance.m_damage = __state.OriginalDamage;
        }
    }
}

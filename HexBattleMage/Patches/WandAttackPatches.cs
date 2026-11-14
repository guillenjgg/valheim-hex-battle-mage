using HarmonyLib;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
    internal static class WandAttackStartPatch
    {
        private static void Prefix(Attack __instance, ItemDrop.ItemData weapon)
        {
            if (!ItemRegistrationManager.IsWand(weapon))
            {
                return;
            }

            if (__instance.m_attackType != Attack.AttackType.Projectile)
            {
                return;
            }

            ItemRegistrationManager.ApplySettings(__instance);
        }

        private static void Postfix(Attack __instance, bool __result, ItemDrop.ItemData weapon, CharacterAnimEvent animEvent)
        {
            if (!__result)
            {
                return;
            }

            if (!ItemRegistrationManager.IsWand(weapon))
            {
                return;
            }

            if (__instance.m_attackType != Attack.AttackType.Projectile)
            {
                return;
            }

            var animator = animEvent.GetComponent<Animator>();

            animator.speed = DevConfig.AnimationSpeedMultiplier.Value;
        }
    }

    [HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.Speed))]
    internal static class WandAnimationSpeedPatch
    {
        private static void Prefix(Character ___m_character, ref float speedScale)
        {
            var player = ___m_character as Player;

            if (player == null)
            {
                return;
            }

            if (!player.InAttack())
            {
                return;
            }

            if (!ItemRegistrationManager.IsWand(player.GetCurrentWeapon()))
            {
                return;
            }

            speedScale *= DevConfig.AnimationSpeedMultiplier.Value;
        }
    }
}

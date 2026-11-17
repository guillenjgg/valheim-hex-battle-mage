using System;
using HarmonyLib;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(Humanoid), "SetAnimationState")]
    internal static class MagicShieldAnimationPatch
    {
        private static void Prefix(Humanoid __instance, ref ItemDrop.ItemData.AnimationState __0)
        {
            if (MagicShieldManager.GetBlocker(__instance) == null)
            {
                return;
            }

            if (__instance.IsBlocking())
            {
                __0 = ItemDrop.ItemData.AnimationState.Shield;
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), "UpdateBlock")]
    internal static class MagicShieldBlockStatePatch
    {
        private static readonly Action<Humanoid> SetupAnimationState = AccessTools.MethodDelegate<Action<Humanoid>>(AccessTools.Method(typeof(Humanoid), "SetupAnimationState"));

        private static void Prefix(bool ___m_internalBlockingState, out bool __state)
        {
            __state = ___m_internalBlockingState;
        }

        private static void Postfix(Humanoid __instance, bool ___m_internalBlockingState, bool __state)
        {
            if (MagicShieldManager.GetBlocker(__instance) == null)
            {
                return;
            }

            if (__state == ___m_internalBlockingState && !___m_internalBlockingState)
            {
                return;
            }

            SetupAnimationState(__instance);
        }
    }

    [HarmonyPatch(typeof(Humanoid), "GetCurrentBlocker")]
    internal static class MagicShieldBlockerPatch
    {
        private static void Postfix(Humanoid __instance, ref ItemDrop.ItemData __result)
        {
            var bracer = MagicShieldManager.GetBlocker(__instance);

            if (bracer != null)
            {
                __result = bracer;
            }
        }
    }

}

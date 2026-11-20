using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Patches
{
    [HarmonyPatch]
    internal static class WandAttunedDamagePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(ItemDrop.ItemData)))
            {
                if (method.Name == nameof(ItemDrop.ItemData.GetDamage))
                {
                    yield return method;
                }
            }
        }

        private static void Prefix(ItemDrop.ItemData __instance)
        {
            if (ItemRegistrationManager.IsWand(__instance))
            {
                ItemRegistrationManager.ApplyDamageSettings(__instance);
            }
        }
    }
}

using HarmonyLib;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetLevel))]
    internal static class RuneBlockLevelPatch
    {
        private static void Postfix(CraftingStation __instance, ref int __result)
        {
            var profile = __instance.GetComponent<HexBattleMageRuneBlockProfile>();

            if (profile == null)
            {
                return;
            }

            __result = RuneBlockProgression.GetLevel(profile);
        }
    }
}

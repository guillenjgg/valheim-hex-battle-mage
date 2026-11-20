using HarmonyLib;

namespace HexBattleMage.Core
{
    internal static class StarterWandRecipeRules
    {
        private static Recipe _craft;
        private static Recipe _upgrade;

        internal static void Register(Recipe craft, Recipe upgrade)
        {
            _craft = craft;
            _upgrade = upgrade;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.RequiredCraftingStation))]
        private static class InventoryCraftingStationPatch
        {
            private static void Postfix(Player __instance, Recipe recipe, ref bool __result)
            {
                if (recipe != _craft)
                {
                    return;
                }

                if (__instance.GetCurrentCraftingStation() != null)
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(Recipe), nameof(Recipe.GetRequiredStation))]
        private static class UpgradeStationPatch
        {
            private static void Postfix(Recipe __instance, int quality, ref CraftingStation __result)
            {
                if (quality <= 1)
                {
                    return;
                }

                if (__instance != _craft)
                {
                    return;
                }

                __result = _upgrade.m_craftingStation;
            }
        }
    }
}

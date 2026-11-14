using HarmonyLib;
using HexBattleMage.Core.UI;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(InventoryGui), "Awake")]
    internal static class MageUnlocksCreatePatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            __instance.gameObject.AddComponent<MageUnlocksTab>().Initialize(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTabCraftPressed))]
    internal static class MageUnlocksCraftTabPatch
    {
        private static void Prefix(InventoryGui __instance)
        {
            __instance.GetComponent<MageUnlocksTab>()?.Close();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTabUpgradePressed))]
    internal static class MageUnlocksUpgradeTabPatch
    {
        private static void Prefix(InventoryGui __instance)
        {
            __instance.GetComponent<MageUnlocksTab>()?.Close();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed")]
    internal static class MageUnlocksBlockCraftPatch
    {
        private static bool Prefix(InventoryGui __instance)
        {
            var tab = __instance.GetComponent<MageUnlocksTab>();

            return tab == null || !tab.IsOpen;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipeGamepadInput")]
    internal static class MageUnlocksBlockRecipeInputPatch
    {
        private static bool Prefix(InventoryGui __instance)
        {
            var tab = __instance.GetComponent<MageUnlocksTab>();

            return tab == null || !tab.IsOpen;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    internal static class MageUnlocksHidePatch
    {
        private static void Prefix(InventoryGui __instance)
        {
            __instance.GetComponent<MageUnlocksTab>()?.Close();
        }
    }
}

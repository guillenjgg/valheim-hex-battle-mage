using HarmonyLib;
using HexBattleMage.Assets;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Visuals;
using System.Reflection;

namespace HexBattleMage.Patches
{
    internal static class WandStyleSelection
    {
        private static readonly FieldInfo SelectedRecipeField = AccessTools.Field(typeof(InventoryGui), "m_selectedRecipe");
        private static readonly PropertyInfo SelectedItemProperty = AccessTools.Property(SelectedRecipeField.FieldType, "ItemData");
        private static readonly PropertyInfo SelectedRecipeProperty = AccessTools.Property(SelectedRecipeField.FieldType, "Recipe");
        private static readonly MethodInfo InventoryChangedMethod = AccessTools.Method(typeof(Inventory), "Changed", new[] { typeof(bool), typeof(bool) });
        private static readonly MethodInfo SetupEquipmentMethod = AccessTools.Method(typeof(Humanoid), "SetupEquipment");

        internal static ItemDrop.ItemData GetSelectedWand(InventoryGui gui)
        {
            var selection = SelectedRecipeField.GetValue(gui);
            var item = (ItemDrop.ItemData)SelectedItemProperty.GetValue(selection);

            return ItemRegistrationManager.IsWand(item) ? item : null;
        }

        internal static bool IsWandRecipe(InventoryGui gui)
        {
            var selection = SelectedRecipeField.GetValue(gui);
            var recipe = (Recipe)SelectedRecipeProperty.GetValue(selection);

            if (recipe == null)
            {
                return false;
            }

            if (recipe.m_item == null)
            {
                return false;
            }

            return recipe.m_item.name == AssetBundleHelper.HexBattleMageWand.name;
        }

        internal static bool IsAtMageTable(Player player)
        {
            if (player == null)
            {
                return false;
            }

            var station = player.GetCurrentCraftingStation();

            if (station == null)
            {
                return false;
            }

            return CraftingStationManager.IsMageTable(station);
        }

        internal static ItemDrop.ItemData FindOwnedWand(InventoryGui gui, Player player, out bool ambiguous)
        {
            ambiguous = false;

            var selected = GetSelectedWand(gui);

            if (selected != null)
            {
                return selected;
            }

            var equipped = player.GetCurrentWeapon();

            if (ItemRegistrationManager.IsWand(equipped))
            {
                return equipped;
            }

            ItemDrop.ItemData candidate = null;

            foreach (var item in player.GetInventory().GetAllItems())
            {
                if (!ItemRegistrationManager.IsWand(item))
                {
                    continue;
                }

                if (candidate != null)
                {
                    ambiguous = true;
                    return null;
                }

                candidate = item;
            }

            return candidate;
        }

        internal static void RefreshEquipment(Player player)
        {
            InventoryChangedMethod.Invoke(player.GetInventory(), new object[] { false, false });
            SetupEquipmentMethod.Invoke(player, null);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
    internal static class WandStyleButtonPatch
    {
        private static void Postfix(InventoryGui __instance, Player player)
        {
            if (!WandStyleSelection.IsAtMageTable(player))
            {
                return;
            }

            if (!WandStyleSelection.IsWandRecipe(__instance))
            {
                return;
            }

            __instance.m_variantButton.gameObject.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnVariantSelected")]
    internal static class WandRestylePatch
    {
        private static void Postfix(InventoryGui __instance, int index)
        {
            var player = Player.m_localPlayer;

            if (!WandStyleSelection.IsAtMageTable(player))
            {
                return;
            }

            if (!WandStyleSelection.IsWandRecipe(__instance))
            {
                return;
            }

            if (index < 0 || index >= WandStyle.StyleCount)
            {
                return;
            }

            var item = WandStyleSelection.FindOwnedWand(__instance, player, out bool ambiguous);

            if (ambiguous)
            {
                player.Message(MessageHud.MessageType.Center, ModLocalization.Format("$hexbattlemage_style_equip_wand"));
                return;
            }

            if (item == null)
            {
                player.Message(MessageHud.MessageType.Center, ModLocalization.Format("$hexbattlemage_style_next_craft"));
                return;
            }

            item.m_variant = index;
            WandStyleSelection.RefreshEquipment(player);
            player.Message(MessageHud.MessageType.Center, ModLocalization.Format("$hexbattlemage_style_applied"));
            DebugLog.LogInfo($"Applied wand cosmetic {index} to an owned wand from the Mage Table Style picker.");
        }
    }
}

using System;
using System.Reflection;
using HarmonyLib;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Patches
{
    internal static class ArmorStyleSelection
    {
        private static readonly FieldInfo Selection = AccessTools.Field(typeof(InventoryGui), "m_selectedRecipe");
        private static readonly PropertyInfo RecipeProperty = AccessTools.Property(Selection.FieldType, "Recipe");
        private static readonly PropertyInfo ItemProperty = AccessTools.Property(Selection.FieldType, "ItemData");

        internal static Recipe GetRecipe(InventoryGui gui)
        {
            return (Recipe)RecipeProperty.GetValue(Selection.GetValue(gui));
        }

        internal static ItemDrop.ItemData GetSelectedItem(InventoryGui gui)
        {
            return (ItemDrop.ItemData)ItemProperty.GetValue(Selection.GetValue(gui));
        }

        internal static MageArmorStyleProfile GetProfile(Recipe recipe)
        {
            if (recipe?.m_item == null)
            {
                return null;
            }

            var profile = recipe.m_item.GetComponent<MageArmorStyleProfile>();

            if (profile != null)
            {
                return profile;
            }

            var drop = recipe.m_item.m_itemData.m_dropPrefab;

            return drop != null ? drop.GetComponent<MageArmorStyleProfile>() : null;
        }

        internal static bool MatchesRecipe(ItemDrop.ItemData item, Recipe recipe)
        {
            if (item?.m_dropPrefab == null)
            {
                return false;
            }

            if (recipe?.m_item == null)
            {
                return false;
            }

            var target = recipe.m_item.m_itemData.m_dropPrefab;
            string targetName = target != null ? target.name : recipe.m_item.name;

            return string.Equals(item.m_dropPrefab.name, targetName, StringComparison.Ordinal);
        }

        internal static ItemDrop.ItemData FindOwnedItem(InventoryGui gui, Player player, Recipe recipe, out bool ambiguous)
        {
            ambiguous = false;
            var selected = GetSelectedItem(gui);

            if (MatchesRecipe(selected, recipe))
            {
                return selected;
            }

            ItemDrop.ItemData candidate = null;

            foreach (var item in player.GetInventory().GetAllItems())
            {
                if (!MatchesRecipe(item, recipe))
                {
                    continue;
                }

                if (item.m_equipped)
                {
                    ambiguous = false;
                    return item;
                }

                if (candidate != null)
                {
                    ambiguous = true;
                }

                candidate = item;
            }

            return ambiguous ? null : candidate;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
    internal static class ArmorStyleButtonPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(InventoryGui __instance, Player player)
        {
            if (!WandStyleSelection.IsAtMageTable(player))
            {
                return;
            }

            var profile = ArmorStyleSelection.GetProfile(ArmorStyleSelection.GetRecipe(__instance));

            if (profile == null)
            {
                return;
            }

            __instance.m_variantButton.gameObject.SetActive(profile.Styles.Length > 1);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnVariantSelected")]
    internal static class ArmorRestylePatch
    {
        private static void Postfix(InventoryGui __instance, int index)
        {
            var player = Player.m_localPlayer;

            if (!WandStyleSelection.IsAtMageTable(player))
            {
                return;
            }

            var recipe = ArmorStyleSelection.GetRecipe(__instance);
            var profile = ArmorStyleSelection.GetProfile(recipe);

            if (profile == null)
            {
                return;
            }

            if (index < 0 || index >= profile.Styles.Length)
            {
                return;
            }

            var item = ArmorStyleSelection.FindOwnedItem(__instance, player, recipe, out bool ambiguous);

            if (ambiguous)
            {
                player.Message(MessageHud.MessageType.Center, ModLocalization.Format("$hexbattlemage_style_equip_armor"));
                return;
            }

            if (item == null)
            {
                player.Message(MessageHud.MessageType.Center, ModLocalization.Format("$hexbattlemage_armor_style_next_craft"));
                return;
            }

            item.m_variant = index;
            DebugLog.LogInfo("Armor appearance applied: " + item.m_dropPrefab.name + "; variant=" + index + "; equipped=" + item.m_equipped + ".");
            WandStyleSelection.RefreshEquipment(player);
            player.Message(MessageHud.MessageType.Center, ModLocalization.Format("$hexbattlemage_armor_style_applied", ModLocalization.Format(profile.Styles[index].DisplayName)));
        }
    }
}

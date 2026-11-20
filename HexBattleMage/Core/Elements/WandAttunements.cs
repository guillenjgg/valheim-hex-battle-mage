using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HexBattleMage.Assets;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Elements
{
    internal static class WandAttunements
    {
        private const string ActiveKey = "HexBattleMage.Attunements.Active";
        private const string LearnedPrefix = "HexBattleMage.Attunements.Learned.";
        private static readonly MethodInfo CloneShared = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly ConditionalWeakTable<ItemDrop.ItemData, ItemDrop.ItemData.SharedData> OwnedStats = new ConditionalWeakTable<ItemDrop.ItemData, ItemDrop.ItemData.SharedData>();

        internal static HexBattleMageRuneBlockProfile Profile => PrefabSettings.Require<HexBattleMageRuneBlockProfile>(AssetBundleHelper.Bundle.LoadAsset<GameObject>(RuneBlockManager.PrefabName));
        internal static int MaximumActive => Mathf.Clamp(Profile.MaximumActiveAttunements, 1, Enum.GetValues(typeof(WandElement)).Length);
        internal static IEnumerable<HexBattleMageWandAttunementProfile> Definitions => Profile.Attunements.Select(prefab => PrefabSettings.Require<HexBattleMageWandAttunementProfile>(prefab));

        internal static void EnsurePrivateStats(ItemDrop.ItemData item)
        {
            if (OwnedStats.TryGetValue(item, out var shared) && ReferenceEquals(shared, item.m_shared))
            {
                return;
            }

            item.m_shared = (ItemDrop.ItemData.SharedData)CloneShared.Invoke(item.m_shared, null);
            OwnedStats.Remove(item);
            OwnedStats.Add(item, item.m_shared);
        }

        internal static Player ResolvePlayer(ItemDrop.ItemData item)
        {
            var local = Player.m_localPlayer;

            if (local != null && local.GetInventory().ContainsItem(item))
            {
                return local;
            }

            foreach (var player in Player.GetAllPlayers())
            {
                if (player.GetInventory().ContainsItem(item))
                {
                    return player;
                }
            }

            return local;
        }

        internal static bool HasLearned(Player player, WandElement element)
        {
            return player != null && player.m_customData.ContainsKey(LearnedPrefix + element);
        }

        internal static List<WandElement> GetActive(Player player)
        {
            var result = new List<WandElement>();

            if (player == null)
            {
                return result;
            }

            if (!player.m_customData.TryGetValue(ActiveKey, out string value))
            {
                return result;
            }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mask))
            {
                return result;
            }

            foreach (WandElement element in Enum.GetValues(typeof(WandElement)))
            {
                if ((mask & (1 << (int)element)) != 0 && HasLearned(player, element))
                {
                    result.Add(element);
                }

                if (result.Count >= MaximumActive)
                {
                    break;
                }
            }

            return result;
        }

        internal static List<WandElement> GetCombatElements(ItemDrop.ItemData item)
        {
            if (!ItemRegistrationManager.IsWand(item))
            {
                return new List<WandElement>();
            }

            if (WandElementManager.BypassAttunements != null && WandElementManager.BypassAttunements.Value)
            {
                return new List<WandElement> { WandElementManager.ActiveElement.Value };
            }

            return GetActive(ResolvePlayer(item));
        }

        internal static bool IsDiscovered(Player player, MageRecipeProfile recipe)
        {
            foreach (var resource in recipe.GetResources())
            {
                var prefab = ObjectDB.instance.GetItemPrefab(resource.ItemPrefab);

                if (prefab == null)
                {
                    return false;
                }

                if (!player.IsMaterialKnown(prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CanUseStation(Player player, HexBattleMageWandAttunementProfile definition)
        {
            if (player == null)
            {
                return false;
            }

            if (definition == null)
            {
                return false;
            }

            if (!Definitions.Contains(definition))
            {
                return false;
            }

            var station = player.GetCurrentCraftingStation();

            if (!RuneBlockManager.IsRuneBlock(station))
            {
                return false;
            }

            var recipe = definition.GetComponent<MageRecipeProfile>();

            if (station.gameObject.name.Replace("(Clone)", string.Empty) != recipe.CraftingStation)
            {
                return false;
            }

            return station.GetLevel() >= recipe.MinimumStationLevel;
        }

        internal static bool CanLearn(Player player, HexBattleMageWandAttunementProfile definition)
        {
            if (!CanUseStation(player, definition))
            {
                return false;
            }

            if (HasLearned(player, definition.Element))
            {
                return false;
            }

            var recipe = definition.GetComponent<MageRecipeProfile>();

            if (!IsDiscovered(player, recipe))
            {
                return false;
            }

            foreach (var resource in recipe.GetResources())
            {
                var item = ObjectDB.instance.GetItemPrefab(resource.ItemPrefab).GetComponent<ItemDrop>().m_itemData;

                if (player.GetInventory().CountItems(item.m_shared.m_name) < resource.Amount)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool TryLearn(Player player, HexBattleMageWandAttunementProfile definition)
        {
            if (!CanLearn(player, definition))
            {
                return false;
            }

            foreach (var resource in definition.GetComponent<MageRecipeProfile>().GetResources())
            {
                var item = ObjectDB.instance.GetItemPrefab(resource.ItemPrefab).GetComponent<ItemDrop>().m_itemData;

                player.GetInventory().RemoveItem(item.m_shared.m_name, resource.Amount);
            }

            player.m_customData[LearnedPrefix + definition.Element] = "1";
            player.Message(MessageHud.MessageType.Center, ModLocalization.Format("$hexbattlemage_attunement_learned", ModLocalization.Format(definition.DisplayName)));
            return true;
        }

        internal static bool CanActivate(Player player, HexBattleMageWandAttunementProfile definition, WandElement? replacement)
        {
            if (!CanUseStation(player, definition))
            {
                return false;
            }

            if (!HasLearned(player, definition.Element))
            {
                return false;
            }

            var active = GetActive(player);

            if (active.Contains(definition.Element))
            {
                return false;
            }

            if (active.Count < MaximumActive)
            {
                return true;
            }

            return replacement.HasValue && active.Contains(replacement.Value);
        }

        internal static bool TryActivate(Player player, HexBattleMageWandAttunementProfile definition, WandElement? replacement)
        {
            if (!CanActivate(player, definition, replacement))
            {
                return false;
            }

            var active = GetActive(player);

            if (active.Count >= MaximumActive)
            {
                active.Remove(replacement.Value);
            }

            active.Add(definition.Element);
            int mask = active.Aggregate(0, (value, element) => value | (1 << (int)element));

            player.m_customData[ActiveKey] = mask.ToString(CultureInfo.InvariantCulture);

            foreach (var item in player.GetInventory().GetAllItems().Where(ItemRegistrationManager.IsWand))
            {
                ItemRegistrationManager.ApplyDamageSettings(item);
            }

            player.Message(MessageHud.MessageType.Center, ModLocalization.Format("$hexbattlemage_attunement_selected", ModLocalization.Format(definition.DisplayName)));
            return true;
        }
    }
}

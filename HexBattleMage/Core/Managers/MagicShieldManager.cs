using HexBattleMage.Assets;
using System;
using HarmonyLib;
using HexBattleMage.Core.Visuals;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HexBattleMage.Core.Managers
{
    internal static class MagicShieldManager
    {
        internal const string PrefabName = "HexMagicBracer";
        private const string VisualPrefabName = "HexMagicBracerVisual";
        private const int WoodCost = 10;
        private const int ResinCost = 4;
        private const int LeatherCost = 2;
        private const int WoodUpgradeCost = 5;
        private const int ResinUpgradeCost = 2;
        private const int LeatherUpgradeCost = 2;
        private static readonly Action<Humanoid> SetupAnimationState = AccessTools.MethodDelegate<Action<Humanoid>>(AccessTools.Method(typeof(Humanoid), "SetupAnimationState"));

        internal static void Initialize()
        {
            PrefabManager.OnVanillaPrefabsAvailable += RegisterShield;
        }

        internal static void Shutdown()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterShield;
        }

        internal static bool IsBracer(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.m_dropPrefab == null)
            {
                return false;
            }

            return item.m_dropPrefab.name == PrefabName;
        }

        internal static ItemDrop.ItemData GetBlocker(Humanoid humanoid)
        {
            var item = GetEquippedBracer(humanoid);

            return IsUsableBracer(item) ? item : null;
        }

        internal static ItemDrop.ItemData GetEquippedBracer(Humanoid humanoid)
        {
            foreach (var item in humanoid.GetInventory().GetAllItems())
            {
                if (!item.m_equipped)
                {
                    continue;
                }

                if (IsBracer(item))
                {
                    return item;
                }
            }

            return null;
        }

        internal static bool EquipBracer(Humanoid humanoid, ItemDrop.ItemData item)
        {
            if (!humanoid.GetInventory().ContainsItem(item))
            {
                return false;
            }

            if (!IsUsableBracer(item))
            {
                return false;
            }

            if (humanoid.IsDead())
            {
                return false;
            }

            if (humanoid.InAttack())
            {
                return false;
            }

            if (humanoid.InDodge())
            {
                return false;
            }

            foreach (var owned in humanoid.GetInventory().GetAllItems())
            {
                if (IsBracer(owned))
                {
                    owned.m_equipped = false;
                }
            }

            item.m_equipped = true;
            SetupAnimationState(humanoid);
            return true;
        }

        internal static void UnequipBracer(Humanoid humanoid, ItemDrop.ItemData item)
        {
            item.m_equipped = false;
            SetupAnimationState(humanoid);
        }

        internal static void RefreshAnimationState(Humanoid humanoid)
        {
            SetupAnimationState(humanoid);
        }

        internal static GameObject CreateWornVisual(Transform hand)
        {
            var source = AssetBundleHelper.Bundle.LoadAsset<GameObject>(VisualPrefabName);
            var visual = Object.Instantiate(source, hand, false);

            visual.name = "HexWornBracer";
            visual.AddComponent<MagicShieldVisual>();
            return visual;
        }

        private static bool IsUsableBracer(ItemDrop.ItemData item)
        {
            if (!IsBracer(item))
            {
                return false;
            }

            return !item.m_shared.m_useDurability || item.m_durability > 0f;
        }

        private static void RegisterShield()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterShield;

            var visualPrefab = AssetBundleHelper.Bundle.LoadAsset<GameObject>(VisualPrefabName);

            if (visualPrefab == null)
            {
                DebugLog.LogError($"Cannot register magic bracer: '{VisualPrefabName}' is missing from the bundle.");
                return;
            }

            var profile = visualPrefab.GetComponent<MagicBracerProfile>();

            if (profile == null)
            {
                DebugLog.LogError("Cannot register magic bracer: shield stat profile is missing from the bundle.");
                return;
            }

            var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, profile.BaseShieldPrefab);

            if (prefab == null)
            {
                DebugLog.LogError("Cannot clone the wooden shield for the magic bracer.");
                return;
            }

            for (int index = prefab.transform.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(prefab.transform.GetChild(index).gameObject);
            }

            var attach = Object.Instantiate(visualPrefab, prefab.transform, false);

            attach.name = "model";
            attach.SetActive(true);

            var collider = prefab.AddComponent<BoxCollider>();

            collider.size = new Vector3(0.15f, 0.2f, 0.15f);

            var itemDrop = prefab.GetComponent<ItemDrop>();
            var shared = itemDrop.m_itemData.m_shared;

            itemDrop.m_itemData.m_dropPrefab = prefab;
            shared.m_name = "$item_hexbattlemage_magicbracer";
            shared.m_description = "$item_hexbattlemage_magicbracer_description";
            shared.m_itemType = ItemDrop.ItemData.ItemType.Hands;
            shared.m_movementModifier = 0f;
            shared.m_variants = 1;
            shared.m_blockPower = profile.BlockPower;
            shared.m_blockPowerPerLevel = profile.BlockPowerPerQuality;
            shared.m_deflectionForce = profile.BlockForce;
            shared.m_deflectionForcePerLevel = profile.BlockForcePerQuality;
            shared.m_timedBlockBonus = profile.ParryBonus;
            shared.m_blockStaminaModifier = profile.BlockStaminaModifier;
            shared.m_maxDurability = profile.Durability;
            shared.m_durabilityPerLevel = profile.DurabilityPerQuality;
            shared.m_maxQuality = profile.MaximumQuality;
            itemDrop.m_itemData.m_variant = 0;

            var icon = AssetBundleHelper.Bundle.LoadAsset<Sprite>("MagicBracerIcon");

            if (icon != null)
            {
                shared.m_icons = new[] { icon };
            }

            var config = new ItemConfig
            {
                CraftingStation = CraftingStationManager.WorkbenchPrefabName,
                RepairStation = CraftingStationManager.WorkbenchPrefabName,
                MinStationLevel = 1,
                Amount = 1,
                Requirements = new[] { new RequirementConfig("Wood", WoodCost, WoodUpgradeCost), new RequirementConfig("Resin", ResinCost, ResinUpgradeCost), new RequirementConfig("LeatherScraps", LeatherCost, LeatherUpgradeCost) }
            };
            var item = new CustomItem(prefab, true, config);

            if (!ItemManager.Instance.AddItem(item))
            {
                DebugLog.LogError("Failed to register the magic bracer.");
                return;
            }

            MageEquipmentCatalog.Register(item.ItemDrop, item.Recipe.Recipe, Heightmap.Biome.Meadows);
            DebugLog.LogInfo("Wearable magic bracer registered with an independent bracer slot and native blocking.");
        }
    }
}

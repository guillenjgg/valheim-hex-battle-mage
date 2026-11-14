using HexBattleMage.Assets;
using Jotunn.Entities;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    internal static class ItemRegistrationManager
    {
        private const int BeechWandWoodCost = 20;
        private const int BeechWandResinCost = 2;
        private const int BaseStationLevel = 1;

        internal static bool RegisterItems()
        {
            var wandPrefab = AssetBundleHelper.HexBeechWand;
            var itemDrop = wandPrefab.GetComponent<ItemDrop>();

            if (itemDrop == null)
            {
                DebugLog.LogError($"Prefab '{wandPrefab.name}' is missing its ItemDrop component.");
                return false;
            }

            ApplySettings(itemDrop.m_itemData.m_shared.m_attack);
            itemDrop.m_itemData.m_shared.m_name = ModLocalization.BeechWandName;
            itemDrop.m_itemData.m_shared.m_description = ModLocalization.BeechWandDescription;

            return RegisterEquipment(wandPrefab, Heightmap.Biome.Meadows, new[] { new RequirementConfig("Wood", BeechWandWoodCost), new RequirementConfig("Resin", BeechWandResinCost) });
        }

        private static bool RegisterEquipment(GameObject prefab, Heightmap.Biome biome, RequirementConfig[] requirements)
        {
            var config = new ItemConfig
            {
                CraftingStation = CraftingStationManager.WorkbenchPrefabName,
                RepairStation = CraftingStationManager.WorkbenchPrefabName,
                MinStationLevel = BaseStationLevel,
                Amount = 1,
                Requirements = requirements
            };

            var item = new CustomItem(prefab, true, config);

            if (!ItemManager.Instance.AddItem(item))
            {
                DebugLog.LogError($"Failed to register item '{prefab.name}'.");
                return false;
            }

            MageEquipmentCatalog.Register(item.ItemDrop, item.Recipe.Recipe, biome);
            return true;
        }

        internal static bool IsWand(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.m_dropPrefab == null)
            {
                return false;
            }

            return item.m_dropPrefab.name == AssetBundleHelper.HexBeechWand.name;
        }

        internal static void ApplySettings(Attack attack)
        {
            attack.m_attackAnimation = DevConfig.AttackAnimation.Value;
            attack.m_attackChainLevels = DevConfig.AttackChainLevels.Value;
            attack.m_attackRandomAnimations = DevConfig.RandomAttackAnimations.Value;
            attack.m_attackProjectile = AssetBundleHelper.HexMagicBolt;
            attack.m_projectileVel = DevConfig.ProjectileVelocity.Value;
            attack.m_projectileAccuracy = DevConfig.ProjectileAccuracy.Value;
            attack.m_projectileAccuracyMin = DevConfig.ProjectileAccuracyMin.Value;
            attack.m_skillAccuracy = DevConfig.SkillAccuracy.Value;
        }
    }
}

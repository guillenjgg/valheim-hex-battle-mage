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

            var definition = item.m_dropPrefab.GetComponent<MageEquipmentDefinition>();

            if (definition == null)
            {
                return false;
            }

            if (definition.Category != MageEquipmentCategory.Bracer)
            {
                return false;
            }

            return definition.Slot == MageEquipmentSlot.Bracer;
        }

        internal static bool SupportsBlocking(ItemDrop.ItemData item)
        {
            if (!IsBracer(item))
            {
                return false;
            }

            return item.m_dropPrefab.GetComponent<MageEquipmentDefinition>().SupportsBlocking;
        }

        internal static ItemDrop.ItemData GetBlocker(Humanoid humanoid)
        {
            var item = GetEquippedBracer(humanoid);

            if (!SupportsBlocking(item))
            {
                return null;
            }

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
            var sourceDefinition = visualPrefab.GetComponent<MageEquipmentDefinition>();

            if (sourceDefinition == null)
            {
                DebugLog.LogError("Cannot register magic bracer: equipment definition is missing from the bundle.");
                return;
            }

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

            var definition = prefab.AddComponent<MageEquipmentDefinition>();

            definition.Category = sourceDefinition.Category;
            definition.Slot = sourceDefinition.Slot;
            definition.SupportsBlocking = sourceDefinition.SupportsBlocking;
            definition.DropCollisionDetection = sourceDefinition.DropCollisionDetection;
            definition.ProgressionTier = sourceDefinition.ProgressionTier;
            definition.CatalogBiome = sourceDefinition.CatalogBiome;

            var attach = Object.Instantiate(visualPrefab, prefab.transform, false);

            attach.name = "model";
            attach.SetActive(true);

            var collider = prefab.AddComponent<BoxCollider>();

            collider.size = profile.DropColliderSize;
            WorldDropPhysics.Configure(prefab, sourceDefinition.DropCollisionDetection);

            var itemDrop = prefab.GetComponent<ItemDrop>();
            var shared = itemDrop.m_itemData.m_shared;

            itemDrop.m_itemData.m_dropPrefab = prefab;
            shared.m_name = profile.DisplayName;
            shared.m_description = profile.Description;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Hands;
            shared.m_movementModifier = profile.MovementModifier;
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
            shared.m_weight = profile.Weight;
            shared.m_useDurability = profile.UseDurability;
            shared.m_useDurabilityDrain = profile.UseDurabilityDrain;
            shared.m_durabilityDrain = profile.DurabilityDrain;

            if (!profile.InheritNativeBlockEffects)
            {
                shared.m_blockEffect = new EffectList();
            }
            itemDrop.m_itemData.m_variant = 0;

            var icon = profile.Icon;

            if (icon != null)
            {
                shared.m_icons = new[] { icon };
            }

            var recipeProfile = PrefabSettings.Require<MageRecipeProfile>(visualPrefab);
            var requirements = PrefabSettings.GetRequirements(recipeProfile);
            var config = new ItemConfig
            {
                CraftingStation = recipeProfile.CraftingStation,
                RepairStation = recipeProfile.RepairStation,
                MinStationLevel = recipeProfile.MinimumStationLevel,
                Amount = recipeProfile.Amount,
                Requirements = requirements
            };
            var item = new CustomItem(prefab, true, config);

            if (item.Recipe == null)
            {
                Debug.LogError($"Cannot register '{prefab.name}': Jotunn did not create its authored recipe. Resource count: {config.Requirements.Length}.");
                return;
            }

            if (!ItemManager.Instance.AddItem(item))
            {
                DebugLog.LogError("Failed to register the magic bracer.");
                return;
            }

            MageEquipmentCatalog.Register(item.ItemDrop, item.Recipe.Recipe, (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), definition.CatalogBiome));
            DebugLog.LogInfo("Wearable magic bracer registered with an independent bracer slot and native blocking.");
        }
    }
}

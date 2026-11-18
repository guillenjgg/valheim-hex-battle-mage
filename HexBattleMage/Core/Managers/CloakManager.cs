using System;
using HarmonyLib;
using HexBattleMage.Assets;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HexBattleMage.Core.Managers
{
    internal static class CloakManager
    {
        private static readonly Func<PrefabManager, GameObject> GetPrefabContainer = AccessTools.MethodDelegate<Func<PrefabManager, GameObject>>(AccessTools.PropertyGetter(typeof(PrefabManager), "PrefabContainer"));
        private static readonly AccessTools.FieldRef<VisEquipment, int> CurrentModel = AccessTools.FieldRefAccess<VisEquipment, int>("m_currentModelIndex");
        private const int FemaleModelIndex = 1;

        internal static void Initialize()
        {
            PrefabManager.OnVanillaPrefabsAvailable += Register;
        }

        internal static void Shutdown()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;
        }

        private static void Register()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;

            var visual = AssetBundleHelper.Bundle.LoadAsset<GameObject>("HexBattleMage_Cloak_Meadows_01");

            if (visual == null)
            {
                DebugLog.LogError("Cloak visual is missing from the hexbattlemage bundle.");
                return;
            }

            var profile = PrefabSettings.Require<HexBattleMageCloakProfile>(visual);
            var recipe = PrefabSettings.Require<MageRecipeProfile>(visual);
            var eitr = PrefabSettings.Require<MageEitrProfile>(visual);
            var definition = PrefabSettings.Require<MageEquipmentDefinition>(visual);
            var basePrefab = PrefabManager.Instance.GetPrefab(profile.BaseItemPrefab);

            if (basePrefab == null)
            {
                throw new InvalidOperationException("Missing native cloak baseline: " + profile.BaseItemPrefab);
            }

            if (profile.Icon == null)
            {
                throw new InvalidOperationException("Cloak needs an authored inventory icon.");
            }

            if (profile.MaleVisual == null || profile.FemaleVisual == null || profile.WorldModel == null)
            {
                throw new InvalidOperationException("Cloak needs both body fits and a world model.");
            }

            var prefab = AssetManager.Instance.ClonePrefab(basePrefab, profile.ItemPrefabName, GetPrefabContainer(PrefabManager.Instance).transform);

            for (int index = prefab.transform.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(prefab.transform.GetChild(index).gameObject);
            }

            foreach (var collider in prefab.GetComponents<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            var authored = prefab.AddComponent<HexBattleMageCloakProfile>();

            foreach (var field in typeof(HexBattleMageCloakProfile).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                field.SetValue(authored, field.GetValue(profile));
            }

            var authoredEitr = prefab.AddComponent<MageEitrProfile>();

            authoredEitr.MaximumEitr = eitr.MaximumEitr;
            authoredEitr.RegenerationModifier = eitr.RegenerationModifier;

            var equipmentDefinition = prefab.AddComponent<MageEquipmentDefinition>();

            equipmentDefinition.Category = definition.Category;
            equipmentDefinition.Slot = definition.Slot;
            equipmentDefinition.ProgressionTier = definition.ProgressionTier;
            equipmentDefinition.CatalogBiome = definition.CatalogBiome;

            // Keep both fits inside the registered prefab so Jotunn resolves all material mocks.
            var sources = new GameObject("BodyFits");

            sources.transform.SetParent(prefab.transform, false);
            sources.SetActive(false);
            authored.MaleVisual = Object.Instantiate(profile.MaleVisual, sources.transform, false);
            authored.FemaleVisual = Object.Instantiate(profile.FemaleVisual, sources.transform, false);
            authored.MaleVisual.SetActive(true);
            authored.FemaleVisual.SetActive(true);

            var model = Object.Instantiate(profile.WorldModel, prefab.transform, false);

            model.name = "model";
            model.SetActive(true);

            var dropCollider = prefab.AddComponent<BoxCollider>();

            dropCollider.center = profile.DropColliderCenter;
            dropCollider.size = profile.DropColliderSize;

            var drop = prefab.GetComponent<ItemDrop>();
            var shared = drop.m_itemData.m_shared;

            drop.m_itemData.m_dropPrefab = prefab;
            shared.m_name = profile.DisplayName;
            shared.m_description = profile.Description;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Shoulder;
            shared.m_icons = new[] { profile.Icon };
            shared.m_weight = profile.Weight;
            shared.m_maxQuality = profile.MaximumQuality;
            shared.m_maxStackSize = 1;
            shared.m_armor = profile.Armor;
            shared.m_armorPerLevel = profile.ArmorPerQuality;
            shared.m_useDurability = profile.UseDurability;
            shared.m_maxDurability = profile.Durability;
            shared.m_durabilityPerLevel = profile.DurabilityPerQuality;
            shared.m_movementModifier = profile.MovementModifier;
            shared.m_equipStatusEffect = null;
            shared.m_setStatusEffect = null;
            drop.m_itemData.m_durability = profile.Durability;

            var config = new ItemConfig
            {
                CraftingStation = recipe.CraftingStation,
                RepairStation = recipe.RepairStation,
                MinStationLevel = recipe.MinimumStationLevel,
                Amount = recipe.Amount,
                Requirements = PrefabSettings.GetRequirements(recipe)
            };
            var item = new CustomItem(prefab, true, config);

            if (!ItemManager.Instance.AddItem(item))
            {
                throw new InvalidOperationException("Could not register cloak: " + profile.ItemPrefabName);
            }

            MageEquipmentCatalog.Register(item.ItemDrop, item.Recipe.Recipe, (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), definition.CatalogBiome));
            DebugLog.LogInfo("Cloak registered: " + profile.ItemPrefabName + "; native cape slot; male/female fits; authored Mage Table recipe.");
        }

        internal static GameObject ResolveArmorPrefab(GameObject prefab, VisEquipment equipment)
        {
            if (prefab == null)
            {
                return null;
            }

            var profile = prefab.GetComponent<HexBattleMageCloakProfile>();

            if (profile == null)
            {
                return prefab;
            }

            return CurrentModel(equipment) == FemaleModelIndex ? profile.FemaleVisual : profile.MaleVisual;
        }
    }
}

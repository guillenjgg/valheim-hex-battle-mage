using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HexBattleMage.Assets;
using HexBattleMage.Core.Visuals;
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

        private sealed class MaterialReferences
        {
            public Material[] Materials;
        }

        internal static void Initialize()
        {
            PrefabManager.OnVanillaPrefabsAvailable += Register;
        }

        internal static void Shutdown()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;
            GearUpgradeRules.Clear();
        }

        private static void Register()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;

            var visuals = AssetBundleHelper.Bundle.LoadAllAssets<GameObject>().Where(prefab => prefab.GetComponent<HexBattleMageCloakProfile>() != null).OrderBy(prefab => prefab.name).ToArray();

            foreach (var visual in visuals)
            {
                RegisterCloak(visual);
            }
        }

        private static void RegisterCloak(GameObject visual)
        {
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

            GearVitals.CopyProfile(visual, prefab);

            var authoredEitr = prefab.AddComponent<MageEitrProfile>();

            authoredEitr.MaximumEitr = eitr.MaximumEitr;
            authoredEitr.MaximumEitrPerQuality = eitr.MaximumEitrPerQuality;
            authoredEitr.RegenerationModifier = eitr.RegenerationModifier;

            var upgrades = visual.GetComponent<MageUpgradeProfile>();

            if (upgrades != null)
            {
                var registeredUpgrades = prefab.AddComponent<MageUpgradeProfile>();

                registeredUpgrades.UseFixedStationLevel = upgrades.UseFixedStationLevel;
                registeredUpgrades.RequiredStationLevel = upgrades.RequiredStationLevel;
                registeredUpgrades.FixedResourceCosts = upgrades.FixedResourceCosts;
            }

            var equipmentDefinition = prefab.AddComponent<MageEquipmentDefinition>();

            equipmentDefinition.Category = definition.Category;
            equipmentDefinition.Slot = definition.Slot;
            equipmentDefinition.ProgressionTier = definition.ProgressionTier;
            equipmentDefinition.CatalogBiome = definition.CatalogBiome;
            equipmentDefinition.CatalogRequiresBiomeDiscovery = definition.CatalogRequiresBiomeDiscovery;
            equipmentDefinition.CatalogBossDefeatKey = definition.CatalogBossDefeatKey;
            equipmentDefinition.DropCollisionDetection = definition.DropCollisionDetection;

            // Retain both fits so their material mocks can be resolved before equipment attachment.
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

            var styleSource = visual.GetComponent<MageArmorStyleProfile>();
            var bakedStyles = AssetBundleHelper.Bundle.LoadAsset<TextAsset>("Styles_" + profile.ItemPrefabName);
            MageArmorStyleProfile styles = null;

            if (styleSource != null || bakedStyles != null)
            {
                styles = prefab.AddComponent<MageArmorStyleProfile>();
                styles.RendererName = styleSource != null ? styleSource.RendererName : string.Empty;
                styles.Styles = styleSource != null ? styleSource.Styles : new MageArmorStyle[0];
                styles.BakedStyles = styleSource != null && styleSource.BakedStyles != null ? styleSource.BakedStyles : bakedStyles;
                styles.ApplyBakedDefaults(AssetBundleHelper.Bundle);

                if (styles.Styles.Length > 0)
                {
                    foreach (var fit in new[] { authored.MaleVisual, authored.FemaleVisual })
                    {
                        fit.transform.Find("attach_skin").gameObject.AddComponent<MageArmorStyleVisual>().Configure(styles);
                    }

                    model.AddComponent<MageArmorStyleVisual>().Configure(styles);
                }
            }

            var dropCollider = prefab.AddComponent<BoxCollider>();

            dropCollider.center = profile.DropColliderCenter;
            dropCollider.size = profile.DropColliderSize;
            WorldDropPhysics.Configure(prefab, definition.DropCollisionDetection);

            var drop = prefab.GetComponent<ItemDrop>();
            var shared = drop.m_itemData.m_shared;

            drop.m_itemData.m_dropPrefab = prefab;
            shared.m_name = profile.DisplayName;
            shared.m_description = profile.Description;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Shoulder;
            shared.m_variants = styles != null && styles.Styles.Length > 0 ? styles.Styles.Length : 1;
            shared.m_icons = styles != null && styles.Styles.Length > 0 ? styles.Styles.Select(style => style.Icon != null ? style.Icon : profile.Icon).ToArray() : new[] { profile.Icon };
            shared.m_weight = profile.Weight;
            shared.m_maxQuality = profile.MaximumQuality;
            shared.m_maxStackSize = 1;
            shared.m_armor = profile.Armor;
            shared.m_armorPerLevel = profile.ArmorPerQuality;
            shared.m_useDurability = profile.UseDurability;
            shared.m_maxDurability = profile.Durability;
            shared.m_durabilityPerLevel = profile.DurabilityPerQuality;
            shared.m_movementModifier = profile.MovementModifier;
            shared.m_damageModifiers = new List<HitData.DamageModPair>();

            if (profile.FrostResistance)
            {
                shared.m_damageModifiers.Add(new HitData.DamageModPair { m_type = HitData.DamageType.Frost, m_modifier = HitData.DamageModifier.Resistant });
            }

            shared.m_equipStatusEffect = null;
            shared.m_setStatusEffect = null;
            drop.m_itemData.m_durability = profile.Durability;

            var requirements = PrefabSettings.GetRequirements(recipe);
            var config = new ItemConfig
            {
                CraftingStation = recipe.CraftingStation,
                RepairStation = recipe.RepairStation,
                MinStationLevel = recipe.MinimumStationLevel,
                Amount = recipe.Amount,
                Requirements = requirements
            };
            // MagicaCloth runtime dictionaries are not mock assets; resolve only visual references.
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var references = new MaterialReferences { Materials = renderer.sharedMaterials };

                Jotunn.PrefabExtension.FixReferences(references);
                renderer.sharedMaterials = references.Materials;
            }

            var item = requirements.Length > 0 ? new CustomItem(prefab, false, config) : new CustomItem(prefab, false);

            if (!ItemManager.Instance.AddItem(item))
            {
                throw new InvalidOperationException("Could not register cloak: " + profile.ItemPrefabName);
            }

            if (item.Recipe != null)
            {
                if (upgrades != null)
                {
                    GearUpgradeRules.Register(item.Recipe.Recipe, prefab.GetComponent<MageUpgradeProfile>());
                }

                MageEquipmentCatalog.Register(item.ItemDrop, item.Recipe.Recipe, (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), definition.CatalogBiome));
            }

            DebugLog.LogInfo("Cloak registered: " + profile.ItemPrefabName + "; native cape slot; male/female fits; styles=" + shared.m_variants + "; max quality=" + shared.m_maxQuality + "; authored recipe resources: " + requirements.Length + ".");
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

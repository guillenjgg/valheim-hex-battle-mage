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

namespace HexBattleMage.Core.Managers
{
    internal static class ArmorManager
    {
        private static readonly AccessTools.FieldRef<VisEquipment, int> CurrentModel = AccessTools.FieldRefAccess<VisEquipment, int>("m_currentModelIndex");
        private const int FemaleModelIndex = 1;
        private static readonly Dictionary<int, MageArmorStyleProfile> HelmetStyles = new Dictionary<int, MageArmorStyleProfile>();
        internal static void Initialize()
        {
            PrefabManager.OnVanillaPrefabsAvailable += Register;
        }

        internal static void Shutdown()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;
            HelmetStyles.Clear();
        }

        private static void Register()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;

            var definitions = AssetBundleHelper.Bundle.LoadAllAssets<GameObject>().Where(asset => asset.GetComponent<HexBattleMageArmorProfile>() != null).OrderBy(asset => asset.name).ToArray();

            foreach (var definition in definitions)
            {
                RegisterArmor(definition);
            }
        }

        internal static void RegisterArmor(GameObject definition)
        {
            var profile = PrefabSettings.Require<HexBattleMageArmorProfile>(definition);
            var recipe = PrefabSettings.Require<MageRecipeProfile>(definition);
            var eitr = PrefabSettings.Require<MageEitrProfile>(definition);
            var equipment = PrefabSettings.Require<MageEquipmentDefinition>(definition);

            // Definition assets have a suffix so Jotunn's duplicate-name check cannot find them as items.
            var prefab = PrefabManager.Instance.CreateClonedPrefab(profile.ItemPrefabName, profile.BaseItemPrefab);

            if (prefab == null)
            {
                throw new InvalidOperationException("Could not clone armor baseline: " + profile.BaseItemPrefab + " as " + profile.ItemPrefabName);
            }

            var authored = prefab.AddComponent<HexBattleMageArmorProfile>();

            foreach (var field in typeof(HexBattleMageArmorProfile).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                field.SetValue(authored, field.GetValue(profile));
            }

            if (profile.ReplaceNativeVisuals)
            {
                ReplaceVisuals(prefab, authored);
            }

            var authoredEitr = prefab.AddComponent<MageEitrProfile>();

            authoredEitr.MaximumEitr = eitr.MaximumEitr;
            authoredEitr.MaximumEitrPerQuality = eitr.MaximumEitrPerQuality;
            authoredEitr.RegenerationModifier = eitr.RegenerationModifier;

            var authoredEquipment = prefab.AddComponent<MageEquipmentDefinition>();

            authoredEquipment.Category = equipment.Category;
            authoredEquipment.Slot = equipment.Slot;
            authoredEquipment.ProgressionTier = equipment.ProgressionTier;
            authoredEquipment.CatalogBiome = equipment.CatalogBiome;
            authoredEquipment.DropCollisionDetection = equipment.DropCollisionDetection;
            WorldDropPhysics.Configure(prefab, equipment.DropCollisionDetection);

            var styleSource = PrefabSettings.Require<MageArmorStyleProfile>(definition);
            var styles = prefab.AddComponent<MageArmorStyleProfile>();

            styles.RendererName = styleSource.RendererName;
            styles.Styles = styleSource.Styles;
            styles.BakedStyles = styleSource.BakedStyles != null ? styleSource.BakedStyles : AssetBundleHelper.Bundle.LoadAsset<TextAsset>("Styles_" + definition.name);
            styles.ApplyBakedDefaults(AssetBundleHelper.Bundle);

            if (styles.Styles.Length == 0)
            {
                throw new InvalidOperationException("Armor needs an authored appearance: " + profile.ItemPrefabName);
            }

            if (equipment.Slot != MageEquipmentSlot.Helmet && styles.Styles.Length > 1)
            {
                throw new InvalidOperationException("Multiple appearances currently require the helmet slot: " + profile.ItemPrefabName);
            }

            if (profile.ReplaceNativeVisuals)
            {
                authored.MaleVisual.transform.Find("attach_skin").gameObject.AddComponent<MageArmorStyleVisual>().Configure(styles);
                authored.FemaleVisual.transform.Find("attach_skin").gameObject.AddComponent<MageArmorStyleVisual>().Configure(styles);
                prefab.transform.Find("model").gameObject.AddComponent<MageArmorStyleVisual>().Configure(styles, true);
            }

            var drop = prefab.GetComponent<ItemDrop>();
            var shared = drop.m_itemData.m_shared;

            drop.m_itemData.m_dropPrefab = prefab;
            shared.m_name = profile.DisplayName;
            shared.m_description = profile.Description;
            shared.m_icons = styles.Styles.Select(style => style.Icon != null ? style.Icon : profile.Icon).ToArray();
            shared.m_variants = styles.Styles.Length;
            shared.m_weight = profile.Weight;
            shared.m_maxQuality = profile.MaximumQuality;
            shared.m_maxStackSize = 1;
            shared.m_armor = profile.Armor;
            shared.m_armorPerLevel = profile.ArmorPerQuality;
            shared.m_useDurability = profile.UseDurability;
            shared.m_maxDurability = profile.Durability;
            shared.m_durabilityPerLevel = profile.DurabilityPerQuality;
            shared.m_movementModifier = profile.MovementModifier;
            shared.m_setName = string.Empty;
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;
            shared.m_equipStatusEffect = null;
            shared.m_damageModifiers = new List<HitData.DamageModPair>();
            drop.m_itemData.m_durability = profile.Durability;

            if (profile.ClearBodyArmorMaterial)
            {
                shared.m_armorMaterial = null;
            }

            if (!profile.ReplaceNativeVisuals)
            {
                ApplyAppearance(prefab, profile, shared);
            }

            var requirements = PrefabSettings.GetRequirements(recipe);
            var config = new ItemConfig { CraftingStation = recipe.CraftingStation, RepairStation = recipe.RepairStation, MinStationLevel = recipe.MinimumStationLevel, Amount = recipe.Amount, Requirements = requirements };
            var item = new CustomItem(prefab, profile.ReplaceNativeVisuals, config);

            if (!ItemManager.Instance.AddItem(item))
            {
                throw new InvalidOperationException("Could not register armor: " + profile.ItemPrefabName);
            }

            if (equipment.Slot == MageEquipmentSlot.Helmet)
            {
                HelmetStyles[item.GetHashCode()] = styles;
            }

            if (item.Recipe != null)
            {
                MageEquipmentCatalog.Register(item.ItemDrop, item.Recipe.Recipe, (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), equipment.CatalogBiome));
            }

            DebugLog.LogInfo("Armor registered: " + profile.ItemPrefabName + "; Jotunn clone of " + profile.BaseItemPrefab + "; armor=" + shared.m_armor + "; Eitr=" + eitr.MaximumEitr + ".");
        }

        internal static MageArmorStyleProfile GetHelmetStyles(int hash)
        {
            return HelmetStyles.TryGetValue(hash, out var styles) ? styles : null;
        }

        internal static GameObject ResolveVisualPrefab(GameObject prefab, VisEquipment equipment)
        {
            if (prefab == null)
            {
                return null;
            }

            var profile = prefab.GetComponent<HexBattleMageArmorProfile>();

            if (profile == null)
            {
                return prefab;
            }

            if (!profile.ReplaceNativeVisuals)
            {
                return prefab;
            }

            return CurrentModel(equipment) == FemaleModelIndex ? profile.FemaleVisual : profile.MaleVisual;
        }

        private static void ReplaceVisuals(GameObject prefab, HexBattleMageArmorProfile profile)
        {
            if (profile.MaleVisual == null || profile.FemaleVisual == null || profile.WorldModel == null)
            {
                throw new InvalidOperationException("Custom armor needs both body fits and a world model: " + profile.ItemPrefabName);
            }

            for (int index = prefab.transform.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(prefab.transform.GetChild(index).gameObject);
            }

            var fits = new GameObject("BodyFits");

            fits.transform.SetParent(prefab.transform, false);
            fits.SetActive(false);
            profile.MaleVisual = UnityEngine.Object.Instantiate(profile.MaleVisual, fits.transform, false);
            profile.FemaleVisual = UnityEngine.Object.Instantiate(profile.FemaleVisual, fits.transform, false);
            profile.MaleVisual.SetActive(true);
            profile.FemaleVisual.SetActive(true);

            var model = UnityEngine.Object.Instantiate(profile.WorldModel, prefab.transform, false);

            model.name = "model";
            model.SetActive(true);

            foreach (var collider in prefab.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var dropCollider = prefab.AddComponent<BoxCollider>();

            dropCollider.center = profile.DropColliderCenter;
            dropCollider.size = profile.DropColliderSize;
        }

        private static void ApplyAppearance(GameObject prefab, HexBattleMageArmorProfile profile, ItemDrop.ItemData.SharedData shared)
        {
            if (profile.BodyTexture != null && !string.IsNullOrEmpty(profile.BodyTextureProperty) && shared.m_armorMaterial != null)
            {
                shared.m_armorMaterial = new Material(shared.m_armorMaterial);
                shared.m_armorMaterial.SetTexture(profile.BodyTextureProperty, profile.BodyTexture);
            }

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;

                for (int index = 0; index < materials.Length; index++)
                {
                    if (materials[index] == null)
                    {
                        continue;
                    }

                    var material = new Material(materials[index]);

                    material.color = profile.MeshTint;

                    if (profile.ReplaceMeshTexture && profile.BodyTexture != null)
                    {
                        material.mainTexture = profile.BodyTexture;
                    }

                    materials[index] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }
    }
}

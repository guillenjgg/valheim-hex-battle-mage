using HexBattleMage.Assets;
using Jotunn.Entities;
using HexBattleMage.Core.Visuals;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;
using System;
using Object = UnityEngine.Object;

namespace HexBattleMage.Core.Managers
{
    internal static class ItemRegistrationManager
    {
        private const string IconShaderName = "Unlit/Texture";
        private const string FallbackIconShaderName = "Sprites/Default";

        internal static void Initialize()
        {
            PrefabManager.OnVanillaPrefabsAvailable += RegisterWhenReady;
        }

        internal static void Shutdown()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterWhenReady;
            PrefabManager.OnPrefabsRegistered -= CreateWandStyleIcons;
        }

        private static void RegisterWhenReady()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterWhenReady;

            if (!RegisterItems())
            {
                PrefabManager.OnPrefabsRegistered -= CreateWandStyleIcons;
                Plugin.Log.LogError("Wand registration failed after vanilla prefabs became available.");
            }
        }

        internal static bool RegisterItems()
        {
            var wandPrefab = AssetBundleHelper.HexBattleMageWand;
            var itemDrop = wandPrefab.GetComponent<ItemDrop>();

            if (itemDrop == null)
            {
                DebugLog.LogError($"Prefab '{wandPrefab.name}' is missing its ItemDrop component.");
                return false;
            }

            ApplyDamageSettings(itemDrop.m_itemData);
            ApplySettings(itemDrop.m_itemData.m_shared.m_attack);
            WandTipLight.Configure(wandPrefab);
            wandPrefab.transform.Find("attach").gameObject.AddComponent<WandStyle>();
            itemDrop.m_itemData.m_shared.m_variants = WandStyle.StyleCount;

            var defaultIcon = itemDrop.m_itemData.m_shared.m_icons[0];
            var styleIcons = new Sprite[WandStyle.StyleCount];

            for (int variant = 0; variant < styleIcons.Length; variant++)
            {
                styleIcons[variant] = defaultIcon;
            }

            itemDrop.m_itemData.m_shared.m_icons = styleIcons;
            WandStyle.EnableEquipmentVariants();
            PrefabManager.OnPrefabsRegistered += CreateWandStyleIcons;

            return RegisterEquipment(wandPrefab, (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), PrefabSettings.Require<MageEquipmentDefinition>(wandPrefab).CatalogBiome), PrefabSettings.GetRequirements(PrefabSettings.Require<MageRecipeProfile>(wandPrefab)));
        }

        private static void CreateWandStyleIcons()
        {
            PrefabManager.OnPrefabsRegistered -= CreateWandStyleIcons;

            var prefab = AssetBundleHelper.HexBattleMageWand;
            var style = prefab.GetComponentInChildren<WandStyle>();
            var materials = PrefabSettings.Require<HexBattleMageWandProfile>(prefab).StyleMaterials;

            style.ConfigureMaterials(materials);
            DebugLog.LogInfo("Apprentice wand styles configured: warm bark, dark weathered bark, and pale birch.");

            if (GUIManager.IsHeadless())
            {
                return;
            }

            var shared = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;

            for (int variant = 0; variant < WandStyle.StyleCount; variant++)
            {
                style.Setup(variant);

                var icon = RenderWandStyleIcon(prefab);

                if (icon != null)
                {
                    shared.m_icons[variant] = icon;
                }
            }

            style.Setup(0);
        }

        private static Sprite RenderWandStyleIcon(GameObject prefab)
        {
            var shader = Shader.Find(IconShaderName);

            if (shader == null)
            {
                shader = Shader.Find(FallbackIconShaderName);
            }

            if (shader == null)
            {
                DebugLog.LogWarning("No neutral shader is available for wand style icons.");
                return null;
            }

            var model = prefab.transform.Find("attach/WandModel");
            var sourceMaterial = model.GetComponent<MeshRenderer>().sharedMaterial;
            var material = new Material(shader);
            var preview = new GameObject("WandStylePreview");

            preview.SetActive(false);

            try
            {
                material.mainTexture = sourceMaterial.mainTexture;
                material.mainTextureScale = sourceMaterial.mainTextureScale;
                material.mainTextureOffset = sourceMaterial.mainTextureOffset;

                var meshObject = new GameObject("WandModel");

                meshObject.transform.SetParent(preview.transform, false);
                meshObject.transform.localRotation = Quaternion.Inverse(prefab.transform.rotation) * model.rotation;
                meshObject.transform.localScale = model.lossyScale;
                meshObject.AddComponent<MeshFilter>().sharedMesh = model.GetComponent<MeshFilter>().sharedMesh;
                meshObject.AddComponent<MeshRenderer>().sharedMaterial = material;
                preview.SetActive(true);

                var request = new RenderManager.RenderRequest(preview)
                {
                    Rotation = RenderManager.IsometricRotation,
                    UseCache = false
                };

                return RenderManager.Instance.Render(request);
            }
            finally
            {
                Object.Destroy(preview);
                Object.Destroy(material);
            }
        }

        private static bool RegisterEquipment(GameObject prefab, Heightmap.Biome biome, RequirementConfig[] requirements)
        {
            var recipeProfile = PrefabSettings.Require<MageRecipeProfile>(prefab);

            if (requirements.Length == 0)
            {
                Plugin.Log.LogError($"Cannot register '{prefab.name}': authored recipe data is unavailable after prefab loading. Profile: {JsonUtility.ToJson(recipeProfile)}");
                return false;
            }

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
                Plugin.Log.LogError($"Cannot register '{prefab.name}': Jotunn did not create its recipe from {requirements.Length} authored resources.");
                return false;
            }

            if (!ItemManager.Instance.AddItem(item))
            {
                DebugLog.LogError($"Failed to register item '{prefab.name}'.");
                return false;
            }

            MageEquipmentCatalog.Register(item.ItemDrop, item.Recipe.Recipe, biome);
            Plugin.Log.LogInfo($"Wand registered after prefab loading with {requirements.Length} authored recipe resources.");
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

            return item.m_dropPrefab.name == AssetBundleHelper.HexBattleMageWand.name;
        }

        internal static void ApplyDamageSettings(ItemDrop.ItemData item)
        {
            item.m_shared.m_damages.m_blunt = DevConfig.WandBluntDamage.Value;
            item.m_shared.m_damages.m_pierce = DevConfig.WandPierceDamage.Value;
            Elements.WandElementManager.ApplyToItem(item);
        }

        internal static void ApplySettings(Attack attack)
        {
            attack.m_attackAnimation = DevConfig.AttackAnimation.Value;
            attack.m_attackChainLevels = DevConfig.AttackChainLevels.Value;
            attack.m_attackRandomAnimations = DevConfig.RandomAttackAnimations.Value;
            attack.m_attackProjectile = PrefabSettings.Wand.ProjectilePrefab;
            attack.m_projectileVel = DevConfig.ProjectileVelocity.Value;
            attack.m_projectileAccuracy = DevConfig.ProjectileAccuracy.Value;
            attack.m_projectileAccuracyMin = DevConfig.ProjectileAccuracyMin.Value;
            attack.m_skillAccuracy = DevConfig.SkillAccuracy.Value;
        }
    }
}

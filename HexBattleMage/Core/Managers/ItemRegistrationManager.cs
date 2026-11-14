using HexBattleMage.Assets;
using Jotunn.Entities;
using HexBattleMage.Core.Visuals;
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
        private const string ApprenticeMaterialDirectory = "Assets/_CustomItems/HexBattleMage/Transmogs/Wands/Apprentice/Materials/";
        private const string DarkBarkMaterialPath = ApprenticeMaterialDirectory + "ApprenticeDarkBark.mat";
        private const string BirchMaterialPath = ApprenticeMaterialDirectory + "ApprenticeBirch.mat";
        private const string IconShaderName = "Unlit/Texture";
        private const string FallbackIconShaderName = "Sprites/Default";

        internal static bool RegisterItems()
        {
            var wandPrefab = AssetBundleHelper.HexBattleMageWand;
            var itemDrop = wandPrefab.GetComponent<ItemDrop>();

            if (itemDrop == null)
            {
                DebugLog.LogError($"Prefab '{wandPrefab.name}' is missing its ItemDrop component.");
                return false;
            }

            ApplySettings(itemDrop.m_itemData.m_shared.m_attack);
            itemDrop.m_itemData.m_shared.m_name = ModLocalization.BeechWandName;
            itemDrop.m_itemData.m_shared.m_description = ModLocalization.BeechWandDescription;
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

            return RegisterEquipment(wandPrefab, Heightmap.Biome.Meadows, new[] { new RequirementConfig("Wood", BeechWandWoodCost), new RequirementConfig("Resin", BeechWandResinCost) });
        }

        private static void CreateWandStyleIcons()
        {
            PrefabManager.OnPrefabsRegistered -= CreateWandStyleIcons;

            var prefab = AssetBundleHelper.HexBattleMageWand;
            var style = prefab.GetComponentInChildren<WandStyle>();
            var alternateMaterial = AssetBundleHelper.Bundle.LoadAsset<Material>(DarkBarkMaterialPath);

            if (alternateMaterial == null)
            {
                DebugLog.LogError($"Wand style material '{DarkBarkMaterialPath}' is missing from the imported bundle.");
                return;
            }

            var birchMaterial = AssetBundleHelper.Bundle.LoadAsset<Material>(BirchMaterialPath);

            if (birchMaterial == null)
            {
                DebugLog.LogError($"Wand style material '{BirchMaterialPath}' is missing from the imported bundle.");
                return;
            }

            style.ConfigureMaterials(alternateMaterial, birchMaterial);
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

            return item.m_dropPrefab.name == AssetBundleHelper.HexBattleMageWand.name;
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

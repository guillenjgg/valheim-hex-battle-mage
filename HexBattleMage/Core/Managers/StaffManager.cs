using System;
using System.Linq;
using HexBattleMage.Assets;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HexBattleMage.Core.Managers
{
    internal static class StaffManager
    {
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

            foreach (var definition in AssetBundleHelper.Bundle.LoadAllAssets<GameObject>().Where(asset => asset.GetComponent<HexBattleMageStaffProfile>() != null))
            {
                RegisterStaff(definition);
            }
        }

        internal static void RegisterStaff(GameObject definition)
        {
            var profile = PrefabSettings.Require<HexBattleMageStaffProfile>(definition);
            var prefab = PrefabManager.Instance.CreateClonedPrefab(profile.ItemPrefabName, profile.BaseItemPrefab);
            var drop = prefab.GetComponent<ItemDrop>();
            var shared = drop.m_itemData.m_shared;
            var projectile = PrefabManager.Instance.CreateClonedPrefab(profile.ItemPrefabName + "_Projectile", shared.m_attack.m_attackProjectile);
            var flight = projectile.GetComponent<Projectile>();

            if (profile.SuppressNativeCinders)
            {
                foreach (var spawner in projectile.GetComponents<CinderSpawner>())
                {
                    Object.DestroyImmediate(spawner);
                }

                flight.m_onHit = null;
            }

            ClearChildren(projectile);
            var rock = Object.Instantiate(profile.ProjectileModel, projectile.transform, false);

            rock.transform.localScale *= profile.ProjectileVisualScale;
            rock.SetActive(true);
            flight.m_visual = rock;
            flight.m_hideOnHit = rock;
            flight.m_statusEffect = string.Empty;
            flight.m_rayRadius = profile.ProjectileCollisionRadius;
            flight.m_aoe = 0f;
            flight.m_gravity = profile.ProjectileGravity;
            flight.m_hitEffects = new EffectList();
            flight.m_hitWaterEffects = new EffectList();
            flight.m_spawnOnHitEffects = new EffectList();
            flight.m_spawnOnHit = null;
            flight.m_randomSpawnOnHit.Clear();
            flight.m_spawnOnTtl = false;
            ClearChildren(prefab);
            var worn = Object.Instantiate(profile.WornModel, prefab.transform, false);

            worn.name = "attach";
            worn.SetActive(false);
            var model = Object.Instantiate(profile.WornModel, prefab.transform, false);

            model.name = "model";
            model.SetActive(true);

            foreach (var collider in prefab.GetComponents<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            var body = prefab.AddComponent<BoxCollider>();

            body.center = profile.DropColliderCenter;
            body.size = profile.DropColliderSize;
            WorldDropPhysics.Configure(prefab, CollisionDetectionMode.ContinuousDynamic);
            shared.m_name = profile.DisplayName;
            shared.m_description = profile.Description;
            shared.m_icons = new[] { profile.Icon };
            shared.m_variants = 1;
            shared.m_weight = profile.Weight;
            shared.m_toolTier = profile.ToolTier;
            shared.m_maxQuality = profile.MaximumQuality;
            shared.m_maxDurability = profile.Durability;
            shared.m_durabilityPerLevel = profile.DurabilityPerQuality;
            shared.m_skillType = profile.Skill;
            shared.m_damages = new HitData.DamageTypes { m_blunt = profile.BluntDamage, m_pickaxe = profile.PickaxeDamage };
            shared.m_damagesPerLevel = new HitData.DamageTypes { m_blunt = profile.BluntPerQuality, m_pickaxe = profile.PickaxePerQuality };
            shared.m_startEffect = new EffectList();
            shared.m_triggerEffect = new EffectList();
            shared.m_hitEffect = new EffectList();
            shared.m_hitTerrainEffect = new EffectList();
            shared.m_attackStatusEffect = null;
            shared.m_attackStatusEffectChance = 0f;
            shared.m_attack.m_attackStamina = profile.StaminaCost;
            shared.m_attack.m_attackEitr = profile.EitrCost;
            shared.m_attack.m_attackProjectile = projectile;
            shared.m_attack.m_projectileVel = profile.ProjectileSpeed;
            shared.m_attack.m_projectileAccuracy = 0f;
            shared.m_attack.m_projectileAccuracyMin = 0f;
            shared.m_attack.m_skillAccuracy = false;
            shared.m_attack.m_raiseSkillAmount = 1f;
            shared.m_secondaryAttack = new Attack();
            drop.m_itemData.m_dropPrefab = prefab;
            var sourceEquipment = PrefabSettings.Require<MageEquipmentDefinition>(definition);
            var equipment = prefab.AddComponent<MageEquipmentDefinition>();

            equipment.Category = sourceEquipment.Category;
            equipment.Slot = sourceEquipment.Slot;
            equipment.ProgressionTier = sourceEquipment.ProgressionTier;
            equipment.CatalogBiome = sourceEquipment.CatalogBiome;
            equipment.CatalogRequiresBiomeDiscovery = sourceEquipment.CatalogRequiresBiomeDiscovery;
            equipment.CatalogBossDefeatKey = sourceEquipment.CatalogBossDefeatKey;
            GearUpgradeRules.CopyProfile(definition, prefab);
            var recipe = PrefabSettings.Require<MageRecipeProfile>(definition);
            var item = new CustomItem(prefab, true, new ItemConfig { CraftingStation = recipe.CraftingStation, RepairStation = recipe.RepairStation, MinStationLevel = recipe.MinimumStationLevel, Requirements = PrefabSettings.GetRequirements(recipe) });

            if (!ItemManager.Instance.AddItem(item))
            {
                throw new InvalidOperationException("Could not register staff: " + profile.ItemPrefabName);
            }

            GearUpgradeRules.Register(item.Recipe.Recipe, prefab.GetComponent<MageUpgradeProfile>());
            MageEquipmentCatalog.Register(item.ItemDrop, item.Recipe.Recipe, Heightmap.Biome.BlackForest);
            DebugLog.LogInfo("Earth staff registered: " + profile.ItemPrefabName);
        }

        private static void ClearChildren(GameObject prefab)
        {
            for (int index = prefab.transform.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(prefab.transform.GetChild(index).gameObject);
            }
        }
    }
}

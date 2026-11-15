using HexBattleMage.Assets;
using HexBattleMage.Core.Visuals;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    internal static class ProjectileManager
    {
        private static Projectile _projectile;

        internal static bool RegisterProjectiles()
        {
            var projectilePrefab = AssetBundleHelper.HexMagicBolt;
            var projectile = projectilePrefab.GetComponent<Projectile>();

            if (projectile == null)
            {
                DebugLog.LogError($"Prefab '{projectilePrefab.name}' is missing its Projectile component.");
                return false;
            }

            foreach (var particles in projectilePrefab.GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var emission = particles.emission;

                emission.enabled = false;
            }

            foreach (var renderer in projectilePrefab.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            foreach (var light in projectilePrefab.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
            }

            projectile.m_hitEffects = new EffectList();
            projectile.m_hitWaterEffects = new EffectList();
            projectilePrefab.AddComponent<WandProjectileImpact>();

            var visual = new GameObject("LightningBoltVisual");

            visual.transform.SetParent(projectilePrefab.transform, false);
            visual.AddComponent<LightningBoltVisual>();
            projectile.m_visual = visual;
            projectile.m_hideOnHit = visual;
            _projectile = projectile;
            ApplySettings();
            PrefabManager.OnVanillaPrefabsAvailable += PrepareLightningMaterial;

            var magicBolt = new CustomPrefab(projectilePrefab, fixReference: true);

            PrefabManager.Instance.AddPrefab(magicBolt);
            return true;
        }

        private static void PrepareLightningMaterial()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= PrepareLightningMaterial;
            LightningBoltVisual.PrepareMaterial();
        }

        internal static void ApplySettings()
        {
            _projectile.m_aoe = DevConfig.AreaOfEffectRadius.Value;
            _projectile.m_onlyStopOnTerrain = DevConfig.OnlyStopOnTerrain.Value;
            _projectile.m_rayRadius = DevConfig.CollisionRadius.Value;
        }
    }
}

using HexBattleMage.Assets;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    internal static class ProjectileManager
    {
        private const string SparkEffectName = "SparkEffect";
        private static Projectile _projectile;
        private static ParticleSystem _sparkEffect;
        private static ParticleSystemRenderer _sparkRenderer;

        internal static bool RegisterProjectiles()
        {
            var projectilePrefab = AssetBundleHelper.HexMagicBolt;
            var projectile = projectilePrefab.GetComponent<Projectile>();

            if (projectile == null)
            {
                DebugLog.LogError($"Prefab '{projectilePrefab.name}' is missing its Projectile component.");
                return false;
            }

            var sparkTransform = projectilePrefab.transform.Find(SparkEffectName);

            if (sparkTransform == null)
            {
                DebugLog.LogError($"Prefab '{projectilePrefab.name}' is missing '{SparkEffectName}'.");
                return false;
            }

            var sparkEffect = sparkTransform.GetComponent<ParticleSystem>();

            if (sparkEffect == null)
            {
                DebugLog.LogError($"'{SparkEffectName}' is missing its ParticleSystem component.");
                return false;
            }

            var sparkRenderer = sparkTransform.GetComponent<ParticleSystemRenderer>();

            if (sparkRenderer == null)
            {
                DebugLog.LogError($"'{SparkEffectName}' is missing its ParticleSystemRenderer component.");
                return false;
            }

            _projectile = projectile;
            _sparkEffect = sparkEffect;
            _sparkRenderer = sparkRenderer;
            ApplySettings();

            var magicBolt = new CustomPrefab(projectilePrefab, fixReference: true);

            PrefabManager.Instance.AddPrefab(magicBolt);
            return true;
        }

        internal static void ApplySettings()
        {
            var main = _sparkEffect.main;

            main.startSize = DevConfig.SparkSize.Value;
            main.simulationSpace = DevConfig.SparkSimulationSpace.Value == ParticleSystemSimulationSpace.Custom ? ParticleSystemSimulationSpace.World : DevConfig.SparkSimulationSpace.Value;

            var emission = _sparkEffect.emission;

            emission.enabled = DevConfig.SparkEmissionEnabled.Value;
            emission.rateOverTime = DevConfig.SparksPerSecond.Value;
            emission.rateOverDistance = DevConfig.SparksPerMeter.Value;
            _sparkRenderer.renderMode = DevConfig.SparkRenderMode.Value;

            _projectile.m_aoe = DevConfig.AreaOfEffectRadius.Value;
            _projectile.m_onlyStopOnTerrain = DevConfig.OnlyStopOnTerrain.Value;
            _projectile.m_rayRadius = DevConfig.CollisionRadius.Value;
        }
    }
}

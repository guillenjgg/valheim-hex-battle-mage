using HexBattleMage.Assets;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    internal static class ProjectileManager
    {
        private const string SparkEffectName = "SparkEffect";
        private const float SparkStartSize = 2f;
        private const float AreaOfEffectRadius = 3f;
        private const float CollisionRadius = 0.25f;

        internal static bool RegisterProjectiles()
        {
            var projectilePrefab = AssetBundleHelper.HexMagicBolt;
            var projectile = projectilePrefab.GetComponent<Projectile>();

            if (projectile == null)
            {
                Plugin.Log.LogError($"Prefab '{projectilePrefab.name}' is missing its Projectile component.");
                return false;
            }

            var sparkTransform = projectilePrefab.transform.Find(SparkEffectName);

            if (sparkTransform == null)
            {
                Plugin.Log.LogError($"Prefab '{projectilePrefab.name}' is missing '{SparkEffectName}'.");
                return false;
            }

            var sparkEffect = sparkTransform.GetComponent<ParticleSystem>();

            if (sparkEffect == null)
            {
                Plugin.Log.LogError($"'{SparkEffectName}' is missing its ParticleSystem component.");
                return false;
            }

            var main = sparkEffect.main;

            main.startSize = SparkStartSize;
            projectile.m_aoe = AreaOfEffectRadius;
            projectile.m_onlyStopOnTerrain = false;
            projectile.m_rayRadius = CollisionRadius;

            var magicBolt = new CustomPrefab(projectilePrefab, fixReference: true);

            PrefabManager.Instance.AddPrefab(magicBolt);
            return true;
        }
    }
}

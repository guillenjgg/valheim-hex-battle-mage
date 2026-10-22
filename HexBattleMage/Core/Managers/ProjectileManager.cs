
using HexBattleMage.Assets;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HexBattleMage.Core
{
    internal static class ProjectileManager
    {
        internal static void RegisterProjectiles()
        {
            var projectilePrefab = AssetBundleHelper.HexMagicBolt;
            var sparkEffect = projectilePrefab.transform.Find("SparkEffect").GetComponent<ParticleSystem>();
            var main = sparkEffect.main;
            main.startSize = 2f;

            var projectile = projectilePrefab.GetComponent<Projectile>();

            projectile.m_aoe = 3f;
            projectile.m_onlyStopOnTerrain = false;
            projectile.m_rayRadius = 0.25f;

            var magicBolt = new CustomPrefab(projectilePrefab, fixReference: true);

            PrefabManager.Instance.AddPrefab(magicBolt);
        }
    }
}

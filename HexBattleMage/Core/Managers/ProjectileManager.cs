using HexBattleMage.Assets;
using Jotunn.Entities;
using Jotunn.Managers;

namespace HexBattleMage.Core
{
    internal static class ProjectileManager
    {
        internal static void RegisterProjectiles()
        {
            var projectilePrefab = AssetBundleHelper.HexMagicBolt;
            var projectile = projectilePrefab.GetComponent<Projectile>();

            projectile.m_aoe = 3f;
            projectile.m_onlyStopOnTerrain = false;

            var magicBolt = new CustomPrefab(projectilePrefab, fixReference: true);

            PrefabManager.Instance.AddPrefab(magicBolt);

            Plugin.Log.LogInfo("HexMagicBolt registered.");
        }
    }
}
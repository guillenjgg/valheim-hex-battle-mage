using HexBattleMage.Assets;
using Jotunn.Entities;
using Jotunn.Managers;

namespace HexBattleMage.Core
{
    internal static class ProjectileManager
    {
        internal static void RegisterProjectiles()
        {
            var magicBolt = new CustomPrefab(AssetBundleHelper.HexMagicBolt, fixReference: true);

            PrefabManager.Instance.AddPrefab(magicBolt);

            Plugin.Log.LogInfo("HexMagicBolt registered.");
        }
    }
}
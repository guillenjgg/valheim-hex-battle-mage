using Jotunn.Utils;
using UnityEngine;
using HexBattleMage.Core;

namespace HexBattleMage.Assets
{
    internal static class AssetBundleHelper
    {
        private const string BundleName = "hexbattlemage";
        private const string WandPrefabName = "HexBattleMageWand";
        private const string LegacyWandPrefabName = "HexBeechWand";
        private const string ProjectilePrefabName = "HexMagicBolt";
        private const string WorkbenchPrefabName = "HexArcaneWorkbench";

        internal static AssetBundle Bundle { get; private set; }
        internal static GameObject HexBattleMageWand { get; private set; }
        internal static GameObject HexMagicBolt { get; private set; }
        internal static GameObject HexArcaneWorkbench { get; private set; }

        internal static bool LoadAssets()
        {
            Bundle = AssetUtils.LoadAssetBundleFromResources(BundleName);

            if (Bundle == null)
            {
                DebugLog.LogError("Failed to load HexBattleMage asset bundle.");
                return false;
            }

            HexBattleMageWand = Bundle.LoadAsset<GameObject>(WandPrefabName);

            if (HexBattleMageWand == null)
            {
                HexBattleMageWand = Bundle.LoadAsset<GameObject>(LegacyWandPrefabName);
            }

            if (HexBattleMageWand == null)
            {
                DebugLog.LogError($"Failed to load prefab '{WandPrefabName}'.");
                return false;
            }

            // Keep the registered identity so existing inventory items and world drops still resolve.
            HexBattleMageWand.name = LegacyWandPrefabName;

            HexMagicBolt = Bundle.LoadAsset<GameObject>(ProjectilePrefabName);

            if (HexMagicBolt == null)
            {
                DebugLog.LogError($"Failed to load prefab '{ProjectilePrefabName}'.");
                return false;
            }

            HexArcaneWorkbench = Bundle.LoadAsset<GameObject>(WorkbenchPrefabName);

            if (HexArcaneWorkbench == null)
            {
                DebugLog.LogError($"Failed to load prefab '{WorkbenchPrefabName}'.");
                return false;
            }

            DebugLog.LogInfo("HexBattleMage assets loaded.");
            return true;
        }
    }
}

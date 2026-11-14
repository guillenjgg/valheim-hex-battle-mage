using Jotunn.Utils;
using UnityEngine;
using HexBattleMage.Core;

namespace HexBattleMage.Assets
{
    internal static class AssetBundleHelper
    {
        private const string BundleName = "hexbattlemage";
        private const string WandPrefabName = "HexBeechWand";
        private const string ProjectilePrefabName = "HexMagicBolt";
        private const string WorkbenchPrefabName = "HexArcaneWorkbench";

        internal static AssetBundle Bundle { get; private set; }
        internal static GameObject HexBeechWand { get; private set; }
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

            HexBeechWand = Bundle.LoadAsset<GameObject>(WandPrefabName);

            if (HexBeechWand == null)
            {
                DebugLog.LogError($"Failed to load prefab '{WandPrefabName}'.");
                return false;
            }

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

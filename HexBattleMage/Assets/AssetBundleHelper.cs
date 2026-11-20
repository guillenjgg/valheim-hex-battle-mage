using Jotunn.Utils;
using UnityEngine;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Assets
{
    internal static class AssetBundleHelper
    {
        private const string BundleName = "hexbattlemage";
        private const string WandPrefabName = "HexBattleMage_Wand_Meadows_01";
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

            if (Bundle.LoadAsset<GameObject>(HexBattleMageRuneBlockProfile.PrefabName) == null)
            {
                DebugLog.LogError("Rune-Whittler's Block is missing. Rebuild the hexbattlemage bundle and mod DLL.");
                return false;
            }

            DebugLog.LogInfo("HexBattleMage assets loaded.");
            return true;
        }
    }
}

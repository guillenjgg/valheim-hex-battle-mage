using Jotunn.Utils;
using UnityEngine;

namespace HexBattleMage.Assets
{
    internal static class AssetBundleHelper
    {
        private const string BundleName = "hexbattlemage";
        private const string WandPrefabName = "HexBeechWand";
        private const string ProjectilePrefabName = "HexMagicBolt";

        internal static AssetBundle Bundle { get; private set; }
        internal static GameObject HexBeechWand { get; private set; }
        internal static GameObject HexMagicBolt { get; private set; }

        internal static bool LoadAssets()
        {
            Bundle = AssetUtils.LoadAssetBundleFromResources(BundleName);

            if (Bundle == null)
            {
                Plugin.Log.LogError("Failed to load HexBattleMage asset bundle.");
                return false;
            }

            HexBeechWand = Bundle.LoadAsset<GameObject>(WandPrefabName);

            if (HexBeechWand == null)
            {
                Plugin.Log.LogError($"Failed to load prefab '{WandPrefabName}'.");
                return false;
            }

            HexMagicBolt = Bundle.LoadAsset<GameObject>(ProjectilePrefabName);

            if (HexMagicBolt == null)
            {
                Plugin.Log.LogError($"Failed to load prefab '{ProjectilePrefabName}'.");
                return false;
            }

            Plugin.Log.LogInfo("HexBattleMage assets loaded.");
            return true;
        }
    }
}

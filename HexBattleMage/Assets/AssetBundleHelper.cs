using Jotunn.Utils;
using UnityEngine;

namespace HexBattleMage.Assets
{
    internal static class AssetBundleHelper
    {
        internal static AssetBundle Bundle { get; private set; }
        internal static GameObject HexBeechWand { get; private set; }
        internal static GameObject HexMagicBolt { get; private set; }

        internal static void LoadAssets()
        {
            Bundle = AssetUtils.LoadAssetBundleFromResources("hexbattlemage");

            if (Bundle == null)
            {
                Plugin.Log.LogError("Failed to load HexBattleMage asset bundle.");
                return;
            }

            HexBeechWand = Bundle.LoadAsset<GameObject>("HexBeechWand");
            HexMagicBolt = Bundle.LoadAsset<GameObject>("HexMagicBolt");

            if (HexBeechWand == null || HexMagicBolt == null)
            {
                Plugin.Log.LogError("Failed to load HexBattleMage prefabs.");
                return;
            }

            Plugin.Log.LogInfo("HexBattleMage assets loaded.");
        }
    }
}
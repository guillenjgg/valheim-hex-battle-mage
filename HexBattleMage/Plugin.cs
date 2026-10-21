using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HexBattleMage.Assets;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Reflection;

namespace HexBattleMage
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        private const string PluginGuid = "com.hex.battle.mage";
        private const string PluginName = "HexBattleMage";
        private const string PluginVersion = "1.0.0";

        private Harmony _harmonyInstance;

        internal static ManualLogSource Log;
        internal static Plugin Instance;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            AssetBundleHelper.LoadAssets();
            ProjectileManager.RegisterProjectiles();
            ItemRegistrationManager.RegisterItems();

            _harmonyInstance = new Harmony(PluginGuid);
            _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            Log.LogInfo($"{PluginName} v{PluginVersion} unloaded.");

            _harmonyInstance?.UnpatchSelf();
            _harmonyInstance = null;
            Instance = null;
            Log = null;
        }
    }
}

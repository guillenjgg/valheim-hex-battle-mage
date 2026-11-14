using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HexBattleMage.Assets;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;
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
        private DevConfig _devConfig;
        private bool _initialized;

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            _devConfig = new DevConfig(Config);
            ModLocalization.Initialize();

            if (!AssetBundleHelper.LoadAssets())
            {
                return;
            }

            if (!ProjectileManager.RegisterProjectiles())
            {
                return;
            }

            if (!ItemRegistrationManager.RegisterItems())
            {
                return;
            }

            CraftingStationManager.Initialize();

            _harmonyInstance = new Harmony(PluginGuid);
            _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            _initialized = true;

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            if (!_devConfig.Update(UnityEngine.Time.unscaledTime))
            {
                return;
            }

            ItemRegistrationManager.ApplySettings(AssetBundleHelper.HexBeechWand.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack);
            ProjectileManager.ApplySettings();
            Log.LogInfo("Dev settings applied. Wand changes apply to the next attack; projectile changes apply to newly fired bolts.");
        }

        private void OnDestroy()
        {
            _initialized = false;
            CraftingStationManager.Shutdown();
            _devConfig?.Dispose();
            Log.LogInfo($"{PluginName} v{PluginVersion} unloaded.");

            _harmonyInstance?.UnpatchSelf();
            _harmonyInstance = null;
            Log = null;
        }
    }
}

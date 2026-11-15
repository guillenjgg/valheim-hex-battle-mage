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

            var wandItem = AssetBundleHelper.HexBattleMageWand.GetComponent<ItemDrop>().m_itemData;

            ItemRegistrationManager.ApplyDamageSettings(wandItem);
            ItemRegistrationManager.ApplySettings(wandItem.m_shared.m_attack);

            if (Player.m_localPlayer != null)
            {
                foreach (var item in Player.m_localPlayer.GetInventory().GetAllItems())
                {
                    if (ItemRegistrationManager.IsWand(item))
                    {
                        ItemRegistrationManager.ApplyDamageSettings(item);
                    }
                }
            }
            ProjectileManager.ApplySettings();
            Log.LogInfo("Development settings applied. Wand lights and bolt appearance update live; attack and projectile gameplay settings apply on the next shot.");
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

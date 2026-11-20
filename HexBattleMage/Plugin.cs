using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HexBattleMage.Assets;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Elements;
using Jotunn.Utils;
using HexBattleMage.Core.Tuning;
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
        private RuneBlockNotifications _runeBlockNotifications;
        private bool _initialized;

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            ModLocalization.Initialize();

            if (!AssetBundleHelper.LoadAssets())
            {
                return;
            }

            PrefabTuning.Apply(AssetBundleHelper.Bundle);
            TuningConfiguration.Initialize(Config);
            CraftingTuning.Configure(Config, AssetBundleHelper.Bundle);
            _devConfig = new DevConfig(Config);
            WandElementManager.Initialize(Config);

            if (!ProjectileManager.RegisterProjectiles())
            {
                return;
            }

            ItemRegistrationManager.Initialize();
            CraftingStationManager.Initialize();
            RuneBlockManager.Initialize();
            MagicShieldManager.Initialize();
            TrinketManager.Initialize(Config);
            CloakManager.Initialize();
            ArmorManager.Initialize();

            _harmonyInstance = new Harmony(PluginGuid);
            _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            _runeBlockNotifications = gameObject.AddComponent<RuneBlockNotifications>();
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
            Log.LogInfo($"Development settings applied. Character attunements control wand elements. Testing selector: {WandElementManager.ActiveElement.Value}; bypass: {WandElementManager.BypassAttunements.Value}. Initial damage and projectile settings apply to the next shot; active elemental effects read live balance settings.");
        }

        private void OnDestroy()
        {
            _initialized = false;
            ItemRegistrationManager.Shutdown();
            CraftingStationManager.Shutdown();
            RuneBlockManager.Shutdown();
            MagicShieldManager.Shutdown();
            TrinketManager.Shutdown();
            CloakManager.Shutdown();
            ArmorManager.Shutdown();
            _devConfig?.Dispose();
            UnityEngine.Object.Destroy(_runeBlockNotifications);
            Log.LogInfo($"{PluginName} v{PluginVersion} unloaded.");

            _harmonyInstance?.UnpatchSelf();
            _harmonyInstance = null;
            Log = null;
        }
    }
}

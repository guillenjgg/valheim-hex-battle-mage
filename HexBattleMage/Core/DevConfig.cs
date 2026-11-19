using BepInEx.Configuration;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Core
{
    internal sealed class DevConfig : IDisposable
    {
        private const string LegacySection = "Dev";
        private const string GeneralSection = "Development";
        private const string ProjectileSection = "Development / Projectile";
        private const string AnimationSection = "Development / Wand Animation";
        private const string LightingSection = "Development / Wand Light";
        private const string WandDamageSection = "Development / Wand Damage";
        private const string DamageSection = "Development / Damage Falloff";
        private const float ReloadCheckInterval = 0.5f;
        private readonly ConfigFile _config;
        private readonly HashSet<ConfigDefinition> _savedDefinitions = new HashSet<ConfigDefinition>();
        private DateTime _lastWriteTime;
        private float _nextCheckTime;
        private int _settingsChanged;

        internal static ConfigEntry<bool> EnableDebugLogging { get; private set; }
        internal static ConfigEntry<float> WandLightRange { get; private set; }
        internal static ConfigEntry<float> WandLightIntensity { get; private set; }
        internal static ConfigEntry<string> AttackAnimation { get; private set; }
        internal static ConfigEntry<int> AttackChainLevels { get; private set; }
        internal static ConfigEntry<int> RandomAttackAnimations { get; private set; }
        internal static ConfigEntry<float> AnimationSpeedMultiplier { get; private set; }
        internal static ConfigEntry<float> ProjectileVelocity { get; private set; }
        internal static ConfigEntry<float> ProjectileAccuracy { get; private set; }
        internal static ConfigEntry<float> ProjectileAccuracyMin { get; private set; }
        internal static ConfigEntry<bool> SkillAccuracy { get; private set; }
        internal static ConfigEntry<float> BoltLength { get; private set; }
        internal static ConfigEntry<float> BoltWidth { get; private set; }
        internal static ConfigEntry<float> BoltJaggedness { get; private set; }
        internal static ConfigEntry<float> BoltBrightness { get; private set; }
        internal static ConfigEntry<Color> BoltColor { get; private set; }
        internal static ConfigEntry<float> ImpactSoundVolume { get; private set; }
        internal static ConfigEntry<float> ImpactVisualDuration { get; private set; }
        internal static ConfigEntry<float> ImpactBurstOpacity { get; private set; }
        internal static ConfigEntry<float> ImpactLineWidth { get; private set; }
        internal static ConfigEntry<float> AreaOfEffectRadius { get; private set; }
        internal static ConfigEntry<float> CollisionRadius { get; private set; }
        internal static ConfigEntry<bool> OnlyStopOnTerrain { get; private set; }
        internal static ConfigEntry<float> WandBluntDamage { get; private set; }
        internal static ConfigEntry<float> WandPierceDamage { get; private set; }
        internal static ConfigEntry<float> WandBluntDamagePerQuality { get; private set; }
        internal static ConfigEntry<float> WandPierceDamagePerQuality { get; private set; }
        internal static ConfigEntry<float> FullDamageRange { get; private set; }
        internal static ConfigEntry<float> DamageFalloffDistance { get; private set; }
        internal static ConfigEntry<float> MinimumDamageMultiplier { get; private set; }

        internal DevConfig(ConfigFile config)
        {
            _config = config;
            ReadSavedDefinitions();

            bool saveOnConfigSet = config.SaveOnConfigSet;

            config.SaveOnConfigSet = false;

            try
            {
                EnableDebugLogging = BindSetting(GeneralSection, nameof(EnableDebugLogging), false, "Enable mod logs outside Plugin.cs, including diagnostic warnings and errors.");
                WandLightRange = BindFloat(LightingSection, nameof(WandLightRange), PrefabSettings.Wand.WandLightRange, "Wand tip light range in metres. Applies immediately to equipped wands.", 0f, 20f);
                WandLightIntensity = BindFloat(LightingSection, nameof(WandLightIntensity), PrefabSettings.Wand.WandLightIntensity, "Wand tip light intensity. Applies immediately to equipped wands.", 0f, 10f);
                AttackAnimation = BindSetting(AnimationSection, nameof(AttackAnimation), PrefabSettings.Wand.AttackAnimation, "Existing player animation trigger prefix. Use swing_axe with 3 chain levels or staff_fireball with 2. Applies to the next attack.");
                AttackChainLevels = BindSetting(AnimationSection, nameof(AttackChainLevels), PrefabSettings.Wand.AttackChainLevels, new ConfigDescription("Number of combo stages. Must match the chosen animation's available triggers. 0 disables chaining.", new AcceptableValueRange<int>(0, 10)));
                RandomAttackAnimations = BindSetting(AnimationSection, nameof(RandomAttackAnimations), PrefabSettings.Wand.RandomAttackAnimations, new ConfigDescription("Random animation variants when chaining is disabled. Must match existing triggers.", new AcceptableValueRange<int>(0, 10)));
                AnimationSpeedMultiplier = BindFloat(AnimationSection, nameof(AnimationSpeedMultiplier), PrefabSettings.Wand.AnimationSpeedMultiplier, "Wand attack animation speed. 1 is normal; 1.5 is 50% faster. Applies on the next attack or animation speed event.", 0.1f, 5f);
                ProjectileVelocity = BindFloat(ProjectileSection, nameof(ProjectileVelocity), PrefabSettings.Wand.ProjectileVelocity, "Projectile launch speed in metres per second.", 1f, 500f);
                ProjectileAccuracy = BindFloat(ProjectileSection, nameof(ProjectileAccuracy), PrefabSettings.Wand.ProjectileAccuracy, "Projectile spread in degrees.", 0f, 180f);
                ProjectileAccuracyMin = BindFloat(ProjectileSection, nameof(ProjectileAccuracyMin), PrefabSettings.Wand.ProjectileAccuracyMin, "Minimum projectile spread in degrees.", 0f, 180f);
                SkillAccuracy = BindSetting(ProjectileSection, nameof(SkillAccuracy), PrefabSettings.Wand.SkillAccuracy, "Let skill affect projectile accuracy.");
                BoltLength = BindFloat(ProjectileSection, nameof(BoltLength), PrefabSettings.Projectile.BoltLength, "Length of the short lightning bolt behind the moving projectile, in metres. Applies live.", 0.2f, 5f);
                BoltWidth = BindFloat(ProjectileSection, nameof(BoltWidth), PrefabSettings.Projectile.BoltWidth, "Width of the lightning bolt's bright core, in metres. Applies live.", 0.005f, 0.15f);
                BoltJaggedness = BindFloat(ProjectileSection, nameof(BoltJaggedness), PrefabSettings.Projectile.BoltJaggedness, "Maximum sideways displacement of the bolt's zigzags, in metres. Applies live.", 0f, 0.3f);
                BoltBrightness = BindFloat(ProjectileSection, nameof(BoltBrightness), PrefabSettings.Projectile.BoltBrightness, "Lightning bolt brightness. Applies live.", 0.1f, 6f);
                BoltColor = BindSetting(ProjectileSection, nameof(BoltColor), PrefabSettings.Projectile.BoltColor, "Lightning bolt halo color. The narrow core stays white. Applies live.");
                ImpactSoundVolume = BindFloat(ProjectileSection, nameof(ImpactSoundVolume), PrefabSettings.Projectile.ImpactSoundVolume, "Local wand impact sound volume multiplier. 1 keeps the current loudness; 0.5 halves the source volume; 0 mutes. Applies to the next impact, including other players wand impacts heard by you.", 0f, 1f);
                ImpactVisualDuration = BindFloat(ProjectileSection, nameof(ImpactVisualDuration), PrefabSettings.Projectile.ImpactVisualDuration, "Smoke-free explosion burst lifetime in seconds. Applies to the next impact.", 0.05f, 0.5f);
                ImpactBurstOpacity = BindFloat(ProjectileSection, nameof(ImpactBurstOpacity), PrefabSettings.Projectile.ImpactBurstOpacity, "Opacity of the smoke-free explosion burst. Applies to the next impact.", 0f, 1f);
                ImpactLineWidth = BindFloat(ProjectileSection, nameof(ImpactLineWidth), PrefabSettings.Projectile.ImpactLineWidth, "Explosion spark width in metres. Burst radius stays within the actual AoE, capped at 0.5 metres; direct hits use a small flash.", 0.005f, 0.05f);
                AreaOfEffectRadius = BindFloat(ProjectileSection, nameof(AreaOfEffectRadius), PrefabSettings.Projectile.AreaOfEffectRadius, "Projectile impact area radius in metres.", 0f, 30f);
                CollisionRadius = BindFloat(ProjectileSection, nameof(CollisionRadius), PrefabSettings.Projectile.CollisionRadius, "Projectile hit detection radius in metres. Larger values make aiming more forgiving and can hit nearby cover sooner. Does not change AoE damage radius. Applies to newly fired projectiles.", 0.01f, 5f);
                OnlyStopOnTerrain = BindSetting(ProjectileSection, nameof(OnlyStopOnTerrain), PrefabSettings.Projectile.OnlyStopOnTerrain, "Only terrain stops the projectile.");
                WandBluntDamage = BindFloat(WandDamageSection, nameof(WandBluntDamage), PrefabSettings.Wand.WandBluntDamage, "Base blunt damage on every wand shot, in addition to pierce and elemental damage. Applies to the next shot.", 0.1f, 1000f);
                WandPierceDamage = BindFloat(WandDamageSection, nameof(WandPierceDamage), PrefabSettings.Wand.WandPierceDamage, "Base pierce damage on every wand shot, in addition to blunt and elemental damage. Applies to the next shot.", 0.1f, 1000f);
                WandBluntDamagePerQuality = BindFloat(WandDamageSection, nameof(WandBluntDamagePerQuality), PrefabSettings.Wand.WandBluntDamagePerQuality, "Additional blunt damage per item quality above one.", 0f, 1000f);
                WandPierceDamagePerQuality = BindFloat(WandDamageSection, nameof(WandPierceDamagePerQuality), PrefabSettings.Wand.WandPierceDamagePerQuality, "Additional pierce damage per item quality above one.", 0f, 1000f);
                FullDamageRange = BindFloat(DamageSection, nameof(FullDamageRange), PrefabSettings.Wand.FullDamageRange, "Full wand projectile damage up to this distance from launch, in metres. Falloff is evaluated at impact for both direct and area damage.", 0f, 100f);
                DamageFalloffDistance = BindFloat(DamageSection, nameof(DamageFalloffDistance), PrefabSettings.Wand.DamageFalloffDistance, "Distance beyond FullDamageRange over which damage gradually falls to its minimum. For example, 15 + 20 reaches minimum damage at 35 metres.", 0.1f, 200f);
                MinimumDamageMultiplier = BindFloat(DamageSection, nameof(MinimumDamageMultiplier), PrefabSettings.Wand.MinimumDamageMultiplier, "Minimum fraction of wand projectile damage at long range. 0.2 retains 20%. Cannot be set to zero.", 0.01f, 1f);
            }
            finally
            {
                config.SaveOnConfigSet = saveOnConfigSet;
            }

            config.Save();
            _lastWriteTime = File.GetLastWriteTimeUtc(config.ConfigFilePath);
            config.SettingChanged += OnSettingChanged;
        }

        internal bool Update(float currentTime)
        {
            if (currentTime >= _nextCheckTime)
            {
                _nextCheckTime = currentTime + ReloadCheckInterval;
                CheckForFileChanges();
            }

            return Interlocked.Exchange(ref _settingsChanged, 0) != 0;
        }

        public void Dispose()
        {
            _config.SettingChanged -= OnSettingChanged;
        }

        private ConfigEntry<float> BindFloat(string section, string name, float defaultValue, string description, float minimum, float maximum)
        {
            return BindSetting(section, name, defaultValue, new ConfigDescription(description, new AcceptableValueRange<float>(minimum, maximum)));
        }

        private ConfigEntry<T> BindSetting<T>(string section, string name, T defaultValue, string description)
        {
            return BindSetting(section, name, defaultValue, new ConfigDescription(description));
        }

        private ConfigEntry<T> BindSetting<T>(string section, string name, T defaultValue, ConfigDescription description)
        {
            var entry = _config.Bind(section, name, defaultValue, description);

            if (_savedDefinitions.Contains(entry.Definition))
            {
                return entry;
            }

            var legacyDefinition = new ConfigDefinition(LegacySection, name);

            if (!_savedDefinitions.Contains(legacyDefinition))
            {
                return entry;
            }

            var legacyEntry = _config.Bind(LegacySection, name, defaultValue, description);

            entry.Value = legacyEntry.Value;
            _config.Remove(legacyDefinition);
            return entry;
        }

        private void ReadSavedDefinitions()
        {
            if (!File.Exists(_config.ConfigFilePath))
            {
                return;
            }

            string section = string.Empty;

            foreach (string line in File.ReadAllLines(_config.ConfigFilePath))
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith("#"))
                {
                    continue;
                }

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    section = trimmed.Substring(1, trimmed.Length - 2);
                    continue;
                }

                int separator = trimmed.IndexOf('=');

                if (separator <= 0)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(section))
                {
                    continue;
                }

                _savedDefinitions.Add(new ConfigDefinition(section, trimmed.Substring(0, separator).Trim()));
            }
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs args)
        {
            Interlocked.Exchange(ref _settingsChanged, 1);
        }

        private void CheckForFileChanges()
        {
            bool saveOnConfigSet = _config.SaveOnConfigSet;

            try
            {
                if (!File.Exists(_config.ConfigFilePath))
                {
                    return;
                }

                DateTime writeTime = File.GetLastWriteTimeUtc(_config.ConfigFilePath);

                if (writeTime == _lastWriteTime)
                {
                    return;
                }

                _config.SaveOnConfigSet = false;
                _config.Reload();
                _lastWriteTime = writeTime;
            }
            catch (IOException exception)
            {
                DebugLog.LogWarning($"Could not reload Dev config: {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                DebugLog.LogWarning($"Could not read Dev config: {exception.Message}");
            }
            finally
            {
                _config.SaveOnConfigSet = saveOnConfigSet;
            }
        }
    }
}

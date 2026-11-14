using BepInEx.Configuration;
using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace HexBattleMage.Core
{
    internal sealed class DevConfig : IDisposable
    {
        private const string Section = "Dev";
        private const float ReloadCheckInterval = 0.5f;
        private readonly ConfigFile _config;
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
        internal static ConfigEntry<float> SparkSize { get; private set; }
        internal static ConfigEntry<float> SparksPerSecond { get; private set; }
        internal static ConfigEntry<float> SparksPerMeter { get; private set; }
        internal static ConfigEntry<ParticleSystemSimulationSpace> SparkSimulationSpace { get; private set; }
        internal static ConfigEntry<ParticleSystemRenderMode> SparkRenderMode { get; private set; }
        internal static ConfigEntry<bool> SparkEmissionEnabled { get; private set; }
        internal static ConfigEntry<float> AreaOfEffectRadius { get; private set; }
        internal static ConfigEntry<float> CollisionRadius { get; private set; }
        internal static ConfigEntry<bool> OnlyStopOnTerrain { get; private set; }
        internal static ConfigEntry<float> FullDamageRange { get; private set; }
        internal static ConfigEntry<float> DamageFalloffDistance { get; private set; }
        internal static ConfigEntry<float> MinimumDamageMultiplier { get; private set; }

        internal DevConfig(ConfigFile config)
        {
            _config = config;
            EnableDebugLogging = config.Bind(Section, nameof(EnableDebugLogging), false, "Enable mod logs outside Plugin.cs, including diagnostic warnings and errors.");
            WandLightRange = BindFloat(nameof(WandLightRange), 5f, "Wand tip light range in metres. Applies immediately to equipped wands.", 0f, 20f);
            WandLightIntensity = BindFloat(nameof(WandLightIntensity), 3f, "Wand tip light intensity. Applies immediately to equipped wands.", 0f, 10f);
            AttackAnimation = config.Bind(Section, nameof(AttackAnimation), "swing_axe", "Existing player animation trigger prefix. Use swing_axe with 3 chain levels or staff_fireball with 2. Applies to the next attack.");
            AttackChainLevels = config.Bind(Section, nameof(AttackChainLevels), 3, new ConfigDescription("Number of combo stages. Must match the chosen animation's available triggers. 0 disables chaining.", new AcceptableValueRange<int>(0, 10)));
            RandomAttackAnimations = config.Bind(Section, nameof(RandomAttackAnimations), 0, new ConfigDescription("Random animation variants when chaining is disabled. Must match existing triggers.", new AcceptableValueRange<int>(0, 10)));
            AnimationSpeedMultiplier = BindFloat(nameof(AnimationSpeedMultiplier), 1f, "Wand attack animation speed. 1 is normal; 1.5 is 50% faster. Applies on the next attack or animation speed event.", 0.1f, 5f);
            ProjectileVelocity = BindFloat(nameof(ProjectileVelocity), 100f, "Projectile launch speed in metres per second.", 1f, 500f);
            ProjectileAccuracy = BindFloat(nameof(ProjectileAccuracy), 0f, "Projectile spread in degrees.", 0f, 180f);
            ProjectileAccuracyMin = BindFloat(nameof(ProjectileAccuracyMin), 0f, "Minimum projectile spread in degrees.", 0f, 180f);
            SkillAccuracy = config.Bind(Section, nameof(SkillAccuracy), false, "Let skill affect projectile accuracy.");
            SparkSize = BindFloat(nameof(SparkSize), 0.45f, "Spark particle start size.", 0.01f, 20f);
            SparksPerSecond = BindFloat(nameof(SparksPerSecond), 40f, "Spark emission per second.", 0f, 1000f);
            SparksPerMeter = BindFloat(nameof(SparksPerMeter), 0.5f, "Spark emission per metre travelled.", 0f, 100f);
            SparkSimulationSpace = config.Bind(Section, nameof(SparkSimulationSpace), ParticleSystemSimulationSpace.World, "World leaves particles along the flight path; Local moves them with the bolt. Custom requires a transform and is treated as World.");
            SparkRenderMode = config.Bind(Section, nameof(SparkRenderMode), ParticleSystemRenderMode.Billboard, "Particle rendering mode. Billboard faces the camera.");
            SparkEmissionEnabled = config.Bind(Section, nameof(SparkEmissionEnabled), true, "Enable spark emission on newly fired bolts.");
            AreaOfEffectRadius = BindFloat(nameof(AreaOfEffectRadius), 3f, "Projectile impact area radius in metres.", 0f, 30f);
            CollisionRadius = BindFloat(nameof(CollisionRadius), 0.25f, "Projectile collision radius in metres.", 0.01f, 5f);
            OnlyStopOnTerrain = config.Bind(Section, nameof(OnlyStopOnTerrain), false, "Only terrain stops the projectile.");
            FullDamageRange = BindFloat(nameof(FullDamageRange), 10f, "Full wand projectile damage up to this distance from launch, in metres. Falloff is evaluated at impact for both direct and area damage.", 0f, 100f);
            DamageFalloffDistance = BindFloat(nameof(DamageFalloffDistance), 20f, "Distance beyond FullDamageRange over which damage gradually falls to its minimum. For example, 10 + 20 reaches minimum damage at 30 metres.", 0.1f, 200f);
            MinimumDamageMultiplier = BindFloat(nameof(MinimumDamageMultiplier), 0.2f, "Minimum fraction of wand projectile damage at long range. 0.2 retains 20%. Cannot be set to zero.", 0.01f, 1f);
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

        private ConfigEntry<float> BindFloat(string name, float defaultValue, string description, float minimum, float maximum)
        {
            return _config.Bind(Section, name, defaultValue, new ConfigDescription(description, new AcceptableValueRange<float>(minimum, maximum)));
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

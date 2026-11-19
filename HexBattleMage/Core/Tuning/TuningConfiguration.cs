using BepInEx.Configuration;

namespace HexBattleMage.Core.Tuning
{
    internal static class TuningConfiguration
    {
        private static ConfigEntry<bool> _useConfigOverrides;

        internal static void Initialize(ConfigFile config)
        {
            _useConfigOverrides = config.Bind("Tuning", "UseConfigOverrides", true, "False uses baked prefab defaults plus Core/Tuning code changes. True additionally applies saved development and trinket configuration values. Element selection and debug logging always remain live configuration controls. Code changes require a DLL rebuild and game restart.");
        }

        internal static T EffectiveValue<T>(this ConfigEntry<T> entry)
        {
            if (_useConfigOverrides == null || _useConfigOverrides.Value)
            {
                return entry.Value;
            }

            return (T)entry.DefaultValue;
        }
    }
}

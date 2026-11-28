using BepInEx.Configuration;

namespace HexBattleMage.Core.Elements
{
    internal sealed class WandElementSettings
    {
        internal WandElementProfile Profile { get; }
        internal ConfigEntry<float> DirectDamage { get; }
        internal ConfigEntry<float> DamagePerQuality { get; }
        internal ConfigEntry<float> TickDamage { get; }
        internal ConfigEntry<float> TotalDamage { get; }
        internal ConfigEntry<float> Duration { get; }
        internal ConfigEntry<float> TickInterval { get; }
        internal ConfigEntry<float> MovementSlow { get; }
        internal ConfigEntry<float> AttackSlow { get; }
        internal ConfigEntry<float> SpreadRadius { get; }
        internal ConfigEntry<float> SpreadInterval { get; }
        internal ConfigEntry<int> MaximumStacks { get; }
        internal ConfigEntry<float> BossTickMultiplier { get; }

        internal WandElementSettings(ConfigFile config, WandElementProfile profile)
        {
            Profile = profile;

            string section = "Development / Elements / " + profile.Element;

            DirectDamage = Bind(config, section, nameof(DirectDamage), profile.DirectDamage, 0f, 1000f, "Initial elemental hit damage. Applies on the next shot, before skill and range modifiers.");
            DamagePerQuality = Bind(config, section, nameof(DamagePerQuality), profile.DamagePerQuality, 0f, 1000f, "Additional initial elemental damage per item quality above 1.");
            TickDamage = Bind(config, section, nameof(TickDamage), profile.TickDamage, 0f, 1000f, "Elemental damage per DoT tick and stack, before enemy resistance. Applies to active effects on their next tick.");
            if (profile.Element == WandElement.Blight)
            {
                TotalDamage = Bind(config, section, nameof(TotalDamage), profile.TotalDamage, 0f, 10000f, "Total untyped damage over a completed Blight application. Duration changes delivery speed without changing this budget.");
            }

            Duration = Bind(config, section, nameof(Duration), profile.Duration, 0.1f, 120f, "Effect duration in seconds. Lightning uses this as stun duration. Reinfection refreshes duration.");
            TickInterval = Bind(config, section, nameof(TickInterval), profile.TickInterval, 0.1f, 10f, "Seconds between DoT ticks. Applies to active effects.");
            MovementSlow = Bind(config, section, nameof(MovementSlow), profile.MovementSlow, 0f, 1f, "Movement speed reduction as a fraction: 0.3 means 30%. Does not stack.");
            AttackSlow = Bind(config, section, nameof(AttackSlow), profile.AttackSlow, 0f, 1f, "Attack animation speed reduction as a fraction: 0.3 means 30%. Does not stack.");
            SpreadRadius = Bind(config, section, nameof(SpreadRadius), profile.SpreadRadius, 0f, 30f, "Infection radius in metres for Spirit/Poison. Zero disables spreading. Players and tamed creatures are excluded.");
            SpreadInterval = Bind(config, section, nameof(SpreadInterval), profile.SpreadInterval, 0.1f, 10f, "Seconds between proximity checks. Each infected source can affect each neighbour once until a fresh wand hit resets its spread list.");
            MaximumStacks = config.Bind(section, nameof(MaximumStacks), profile.MaximumStacks, new ConfigDescription("Poison damage stack cap. Other elements refresh without stacking.", new AcceptableValueRange<int>(1, 20)));
            BossTickMultiplier = Bind(config, section, nameof(BossTickMultiplier), profile.BossTickMultiplier, 0f, 2f, "DoT multiplier against bosses. Poison's provisional default is 0.25; other elements default to 1.");
        }

        private static ConfigEntry<float> Bind(ConfigFile config, string section, string key, float value, float minimum, float maximum, string description)
        {
            return config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(minimum, maximum)));
        }
    }
}

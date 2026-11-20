using BepInEx.Configuration;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Trinkets
{
    internal sealed class TrinketSettings
    {
        internal const float MinimumRewardDuration = 0.01f;
        internal GameObject VisualPrefab { get; }
        internal HexBattleMageTrinketProfile Profile { get; }
        internal string ConfigSection { get; }
        internal string RewardStatusName { get; }
        internal int RewardHash { get; }
        internal ConfigEntry<float> MaximumEitr { get; }
        internal ConfigEntry<float> Regeneration { get; }
        internal ConfigEntry<float> StaminaRegeneration { get; }
        internal ConfigEntry<float> DamageBonus { get; }
        internal ConfigEntry<float> BossDamageBonus { get; }
        internal ConfigEntry<float> Duration { get; }

        internal static string GetConfigSection(HexBattleMageTrinketProfile profile)
        {
            return string.IsNullOrWhiteSpace(profile.ConfigSection) ? "Equipment / Trinkets / " + profile.ItemPrefabName : profile.ConfigSection;
        }

        internal static string GetRewardStatusName(HexBattleMageTrinketProfile profile)
        {
            return string.IsNullOrWhiteSpace(profile.RewardStatusName) ? profile.ItemPrefabName + "ParryEmpowerment" : profile.RewardStatusName;
        }

        internal TrinketSettings(GameObject visualPrefab, ConfigFile config)
        {
            VisualPrefab = visualPrefab;
            Profile = PrefabSettings.Require<HexBattleMageTrinketProfile>(visualPrefab);
            ConfigSection = GetConfigSection(Profile);

            var eitr = PrefabSettings.Require<MageEitrProfile>(visualPrefab);

            MaximumEitr = config.Bind(ConfigSection, "MaximumEitr", eitr.MaximumEitr, "Flat maximum Eitr while equipped. Regenerates without Eitr food.");
            Regeneration = config.Bind(ConfigSection, "RegenerationModifier", eitr.RegenerationModifier, "Extra Eitr regeneration; 0.10 means +10%.");
            StaminaRegeneration = config.Bind(ConfigSection, "StaminaRegenerationModifier", Profile.StaminaRegenerationModifier, "Extra stamina regeneration; 0.10 means +10%.");

            if (Profile.RewardTrigger == TrinketRewardTrigger.None)
            {
                return;
            }

            DamageBonus = config.Bind(ConfigSection, "ParryWandDamageBonus", Profile.WandDamageBonus, "Wand shot damage bonus after a successful parry; 0.25 means +25%.");
            BossDamageBonus = config.Bind(ConfigSection, "BossParryAdditionalDamageBonus", Profile.BossParryDamageBonus, "Additional wand damage bonus when the parried attacker is a boss; 0.10 adds 10 percentage points to the normal reward.");
            Duration = config.Bind(ConfigSection, "ParryDuration", Profile.ParryDuration, "Parry reward duration in seconds. Further parries refresh it without stacking.");
            RewardStatusName = GetRewardStatusName(Profile);

            var status = ScriptableObject.CreateInstance<TrinketRewardStatus>();

            status.name = RewardStatusName;
            RewardHash = status.NameHash();
            Object.DestroyImmediate(status);
        }
    }
}

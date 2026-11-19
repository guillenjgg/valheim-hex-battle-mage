using System;
using System.Text;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Trinkets
{
    internal static class TrinketEffects
    {
        private delegate void FoodValues(Player player, ref float health, ref float stamina, ref float eitr);
        private static readonly FoodValues ReadFoodValues = AccessTools.MethodDelegate<FoodValues>(AccessTools.Method(typeof(Player), "GetTotalFoodValue"));
        private static readonly Action<Player, float, bool> SetMaximumEitr = AccessTools.MethodDelegate<Action<Player, float, bool>>(AccessTools.Method(typeof(Player), "SetMaxEitr"));

        internal static float EitrBonus(Humanoid humanoid, bool regeneration)
        {
            float total = 0f;

            foreach (var item in humanoid.GetInventory().GetAllItems())
            {
                if (!item.m_equipped)
                {
                    continue;
                }

                if (item.m_dropPrefab == null)
                {
                    continue;
                }

                var profile = item.m_dropPrefab.GetComponent<MageEitrProfile>();

                if (profile == null)
                {
                    continue;
                }

                var trinket = TrinketManager.GetProfile(item);

                if (trinket == null)
                {
                    total += Mathf.Max(0f, regeneration ? profile.RegenerationModifier : profile.GetMaximumEitr(item.m_quality));
                    continue;
                }

                if (TrinketManager.GetEquipped(humanoid) != item)
                {
                    continue;
                }

                var settings = TrinketManager.GetSettings(trinket);

                if (settings != null)
                {
                    total += Mathf.Max(0f, regeneration ? settings.Regeneration.Value : settings.MaximumEitr.Value);
                }
            }

            return total;
        }

        internal static void RefreshEitr(Humanoid humanoid, float previousEitr = 0f)
        {
            RefreshGearEitr(humanoid, previousEitr);
            ClearRewards(humanoid);
        }

        internal static void RefreshGearEitr(Humanoid humanoid, float previousEitr = 0f)
        {
            var player = humanoid as Player;

            if (player == null)
            {
                return;
            }

            float health = 0f;
            float stamina = 0f;
            float eitr = 0f;

            ReadFoodValues(player, ref health, ref stamina, ref eitr);
            SetMaximumEitr(player, eitr, false);

            // Native replacement unequips the old trinket first, temporarily clamping the gear pool away.
            float preservedEitr = Mathf.Min(previousEitr, player.GetMaxEitr());
            float removedEitr = preservedEitr - player.GetEitr();

            if (removedEitr > 0f)
            {
                player.AddEitr(removedEitr);
            }

        }

        internal static void ClearRewards(Humanoid humanoid)
        {
            var effects = humanoid.GetSEMan();

            if (effects == null)
            {
                return;
            }

            foreach (var settings in TrinketManager.AllSettings)
            {
                if (settings.Profile.RewardTrigger == TrinketRewardTrigger.None)
                {
                    continue;
                }

                effects.RemoveStatusEffect(settings.RewardHash, true);
            }
        }

        internal static void RewardParry(Humanoid humanoid, Character attacker)
        {
            var player = humanoid as Player;

            if (player == null)
            {
                return;
            }

            if (player.IsDead())
            {
                return;
            }

            var item = TrinketManager.GetEquipped(player);
            var settings = TrinketManager.GetSettings(TrinketManager.GetProfile(item));

            if (settings == null)
            {
                return;
            }

            if (settings.Profile.RewardTrigger != TrinketRewardTrigger.SuccessfulParry)
            {
                return;
            }

            var effects = player.GetSEMan();

            effects.AddStatusEffect(settings.RewardHash, true);

            var effect = effects.GetStatusEffect(settings.RewardHash) as TrinketRewardStatus;

            if (effect != null)
            {
                effect.m_ttl = Mathf.Max(TrinketSettings.MinimumRewardDuration, settings.Duration.Value);
                effect.FromBossParry = attacker != null && attacker.IsBoss();
            }
        }

        internal static float DamageMultiplier(Humanoid humanoid)
        {
            if (humanoid.IsDead())
            {
                return 1f;
            }

            var item = TrinketManager.GetEquipped(humanoid);
            var settings = TrinketManager.GetSettings(TrinketManager.GetProfile(item));

            if (settings == null)
            {
                return 1f;
            }

            if (settings.Profile.RewardTrigger != TrinketRewardTrigger.SuccessfulParry)
            {
                return 1f;
            }

            var effect = humanoid.GetSEMan().GetStatusEffect(settings.RewardHash) as TrinketRewardStatus;

            if (effect == null)
            {
                return 1f;
            }

            float bonus = Mathf.Max(0f, settings.DamageBonus.Value);

            if (effect.FromBossParry)
            {
                bonus += Mathf.Max(0f, settings.BossDamageBonus.Value);
            }

            return 1f + bonus;
        }

        internal static string Tooltip(HexBattleMageTrinketProfile profile)
        {
            var settings = TrinketManager.GetSettings(profile);

            if (settings == null)
            {
                return string.Empty;
            }

            var text = new StringBuilder();

            AppendHeading(text, profile.PassiveHeading);
            text.AppendLine(ModLocalization.Format("$hexbattlemage_trinket_maximum_eitr", Mathf.Max(0f, settings.MaximumEitr.Value)));
            text.Append(ModLocalization.Format("$hexbattlemage_trinket_eitr_regeneration", Mathf.Max(0f, settings.Regeneration.Value) * 100f));

            if (profile.RewardTrigger == TrinketRewardTrigger.None)
            {
                return text.ToString();
            }

            float normalBonus = Mathf.Max(0f, settings.DamageBonus.Value) * 100f;
            float additionalBossBonus = Mathf.Max(0f, settings.BossDamageBonus.Value) * 100f;
            float duration = Mathf.Max(TrinketSettings.MinimumRewardDuration, settings.Duration.Value);

            AppendHeading(text, profile.RewardHeading);
            text.AppendLine(ModLocalization.Format(profile.RewardCondition));
            text.AppendLine(ModLocalization.Format("$hexbattlemage_trinket_parry_damage", normalBonus, duration));
            text.Append(ModLocalization.Format(profile.RewardDetails));

            if (additionalBossBonus > 0f)
            {
                AppendHeading(text, profile.BossHeading);
                text.AppendLine(ModLocalization.Format(profile.BossCondition));
                text.Append(ModLocalization.Format("$hexbattlemage_trinket_boss_damage", additionalBossBonus, normalBonus + additionalBossBonus, duration));
            }

            return text.ToString();
        }

        private static void AppendHeading(StringBuilder text, string token)
        {
            text.Append("\n\n<color=orange><b>");
            text.Append(ModLocalization.Format(token));
            text.Append("</b></color>\n");
        }
    }
}

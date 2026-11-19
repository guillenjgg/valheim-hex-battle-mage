using HexBattleMage.Core.Tuning;
using System;
using System.Text;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Trinkets;

namespace HexBattleMage.Core
{
    internal static class MageGearStats
    {
        internal const string CompendiumTopic = "$hexbattlemage_gearstats_title";
        private static readonly Func<Humanoid, ItemDrop.ItemData> ReadBlocker = AccessTools.MethodDelegate<Func<Humanoid, ItemDrop.ItemData>>(AccessTools.Method(typeof(Humanoid), "GetCurrentBlocker"));

        private static readonly AccessTools.FieldRef<Humanoid, ItemDrop> UnarmedWeapon = AccessTools.FieldRefAccess<Humanoid, ItemDrop>("m_unarmedWeapon");

        internal static string GetText()
        {
            var player = Player.m_localPlayer;
            var text = new StringBuilder();

            if (player == null)
            {
                return ModLocalization.Format("$hexbattlemage_gearstats_no_player");
            }

            var weapon = player.GetCurrentWeapon();

            Heading(text, "$hexbattlemage_gearstats_weapon");

            var unarmed = UnarmedWeapon(player);

            if (weapon == null || unarmed != null && weapon == unarmed.m_itemData)
            {
                text.AppendLine(ModLocalization.Format("$hexbattlemage_gearstats_no_weapon"));
            }
            else
            {
                text.AppendLine(ModLocalization.Format("$hexbattlemage_gearstats_item", ModLocalization.Format(weapon.m_shared.m_name), weapon.m_quality));
                var damage = weapon.GetDamage();

                text.AppendLine(ModLocalization.Format("$hexbattlemage_gearstats_damage", damage.GetTotalDamage()));
                AppendDamage(text, "blunt", damage.m_blunt);
                AppendDamage(text, "slash", damage.m_slash);
                AppendDamage(text, "pierce", damage.m_pierce);
                AppendDamage(text, "fire", damage.m_fire);
                AppendDamage(text, "frost", damage.m_frost);
                AppendDamage(text, "lightning", damage.m_lightning);
                AppendDamage(text, "poison", damage.m_poison);
                AppendDamage(text, "spirit", damage.m_spirit);
                AppendDamage(text, "generic", damage.m_damage);

                if (ItemRegistrationManager.IsWand(weapon))
                {
                    text.AppendLine();
                    Heading(text, "$hexbattlemage_gearstats_range");
                    AppendRange(text, damage.GetTotalDamage(), DevConfig.FullDamageRange.EffectiveValue(), DevConfig.DamageFalloffDistance.EffectiveValue(), DevConfig.MinimumDamageMultiplier.EffectiveValue());
                }
            }

            text.AppendLine();
            Heading(text, "$hexbattlemage_gearstats_blocking");
            var blocker = ReadBlocker(player);

            if (blocker == null || unarmed != null && blocker == unarmed.m_itemData)
            {
                text.AppendLine(ModLocalization.Format("$hexbattlemage_gearstats_no_blocker"));
            }
            else
            {
                float block = blocker.GetBlockPowerTooltip(blocker.m_quality);
                float parry = blocker.m_shared.m_timedBlockBonus;

                text.AppendLine(ModLocalization.Format(blocker.m_shared.m_name));
                text.AppendLine(ModLocalization.Format("$hexbattlemage_gearstats_block", block, blocker.GetDeflectionForce()));
                text.AppendLine(ModLocalization.Format("$hexbattlemage_gearstats_parry", parry, block * parry));
            }

            text.AppendLine();
            Heading(text, "$hexbattlemage_gearstats_eitr");
            float health = GearVitals.Bonus(player, true);
            float stamina = GearVitals.Bonus(player, false);

            if (health > 0f)
            {
                text.AppendLine(ModLocalization.Format("$hexbattlemage_gear_maximum_health", health));
            }

            if (stamina > 0f)
            {
                text.AppendLine(ModLocalization.Format("$hexbattlemage_gear_maximum_stamina", stamina));
            }

            text.AppendLine(ModLocalization.Format("$hexbattlemage_gearstats_eitr_total", TrinketEffects.EitrBonus(player, false)));
            text.AppendLine(ModLocalization.Format("$hexbattlemage_gearstats_eitr_note"));
            text.AppendLine();
            text.Append(ModLocalization.Format("$hexbattlemage_gearstats_damage_note"));
            return text.ToString();
        }

        internal static void AppendRange(StringBuilder text, float damage, float fullRange, float falloffDistance, float minimumMultiplier)
        {
            float end = fullRange + falloffDistance;
            float middle = fullRange + falloffDistance * 0.5f;
            float middleMultiplier = ProjectileDamageFalloff.CalculateMultiplier(middle, fullRange, falloffDistance, minimumMultiplier);

            text.AppendLine(ModLocalization.Format("$hexbattlemage_gearstats_range_full", fullRange, damage));
            text.AppendLine(ModLocalization.Format("$hexbattlemage_gearstats_range_middle", middle, damage * middleMultiplier, middleMultiplier * 100f));
            text.AppendLine(ModLocalization.Format("$hexbattlemage_gearstats_range_minimum", end, damage * minimumMultiplier, minimumMultiplier * 100f));
        }

        private static void Heading(StringBuilder text, string token)
        {
            text.AppendLine("<color=#FFA500><b>" + ModLocalization.Format(token) + "</b></color>");
        }

        private static void AppendDamage(StringBuilder text, string type, float value)
        {
            if (value <= 0f)
            {
                return;
            }

            text.AppendLine("  " + ModLocalization.Format("$hexbattlemage_gearstats_damage_component", ModLocalization.Format("$hexbattlemage_damage_" + type), value));
        }
    }
}

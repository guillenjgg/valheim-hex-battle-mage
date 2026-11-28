using HexBattleMage.Core.Tuning;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace HexBattleMage.Core.Elements
{
    internal static class WandElementText
    {
        internal static string Damage(WandElement element, int quality, bool showQualityGrowth)
        {
            var settings = WandElementManager.GetSettings(element);
            var text = new StringBuilder();
            float duration = Mathf.Max(0.1f, settings.Duration.EffectiveValue());

            if (element == WandElement.Blight)
            {
                float total = Mathf.Max(0f, settings.TotalDamage.EffectiveValue());

                text.AppendLine(ModLocalization.Format("$hexbattlemage_element_blight_damage", Number(total), Number(duration), Number(total / duration)));
                text.AppendLine(ModLocalization.Format("$hexbattlemage_element_blight_refresh"));
            }
            else
            {
                float perQuality = settings.DamagePerQuality.EffectiveValue();
                float direct = settings.DirectDamage.EffectiveValue() + Mathf.Max(0, quality - 1) * perQuality;

                text.AppendLine(ModLocalization.Format("$hexbattlemage_element_hit_damage", Number(direct)));

                if (showQualityGrowth && perQuality > 0f)
                {
                    text.AppendLine(ModLocalization.Format("$hexbattlemage_element_quality_damage", Number(perQuality)));
                }

                float tick = settings.TickDamage.EffectiveValue();

                if (tick > 0f)
                {
                    text.AppendLine(ModLocalization.Format("$hexbattlemage_element_tick_damage", Number(tick), Number(settings.TickInterval.EffectiveValue()), Number(duration)));
                }
            }

            float bossMultiplier = settings.BossTickMultiplier.EffectiveValue();

            if (!Mathf.Approximately(bossMultiplier, 1f) && (element == WandElement.Blight || settings.TickDamage.EffectiveValue() > 0f))
            {
                text.AppendLine(ModLocalization.Format("$hexbattlemage_element_boss_dot", Number(bossMultiplier * 100f)));
            }

            return text.ToString().TrimEnd();
        }

        internal static string Name(WandElement element)
        {
            return ModLocalization.Format("$hexbattlemage_element_" + element.ToString().ToLowerInvariant());
        }

        private static string Number(float value)
        {
            return value.ToString("0.##", CultureInfo.CurrentCulture);
        }
    }
}

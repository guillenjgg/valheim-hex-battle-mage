using HexBattleMage.Core.Tuning;
using System;
using System.Collections.Generic;
using System.Text;
using HexBattleMage.Core;
using HexBattleMage.Core.Elements;
using System.Reflection;
using HarmonyLib;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Patches
{
    [HarmonyPatch]
    internal static class WandAttunedDamagePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(ItemDrop.ItemData)))
            {
                if (method.Name == nameof(ItemDrop.ItemData.GetDamage))
                {
                    yield return method;
                }
            }
        }

        private static void Prefix(ItemDrop.ItemData __instance)
        {
            if (ItemRegistrationManager.IsWand(__instance))
            {
                ItemRegistrationManager.ApplyDamageSettings(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    internal static class WandElementTooltipPatch
    {
        private const string DamageSectionEnd = "\n$item_knockback:";

        private static void Postfix(ItemDrop.ItemData item, int qualityLevel, ref string __result)
        {
            if (!ItemRegistrationManager.IsWand(item))
            {
                return;
            }

            if (WandElementManager.Enabled == null)
            {
                return;
            }

            if (!WandElementManager.Enabled.EffectiveValue())
            {
                return;
            }

            var text = new StringBuilder();

            foreach (var element in WandAttunements.GetCombatElements(item))
            {
                text.AppendLine("<color=orange>" + WandElementText.Name(element) + "</color>");
                text.AppendLine(WandElementText.Damage(element, qualityLevel, false));
            }

            if (text.Length == 0)
            {
                return;
            }

            text.AppendLine(ModLocalization.Format("$hexbattlemage_element_damage_basis"));
            int position = __result.IndexOf(DamageSectionEnd, StringComparison.Ordinal);

            if (position >= 0)
            {
                __result = __result.Insert(position, "\n\n" + text.ToString().TrimEnd() + "\n");
                return;
            }

            __result = GearTooltip.InsertPerks(item, __result, text.ToString());
        }
    }

}

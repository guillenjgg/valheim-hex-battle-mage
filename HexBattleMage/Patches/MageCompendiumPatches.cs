using HarmonyLib;
using HexBattleMage.Core;
using System.Collections.Generic;
using System.Reflection;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(TextsDialog), "UpdateTextsList")]
    internal static class MageCompendiumListPatch
    {
        private static void Postfix(List<TextsDialog.TextInfo> ___m_texts)
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            ___m_texts.Add(new TextsDialog.TextInfo(MageGearStats.CompendiumTopic, MageGearStats.GetText()));
        }
    }

    [HarmonyPatch(typeof(TextsDialog), "ShowText", new[] { typeof(TextsDialog.TextInfo) })]
    internal static class MageCompendiumSelectionPatch
    {
        private static void Prefix(TextsDialog.TextInfo text)
        {
            if (text.m_topic != MageGearStats.CompendiumTopic)
            {
                return;
            }

            text.m_text = MageGearStats.GetText();
        }
    }

    [HarmonyPatch(typeof(TextsDialog), "Update")]
    internal static class MageCompendiumRefreshPatch
    {
        private static readonly MethodInfo ShowTextMethod = AccessTools.Method(typeof(TextsDialog), "ShowText", new[] { typeof(TextsDialog.TextInfo) });

        private static void Postfix(TextsDialog __instance, List<TextsDialog.TextInfo> ___m_texts, int ___m_selectionIndex)
        {
            if (___m_selectionIndex < 0)
            {
                return;
            }

            if (___m_selectionIndex >= ___m_texts.Count)
            {
                return;
            }

            var entry = ___m_texts[___m_selectionIndex];

            if (entry.m_topic != MageGearStats.CompendiumTopic)
            {
                return;
            }

            string currentText = MageGearStats.GetText();

            if (entry.m_text == currentText)
            {
                return;
            }

            entry.m_text = currentText;
            ShowTextMethod.Invoke(__instance, new object[] { entry });
        }
    }
}

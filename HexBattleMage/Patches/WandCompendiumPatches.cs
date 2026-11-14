using HarmonyLib;
using HexBattleMage.Core;
using System.Collections.Generic;
using System.Reflection;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(TextsDialog), "UpdateTextsList")]
    internal static class WandCompendiumListPatch
    {
        private static void Postfix(List<TextsDialog.TextInfo> ___m_texts)
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            ___m_texts.Add(new TextsDialog.TextInfo(WandStats.CompendiumTopic, WandStats.GetText()));
        }
    }

    [HarmonyPatch(typeof(TextsDialog), "ShowText", new[] { typeof(TextsDialog.TextInfo) })]
    internal static class WandCompendiumSelectionPatch
    {
        private static void Prefix(TextsDialog.TextInfo text)
        {
            if (text.m_topic != WandStats.CompendiumTopic)
            {
                return;
            }

            text.m_text = WandStats.GetText();
        }
    }

    [HarmonyPatch(typeof(TextsDialog), "Update")]
    internal static class WandCompendiumRefreshPatch
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

            if (entry.m_topic != WandStats.CompendiumTopic)
            {
                return;
            }

            string currentText = WandStats.GetText();

            if (entry.m_text == currentText)
            {
                return;
            }

            entry.m_text = currentText;
            ShowTextMethod.Invoke(__instance, new object[] { entry });
        }
    }
}

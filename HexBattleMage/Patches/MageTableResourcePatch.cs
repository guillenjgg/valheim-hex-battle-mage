using HarmonyLib;
using HexBattleMage.Core;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
    internal static class MageTableResourcePatch
    {
        private static void Prefix(Piece __instance)
        {
            if (DevConfig.EnableDebugLogging == null)
            {
                return;
            }

            if (!DevConfig.EnableDebugLogging.Value)
            {
                return;
            }

            if (__instance.gameObject.name.Replace("(Clone)", string.Empty) != CraftingStationManager.WorkbenchPrefabName)
            {
                return;
            }

            bool freeBuild = ZoneSystem.instance.GetGlobalKey(__instance.FreeBuildKey());

            DebugLog.LogInfo($"Mage Table resource return: free build={freeBuild}, player placed={__instance.IsPlacedByPlayer()}, drop height={__instance.m_returnResourceHeightOffset}.");

            foreach (var requirement in __instance.m_resources)
            {
                string resourceName = requirement.m_resItem == null ? "missing" : requirement.m_resItem.name;

                DebugLog.LogInfo($"Mage Table resource: {resourceName}, amount={requirement.m_amount}, recover={requirement.m_recover}.");
            }
        }
    }
}


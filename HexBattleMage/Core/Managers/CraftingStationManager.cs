using HexBattleMage.Core.Visuals;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    internal static class CraftingStationManager
    {
        internal const string WorkbenchPrefabName = "HexArcaneWorkbench";
        internal const string CraftingPieceCategory = "Battle Mage";
        private const string BaseWorkbenchPrefabName = "piece_workbench";
        private const int WoodCost = 10;
        private const int StoneCost = 5;
        private static GameObject _workbenchPrefab;

        internal static void Initialize()
        {
            PrefabManager.OnVanillaPrefabsAvailable += RegisterWorkbench;
            PrefabManager.OnPrefabsRegistered += AddWorkbenchVisuals;
        }

        internal static void Shutdown()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterWorkbench;
            PrefabManager.OnPrefabsRegistered -= AddWorkbenchVisuals;
        }

        private static void RegisterWorkbench()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterWorkbench;

            var config = new PieceConfig
            {
                Name = ModLocalization.MageTableName,
                Description = ModLocalization.MageTableDescription,
                PieceTable = "Hammer",
                Category = CraftingPieceCategory,
                Usage = new[] { PieceUsages.Crafting },
                Requirements = new[]
                {
                    new RequirementConfig("Wood", WoodCost, recover: true),
                    new RequirementConfig("Stone", StoneCost, recover: true)
                }
            };

            var workbench = new CustomPiece(WorkbenchPrefabName, BaseWorkbenchPrefabName, config);

            if (workbench.PiecePrefab == null)
            {
                DebugLog.LogError("Could not clone the vanilla workbench.");
                return;
            }

            var station = workbench.PiecePrefab.GetComponent<CraftingStation>();

            if (station == null)
            {
                DebugLog.LogError("The cloned workbench is missing its CraftingStation component.");
                Object.Destroy(workbench.PiecePrefab);
                return;
            }

            station.m_name = ModLocalization.MageTableName;
            station.m_showBasicRecipies = false;
            workbench.Piece.m_craftingStation = null;
            MageRunestoneStructure.ConfigureCollision(workbench.PiecePrefab);

            if (!PieceManager.Instance.AddPiece(workbench))
            {
                DebugLog.LogError("Could not register the Hex Arcane Workbench.");
                Object.Destroy(workbench.PiecePrefab);
                return;
            }

            _workbenchPrefab = workbench.PiecePrefab;
            DebugLog.LogInfo("Hex Arcane Workbench registered under Battle Mage with the Crafting usage tag.");
        }

        private static void AddWorkbenchVisuals()
        {
            if (_workbenchPrefab == null)
            {
                return;
            }

            if (!ArcaneWorkbenchVisuals.AddTo(_workbenchPrefab))
            {
                return;
            }

            if (!GUIManager.IsHeadless())
            {
                var icon = RenderManager.Instance.Render(_workbenchPrefab, RenderManager.IsometricRotation);

                if (icon != null)
                {
                    _workbenchPrefab.GetComponent<Piece>().m_icon = icon;
                    _workbenchPrefab.GetComponent<CraftingStation>().m_icon = icon;
                }
            }

            PrefabManager.OnPrefabsRegistered -= AddWorkbenchVisuals;
        }
    }
}

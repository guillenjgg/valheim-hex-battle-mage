using HexBattleMage.Core.Visuals;
using Jotunn.Configs;
using HexBattleMage.Assets;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    internal static class CraftingStationManager
    {
        internal const string WorkbenchPrefabName = "HexArcaneWorkbench";
        private static GameObject _workbenchPrefab;

        internal static bool IsMageTable(CraftingStation station)
        {
            if (station == null)
            {
                return false;
            }

            return station.m_name == PrefabSettings.Require<MageTableProfile>(AssetBundleHelper.HexArcaneWorkbench).DisplayName;
        }

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

            var profile = PrefabSettings.Require<MageTableProfile>(AssetBundleHelper.HexArcaneWorkbench);
            var recipe = PrefabSettings.Require<MageRecipeProfile>(AssetBundleHelper.HexArcaneWorkbench);
            var config = new PieceConfig
            {
                Name = profile.DisplayName,
                Description = profile.Description,
                PieceTable = profile.PieceTable,
                Category = profile.Category,
                Usage = new[] { PieceUsages.Crafting },
                Requirements = PrefabSettings.GetRequirements(recipe)
            };

            var workbench = new CustomPiece(AssetBundleHelper.HexArcaneWorkbench, true, config);

            if (workbench.PiecePrefab == null)
            {
                DebugLog.LogError("The bundled Mage Table prefab is unavailable.");
                return;
            }

            var station = workbench.PiecePrefab.GetComponent<CraftingStation>();

            if (station == null)
            {
                DebugLog.LogError("The bundled Mage Table is missing its CraftingStation component.");
                Object.Destroy(workbench.PiecePrefab);
                return;
            }

            station.m_name = profile.DisplayName;
            station.m_showBasicRecipies = profile.ShowBasicRecipes;
            workbench.Piece.m_craftingStation = null;

            if (!PieceManager.Instance.AddPiece(workbench))
            {
                DebugLog.LogError("Could not register the Hex Arcane Workbench.");
                Object.Destroy(workbench.PiecePrefab);
                return;
            }

            _workbenchPrefab = workbench.PiecePrefab;
            RegisterExtensions();
            DebugLog.LogInfo("Hex Arcane Workbench registered under Battle Mage with the Crafting usage tag.");
        }

        private static void RegisterExtensions()
        {
            foreach (string name in new[] { "HexMageRuneStone", "HexMageReagentStand" })
            {
                var prefab = AssetBundleHelper.Bundle.LoadAsset<GameObject>(name);

                if (prefab == null)
                {
                    DebugLog.LogError($"Missing Mage Table extension '{name}'. Rebuild the asset bundle.");
                    continue;
                }

                var piece = prefab.GetComponent<Piece>();
                var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);
                var tableProfile = PrefabSettings.Require<MageTableProfile>(_workbenchPrefab);
                var requirements = PrefabSettings.GetRequirements(recipe);
                var config = new PieceConfig
                {
                    Name = piece.m_name,
                    Description = piece.m_description,
                    PieceTable = tableProfile.PieceTable,
                    Category = tableProfile.Category,
                    CraftingStation = recipe.CraftingStation,
                    Requirements = requirements
                };
                var extension = new CustomPiece(prefab, true, config);

                extension.PiecePrefab.GetComponent<StationExtension>().m_craftingStation = _workbenchPrefab.GetComponent<CraftingStation>();

                if (!PieceManager.Instance.AddPiece(extension))
                {
                    DebugLog.LogError($"Could not register Mage Table extension '{name}'.");
                }
            }
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
                var piece = _workbenchPrefab.GetComponent<Piece>();
                var icon = piece.m_icon;

                if (icon == null)
                {
                    icon = RenderManager.Instance.Render(_workbenchPrefab, RenderManager.IsometricRotation);
                }

                if (icon != null)
                {
                    piece.m_icon = icon;
                    _workbenchPrefab.GetComponent<CraftingStation>().m_icon = icon;
                }
            }

            PrefabManager.OnPrefabsRegistered -= AddWorkbenchVisuals;
        }
    }
}

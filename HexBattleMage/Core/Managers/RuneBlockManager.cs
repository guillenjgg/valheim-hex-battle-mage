using HexBattleMage.Assets;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    internal static class RuneBlockManager
    {
        internal const string PrefabName = HexBattleMageRuneBlockProfile.PrefabName;

        internal static bool IsRuneBlock(CraftingStation station)
        {
            return station != null && station.GetComponent<HexBattleMageRuneBlockProfile>() != null;
        }

        internal static void Initialize()
        {
            PrefabManager.OnVanillaPrefabsAvailable += Register;
        }

        internal static void Shutdown()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;
        }

        private static void Register()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;
            var prefab = AssetBundleHelper.Bundle.LoadAsset<GameObject>(PrefabName);
            var profile = PrefabSettings.Require<HexBattleMageRuneBlockProfile>(prefab);
            var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);
            var config = new PieceConfig
            {
                Name = profile.DisplayName,
                Description = profile.Description,
                PieceTable = profile.PieceTable,
                Category = profile.Category,
                Usage = new[] { PieceUsages.Crafting },
                Requirements = PrefabSettings.GetRequirements(recipe)
            };
            var piece = new CustomPiece(prefab, true, config);

            piece.Piece.m_craftingStation = null;
            piece.PiecePrefab.GetComponent<CraftingStation>().m_name = profile.DisplayName;
            piece.PiecePrefab.AddComponent<RuneBlockVisuals>();

            if (!PieceManager.Instance.AddPiece(piece))
            {
                DebugLog.LogError("Could not register Rune-Whittler's Block.");
                return;
            }

            DebugLog.LogInfo("Rune-Whittler's Block registered: one boss-progressed station, no extension pieces.");
        }
    }
}

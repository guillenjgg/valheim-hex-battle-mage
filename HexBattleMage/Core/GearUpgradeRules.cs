using System.Collections.Generic;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Core
{
    internal static class GearUpgradeRules
    {
        private static readonly Dictionary<Piece.Requirement, MageUpgradeProfile> Requirements = new Dictionary<Piece.Requirement, MageUpgradeProfile>();

        internal static void Register(Recipe recipe, MageUpgradeProfile profile)
        {
            foreach (var requirement in recipe.m_resources)
            {
                Requirements[requirement] = profile;
            }
        }

        internal static bool UsesFixedCosts(Piece.Requirement requirement)
        {
            return Requirements.TryGetValue(requirement, out var profile) && profile != null && profile.FixedResourceCosts;
        }

        internal static void Clear()
        {
            Requirements.Clear();
        }
    }
}

using System.Collections.Generic;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core
{
    internal static class GearUpgradeRules
    {
        private static readonly Dictionary<Piece.Requirement, MageUpgradeProfile> Requirements = new Dictionary<Piece.Requirement, MageUpgradeProfile>();

        internal static void CopyProfile(GameObject source, GameObject target)
        {
            var sourceProfile = source.GetComponent<MageUpgradeProfile>();

            if (sourceProfile == null)
            {
                return;
            }

            var targetProfile = target.GetComponent<MageUpgradeProfile>();

            if (targetProfile == null)
            {
                targetProfile = target.AddComponent<MageUpgradeProfile>();
            }

            targetProfile.UseFixedStationLevel = sourceProfile.UseFixedStationLevel;
            targetProfile.RequiredStationLevel = sourceProfile.RequiredStationLevel;
            targetProfile.FixedResourceCosts = sourceProfile.FixedResourceCosts;
        }

        internal static void Register(Recipe recipe, MageUpgradeProfile profile)
        {
            if (profile == null)
            {
                return;
            }

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

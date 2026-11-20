using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Tuning
{
    internal static class CloakTuning
    {
        private const string MeadowsCloak = "HexBattleMage_Cloak_Meadows_01";

        internal static void Apply(GameObject prefab)
        {
            var cloak = prefab.GetComponent<HexBattleMageCloakProfile>();

            if (cloak == null)
            {
                return;
            }

            if (cloak.ItemPrefabName == MeadowsCloak)
            {
                Meadows01(prefab, cloak);
            }
        }

        private static void Meadows01(GameObject prefab, HexBattleMageCloakProfile cloak)
        {
            cloak.Armor = 2f;
            cloak.ArmorPerQuality = 1f;
            cloak.MaximumQuality = 3;
            cloak.FrostResistance = true;
            PrefabSettings.Require<HexBattleMageVitalityProfile>(prefab).MaximumStamina = 5f;

            var eitr = PrefabSettings.Require<MageEitrProfile>(prefab);

            eitr.MaximumEitr = 5f;
            eitr.MaximumEitrPerQuality = 1f;

            var upgrades = PrefabSettings.Require<MageUpgradeProfile>(prefab);

            upgrades.FixedResourceCosts = true;
            upgrades.UseFixedStationLevel = true;
            upgrades.RequiredStationLevel = 3;

            var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);

            recipe.MinimumStationLevel = 1;
            recipe.Resources = new[]
            {
                new MageRecipeResource { ItemPrefab = "LeatherScraps", Amount = 6, AmountPerQuality = 3 },
                new MageRecipeResource { ItemPrefab = "DeerHide", Amount = 4, AmountPerQuality = 2 },
                new MageRecipeResource { ItemPrefab = "Resin", Amount = 5, AmountPerQuality = 2 }
            };
        }
    }
}

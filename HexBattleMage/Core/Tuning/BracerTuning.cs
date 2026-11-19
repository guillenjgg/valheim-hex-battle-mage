using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Tuning
{
    internal static class BracerTuning
    {
        private const string MeadowsBracerVisual = "HexMagicBracerVisual";

        internal static void Apply(GameObject prefab)
        {
            if (prefab.name != MeadowsBracerVisual)
            {
                return;
            }

            Meadows01(PrefabSettings.Require<MagicBracerProfile>(prefab));

            var upgrades = PrefabSettings.Require<MageUpgradeProfile>(prefab);
            var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);

            upgrades.FixedResourceCosts = true;
            upgrades.UseFixedStationLevel = false;
            recipe.Resources = new[]
            {
                new MageRecipeResource { ItemPrefab = "Wood", Amount = 10, AmountPerQuality = 5 },
                new MageRecipeResource { ItemPrefab = "Resin", Amount = 4, AmountPerQuality = 2 },
                new MageRecipeResource { ItemPrefab = "LeatherScraps", Amount = 2, AmountPerQuality = 1 }
            };
        }

        private static void Meadows01(MagicBracerProfile bracer)
        {
            bracer.BlockPower = 6f;
            bracer.BlockPowerPerQuality = 6f;
            bracer.BlockForce = 20f;
            bracer.BlockForcePerQuality = 5f;
            bracer.ParryBonus = 1.5f;
        }
    }
}

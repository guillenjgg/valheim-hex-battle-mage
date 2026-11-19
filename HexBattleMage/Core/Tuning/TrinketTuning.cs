using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Trinkets;
using UnityEngine;

namespace HexBattleMage.Core.Tuning
{
    internal static class TrinketTuning
    {
        private const string MeadowsTrinket = "HexBattleMage_Trinket_Meadows_01";

        internal static void Apply(GameObject prefab)
        {
            var trinket = prefab.GetComponent<HexBattleMageTrinketProfile>();

            if (trinket == null)
            {
                return;
            }

            if (trinket.ItemPrefabName == MeadowsTrinket)
            {
                Meadows01(prefab, trinket);
            }
        }

        private static void Meadows01(GameObject prefab, HexBattleMageTrinketProfile trinket)
        {
            var eitr = PrefabSettings.Require<MageEitrProfile>(prefab);

            eitr.MaximumEitr = 25f;
            eitr.RegenerationModifier = 0.15f;
            trinket.WandDamageBonus = 0.25f;
            trinket.BossParryDamageBonus = 0.10f;
            trinket.ParryDuration = 2f;
            trinket.MaximumQuality = 1;

            var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);

            recipe.Resources = new[]
            {
                new MageRecipeResource { ItemPrefab = "TrophyBoar", Amount = 1 },
                new MageRecipeResource { ItemPrefab = "Stone", Amount = 10 },
                new MageRecipeResource { ItemPrefab = "BoneFragments", Amount = 5 },
                new MageRecipeResource { ItemPrefab = "LeatherScraps", Amount = 2 }
            };
        }
    }
}

using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Tuning
{
    internal static class WandTuning
    {
        private const string MeadowsWand = "HexBattleMage_Wand_Meadows_01";

        internal static void Apply(GameObject prefab)
        {
            if (prefab.name != MeadowsWand)
            {
                return;
            }

            Meadows01(prefab);
        }

        private static void Meadows01(GameObject prefab)
        {
            var wand = PrefabSettings.Require<HexBattleMageWandProfile>(prefab);

            wand.WandBluntDamage = 5f;
            wand.WandPierceDamage = 5f;
            wand.WandBluntDamagePerQuality = 1f;
            wand.WandPierceDamagePerQuality = 1f;

            var item = PrefabSettings.Require<ItemDrop>(prefab).m_itemData.m_shared;

            item.m_maxQuality = 4;

            var upgrades = PrefabSettings.Require<MageUpgradeProfile>(prefab);
            var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);

            upgrades.FixedResourceCosts = true;
            upgrades.UseFixedStationLevel = false;
            recipe.Resources = new[]
            {
                new MageRecipeResource { ItemPrefab = "Wood", Amount = 20, AmountPerQuality = 10 },
                new MageRecipeResource { ItemPrefab = "Resin", Amount = 2, AmountPerQuality = 1 }
            };

            foreach (var element in wand.Elements)
            {
                if (element.Element == WandElement.Fire)
                {
                    element.DirectDamage = 15f;
                    element.DamagePerQuality = 2f;
                }
            }
        }
    }
}

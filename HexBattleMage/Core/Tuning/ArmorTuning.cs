using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Tuning
{
    internal static class ArmorTuning
    {
        private const string MeadowsHelmet = "HexBattleMage_Helmet_Meadows_01";
        private const string MeadowsChest = "HexBattleMage_Chest_Meadows_01";
        private const string MeadowsLegs = "HexBattleMage_Legs_Meadows_01";

        internal static void Apply(GameObject prefab)
        {
            var armor = prefab.GetComponent<HexBattleMageArmorProfile>();

            if (armor == null)
            {
                return;
            }

            switch (armor.ItemPrefabName)
            {
                case MeadowsHelmet:
                    HelmetMeadows01(prefab, armor);
                    break;
                case MeadowsChest:
                    ChestMeadows01(prefab, armor);
                    break;
                case MeadowsLegs:
                    LegsMeadows01(prefab, armor);
                    break;
            }
        }

        private static void ApplyMeadowsQuality(GameObject prefab, HexBattleMageArmorProfile armor)
        {
            armor.MaximumQuality = 4;
            armor.ArmorPerQuality = 2f;

            var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);

            var upgrades = PrefabSettings.Require<MageUpgradeProfile>(prefab);

            upgrades.FixedResourceCosts = true;
            upgrades.UseFixedStationLevel = false;
            recipe.MinimumStationLevel = 1;
            recipe.Resources = new[]
            {
                new MageRecipeResource { ItemPrefab = "DeerHide", Amount = 6, AmountPerQuality = 3 },
                new MageRecipeResource { ItemPrefab = "Resin", Amount = 2, AmountPerQuality = 1 }
            };
        }

        private static void HelmetMeadows01(GameObject prefab, HexBattleMageArmorProfile armor)
        {
            ApplyMeadowsQuality(prefab, armor);
            PrefabSettings.Require<HexBattleMageVitalityProfile>(prefab).MaximumHealth = 2f;
        }

        private static void ChestMeadows01(GameObject prefab, HexBattleMageArmorProfile armor)
        {
            ApplyMeadowsQuality(prefab, armor);
            PrefabSettings.Require<HexBattleMageVitalityProfile>(prefab).MaximumHealth = 2f;
        }

        private static void LegsMeadows01(GameObject prefab, HexBattleMageArmorProfile armor)
        {
            ApplyMeadowsQuality(prefab, armor);
            PrefabSettings.Require<HexBattleMageVitalityProfile>(prefab).MaximumHealth = 2f;
        }
    }
}

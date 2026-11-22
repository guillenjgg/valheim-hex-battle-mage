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

            wand.WeaponSkill = Skills.SkillType.Clubs;
            wand.SkillGain = 1f;
            wand.AttackStamina = 5f;
            wand.WandBluntDamage = 5f;
            wand.WandPierceDamage = 5f;
            wand.WandBluntDamagePerQuality = 1f;
            wand.WandPierceDamagePerQuality = 1f;

            var item = PrefabSettings.Require<ItemDrop>(prefab).m_itemData.m_shared;

            item.m_maxQuality = 4;
            item.m_skillType = wand.WeaponSkill;

            var upgrades = PrefabSettings.Require<MageUpgradeProfile>(prefab);
            var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);

            upgrades.FixedResourceCosts = true;
            upgrades.UseFixedStationLevel = false;
            recipe.CraftingStation = string.Empty;
            recipe.RepairStation = RuneBlockManager.PrefabName;
            recipe.Resources = new[]
            {
                new MageRecipeResource { ItemPrefab = "Wood", Amount = 10, AmountPerQuality = 5 },
                new MageRecipeResource { ItemPrefab = "Stone", Amount = 2, AmountPerQuality = 1 }
            };

            recipe.MinimumStationLevel = 1;
            ConfigureUpgrades(wand);

            foreach (var element in wand.Elements)
            {
                WandElementTuning.Apply(element);
            }
        }

        private static void ConfigureUpgrades(HexBattleMageWandProfile wand)
        {
            wand.CanCraftAtStation = true;
            var upgradeRecipe = PrefabSettings.Require<MageRecipeProfile>(wand.UpgradeRecipe);

            upgradeRecipe.ApplyBakedDefaults();
            upgradeRecipe.CraftingStation = RuneBlockManager.PrefabName;
            upgradeRecipe.RepairStation = RuneBlockManager.PrefabName;
            upgradeRecipe.MinimumStationLevel = 1;
            upgradeRecipe.Resources = new[]
            {
                new MageRecipeResource { ItemPrefab = "Wood", Amount = 10, AmountPerQuality = 5 },
                new MageRecipeResource { ItemPrefab = "Stone", Amount = 2, AmountPerQuality = 1 }
            };
        }

    }
}

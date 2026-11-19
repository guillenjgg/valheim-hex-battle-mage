using BepInEx.Configuration;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Tuning
{
    internal static class CraftingTuning
    {
        private const string RuneStone = "HexMageRuneStone";
        private const string ReagentStand = "HexMageReagentStand";

        internal static void Apply(GameObject prefab)
        {
            switch (prefab.name)
            {
                case RuneStone:
                    PrefabSettings.Require<MageRecipeProfile>(prefab).Resources = new[]
                    {
                        new MageRecipeResource { ItemPrefab = "Wood", Amount = 10 },
                        new MageRecipeResource { ItemPrefab = "Flint", Amount = 10 }
                    };
                    break;
                case ReagentStand:
                    PrefabSettings.Require<MageRecipeProfile>(prefab).Resources = new[]
                    {
                        new MageRecipeResource { ItemPrefab = "Wood", Amount = 10 },
                        new MageRecipeResource { ItemPrefab = "Flint", Amount = 15 },
                        new MageRecipeResource { ItemPrefab = "LeatherScraps", Amount = 20 },
                        new MageRecipeResource { ItemPrefab = "DeerHide", Amount = 5 }
                    };
                    break;
            }
        }

        internal static void Configure(ConfigFile config, AssetBundle bundle)
        {
            foreach (string identity in new[] { RuneStone, ReagentStand })
            {
                var prefab = bundle.LoadAsset<GameObject>(identity);
                var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);
                string section = "Crafting / Mage Table / " + identity;

                foreach (var resource in recipe.GetResources())
                {
                    var amount = config.Bind(section, resource.ItemPrefab, resource.Amount, new ConfigDescription("Construction cost. Requires a game restart. Zero removes this material cost. Honours Tuning / UseConfigOverrides.", new AcceptableValueRange<int>(0, 1000)));

                    resource.Amount = amount.EffectiveValue();
                }
            }
        }
    }
}

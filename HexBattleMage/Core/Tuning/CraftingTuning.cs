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
                case RuneBlockManager.PrefabName:
                    RuneBlock(prefab);
                    break;
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

        private static void RuneBlock(GameObject prefab)
        {
            var profile = PrefabSettings.Require<HexBattleMageRuneBlockProfile>(prefab);

            profile.BodyColliderCenter = new Vector3(0f, 0.39f, 0f);
            profile.BodyColliderSize = new Vector3(1.34f, 0.78f, 1.34f);
            var body = PrefabSettings.Require<BoxCollider>(prefab);

            body.center = profile.BodyColliderCenter;
            body.size = profile.BodyColliderSize;
            profile.BaseLevel = 1;
            profile.RefreshInterval = 1f;
            profile.BossDefeatKeys = new[]
            {
                "defeated_eikthyr", "defeated_gdking", "defeated_bonemass", "defeated_dragon",
                "defeated_goblinking", "defeated_queen", "defeated_fader"
            };

            var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);

            recipe.CraftingStation = string.Empty;
            recipe.Resources = new[] { new MageRecipeResource { ItemPrefab = "Wood", Amount = 10 } };
        }

        internal static void Configure(ConfigFile config, AssetBundle bundle)
        {
            foreach (string identity in new[] { RuneStone, ReagentStand, RuneBlockManager.PrefabName })
            {
                var prefab = bundle.LoadAsset<GameObject>(identity);
                var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);
                string section = identity == RuneBlockManager.PrefabName ? "Crafting / Rune Block" : "Crafting / Mage Table / " + identity;

                foreach (var resource in recipe.GetResources())
                {
                    var amount = config.Bind(section, resource.ItemPrefab, resource.Amount, new ConfigDescription("Construction cost. Requires a game restart. Zero removes this material cost. Honours Tuning / UseConfigOverrides.", new AcceptableValueRange<int>(0, 1000)));

                    resource.Amount = amount.EffectiveValue();
                }
            }
        }
    }
}

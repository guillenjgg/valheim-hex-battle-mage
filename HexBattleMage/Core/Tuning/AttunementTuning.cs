using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Tuning
{
    internal static class AttunementTuning
    {
        internal static void Apply(GameObject prefab)
        {
            var profile = prefab.GetComponent<HexBattleMageRuneBlockProfile>();

            if (profile == null)
            {
                return;
            }

            profile.MaximumActiveAttunements = 1;

            foreach (var reference in profile.Attunements)
            {
                var definition = PrefabSettings.Require<HexBattleMageWandAttunementProfile>(reference);

                if (definition.Element != WandElement.Fire)
                {
                    continue;
                }

                var recipe = PrefabSettings.Require<MageRecipeProfile>(reference);

                recipe.ApplyBakedDefaults();
                recipe.CraftingStation = RuneBlockManager.PrefabName;
                recipe.MinimumStationLevel = 1;
                recipe.Resources = new[]
                {
                    new MageRecipeResource { ItemPrefab = "Resin", Amount = 2 },
                    new MageRecipeResource { ItemPrefab = "Feathers", Amount = 3 },
                    new MageRecipeResource { ItemPrefab = "Wood", Amount = 10 }
                };
            }
        }
    }
}

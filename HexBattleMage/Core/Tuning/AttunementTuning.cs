using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Tuning
{
    internal static class AttunementTuning
    {
        internal static AttunementPreview[] PreviewCatalog()
        {
            return new[]
            {
                new AttunementPreview { Id = "Spirit", DisplayName = "$hexbattlemage_element_spirit" },
                new AttunementPreview { Id = "Frost", DisplayName = "$hexbattlemage_element_frost" },
                new AttunementPreview { Id = "Earth", DisplayName = "$hexbattlemage_element_earth" }
            };
        }

        internal static void Apply(GameObject prefab)
        {
            var profile = prefab.GetComponent<HexBattleMageRuneBlockProfile>();

            if (profile == null)
            {
                return;
            }

            profile.MaximumActiveAttunements = 1;
            profile.AttunementPreviews = PreviewCatalog();

            foreach (var reference in profile.Attunements)
            {
                var definition = PrefabSettings.Require<HexBattleMageWandAttunementProfile>(reference);

                if (definition.Element == WandElement.Blight)
                {
                    var blight = PrefabSettings.Require<MageRecipeProfile>(reference);

                    blight.ApplyBakedDefaults();
                    blight.CraftingStation = RuneBlockManager.PrefabName;
                    blight.MinimumStationLevel = 2;
                    blight.Resources = new[]
                    {
                        new MageRecipeResource { ItemPrefab = "TrophySkeleton", Amount = 1 },
                        new MageRecipeResource { ItemPrefab = "SurtlingCore", Amount = 1 },
                        new MageRecipeResource { ItemPrefab = "MushroomYellow", Amount = 5 }
                    };
                    continue;
                }

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

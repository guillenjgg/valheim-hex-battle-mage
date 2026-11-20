using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Tuning
{
    internal static class PrefabTuning
    {
        internal static void Apply(AssetBundle bundle)
        {
            foreach (var prefab in bundle.LoadAllAssets<GameObject>())
            {
                var recipe = prefab.GetComponent<MageRecipeProfile>();

                if (recipe != null)
                {
                    if (recipe.BakedRecipe == null)
                    {
                        recipe.BakedRecipe = bundle.LoadAsset<TextAsset>("Recipe_" + prefab.name);
                    }

                    recipe.ApplyBakedDefaults();
                }

                CraftingTuning.Apply(prefab);
                AttunementTuning.Apply(prefab);
                WandTuning.Apply(prefab);
                ArmorTuning.Apply(prefab);
                CloakTuning.Apply(prefab);
                BracerTuning.Apply(prefab);
                TrinketTuning.Apply(prefab);
            }
        }
    }
}

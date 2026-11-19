using System;
using HexBattleMage.Assets;
using Jotunn.Configs;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    internal static class PrefabSettings
    {
        internal static HexBattleMageWandProfile Wand => Require<HexBattleMageWandProfile>(AssetBundleHelper.HexBattleMageWand);
        internal static WandProjectileProfile Projectile => Require<WandProjectileProfile>(AssetBundleHelper.HexMagicBolt);

        internal static T Require<T>(GameObject prefab) where T : Component
        {
            var profile = prefab.GetComponent<T>();

            if (profile == null)
            {
                throw new InvalidOperationException(prefab.name + " is missing authored settings: " + typeof(T).Name);
            }

            return profile;
        }

        internal static RequirementConfig[] GetRequirements(MageRecipeProfile profile)
        {
            if (profile.BakedRecipe == null)
            {
                profile.BakedRecipe = AssetBundleHelper.Bundle.LoadAsset<TextAsset>("Recipe_" + profile.gameObject.name);
            }

            var resources = profile.GetResources();
            var requirements = new RequirementConfig[resources.Length];

            for (int index = 0; index < requirements.Length; index++)
            {
                var resource = resources[index];

                requirements[index] = new RequirementConfig(resource.ItemPrefab, resource.Amount, resource.AmountPerQuality, resource.Recover);
            }

            return requirements;
        }
    }
}

using System;
using HexBattleMage.Assets;
using Jotunn.Configs;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    internal static class PrefabSettings
    {
        internal static WandProfile Wand => Require<WandProfile>(AssetBundleHelper.HexBattleMageWand);
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
                string sourceName = profile.gameObject == AssetBundleHelper.HexBattleMageWand ? "HexBattleMageWand" : profile.gameObject.name;

                profile.BakedRecipe = AssetBundleHelper.Bundle.LoadAsset<TextAsset>("Recipe_" + sourceName);
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

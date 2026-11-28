using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Tuning
{
    internal static class BlackForestTuning
    {
        internal static void Apply(GameObject prefab)
        {
            var element = prefab.GetComponent<WandElementProfile>();

            if (element != null && element.Element == WandElement.Blight)
            {
                Blight(element);
            }

            var armor = prefab.GetComponent<HexBattleMageArmorProfile>();

            if (armor != null && armor.ItemPrefabName.Contains("_BlackForest_"))
            {
                Armor(prefab, armor);
            }

            var cloak = prefab.GetComponent<HexBattleMageCloakProfile>();

            if (cloak != null && cloak.ItemPrefabName == "HexBattleMage_Cloak_BlackForest_01")
            {
                cloak.Armor = 4f;
                cloak.ArmorPerQuality = 1f;
                cloak.MaximumQuality = 3;
                PrefabSettings.Require<MageEitrProfile>(prefab).MaximumEitr = 8f;
                PrefabSettings.Require<MageEitrProfile>(prefab).MaximumEitrPerQuality = 0f;
                PrefabSettings.Require<HexBattleMageVitalityProfile>(prefab).MaximumStamina = 8f;
                PrefabSettings.Require<MageRecipeProfile>(prefab).MinimumStationLevel = 2;
                PrefabSettings.Require<MageRecipeProfile>(prefab).Resources = ArmorResources();
            }

            var staff = prefab.GetComponent<HexBattleMageStaffProfile>();

            if (staff != null && staff.ItemPrefabName == "HexBattleMage_Staff_BlackForest_01")
            {
                EarthStaff(staff);
                var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);

                recipe.MinimumStationLevel = 2;
                recipe.Resources = new[]
                {
                    new MageRecipeResource { ItemPrefab = "RoundLog", Amount = 10, AmountPerQuality = 5 },
                    new MageRecipeResource { ItemPrefab = "Stone", Amount = 10, AmountPerQuality = 5 },
                    new MageRecipeResource { ItemPrefab = "Bronze", Amount = 4, AmountPerQuality = 2 }
                };
            }
        }

        private static void Blight(WandElementProfile profile)
        {
            profile.ProjectileCoreColor = new Color(0.008f, 0.018f, 0.009f);
            profile.ProjectileHaloColor = new Color(0.06f, 0.32f, 0.08f);
            profile.ImpactColor = new Color(0.07f, 0.4f, 0.1f);
            profile.TipColor = new Color(0.04f, 0.3f, 0.08f);
            profile.Duration = 3f;
            profile.TickInterval = 0.5f;
            profile.TickDamage = 10f;
            profile.TotalDamage = 60f;
            profile.SingleTargetProjectile = true;
            profile.DirectDamage = 0f;
            profile.DamagePerQuality = 0f;
            profile.SpreadRadius = 0f;
            profile.Spreads = true;
            profile.MaximumChainTargets = 128;
            profile.RequireSpreadLineOfSight = true;
            profile.BossTickMultiplier = 1f;
        }

        private static void Armor(GameObject prefab, HexBattleMageArmorProfile profile)
        {
            profile.Armor = 6f;
            profile.ArmorPerQuality = 2f;
            profile.MaximumQuality = 4;
            var eitr = PrefabSettings.Require<MageEitrProfile>(prefab);
            var vitality = PrefabSettings.Require<HexBattleMageVitalityProfile>(prefab);
            var recipe = PrefabSettings.Require<MageRecipeProfile>(prefab);

            eitr.MaximumEitr = 8f;
            eitr.MaximumEitrPerQuality = 0f;
            vitality.MaximumHealth = 4f;
            recipe.MinimumStationLevel = 2;
            recipe.Resources = ArmorResources();
        }

        private static MageRecipeResource[] ArmorResources()
        {
            return new[]
            {
                new MageRecipeResource { ItemPrefab = "TrollHide", Amount = 5, AmountPerQuality = 2 },
                new MageRecipeResource { ItemPrefab = "DeerHide", Amount = 4, AmountPerQuality = 2 },
                new MageRecipeResource { ItemPrefab = "Bronze", Amount = 2, AmountPerQuality = 1 }
            };
        }

        private static void EarthStaff(HexBattleMageStaffProfile profile)
        {
            profile.BluntDamage = 30f;
            profile.PickaxeDamage = 20f;
            profile.BluntPerQuality = 6f;
            profile.PickaxePerQuality = 4f;
            profile.ProjectileVisualScale = 2.75f;
            profile.ProjectileCollisionRadius = 0.22f;
            profile.SuppressNativeCinders = true;
            profile.StaminaCost = 5f;
            profile.EitrCost = 8f;
            profile.ToolTier = 1;
            profile.MaximumQuality = 3;
        }
    }
}

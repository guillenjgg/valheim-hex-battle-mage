using System;
using BepInEx.Configuration;
using HarmonyLib;
using HexBattleMage.Assets;
using HexBattleMage.Core.Visuals;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HexBattleMage.Core.Managers
{
    internal static class StoneTrinketManager
    {
        internal const string VisualPrefabName = "HexStoneParryTrinketVisual";
        internal const string BuffName = "HexStoneParryEmpowerment";
        private const string ConfigSection = "Equipment / Stone Trinket";
        private const float MinimumRewardDuration = 0.01f;
        private static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> TrinketSlot = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_trinketItem");
        private static readonly Action<Player, float, bool> SetMaximumEitr = AccessTools.MethodDelegate<Action<Player, float, bool>>(AccessTools.Method(typeof(Player), "SetMaxEitr"));
        private delegate void FoodValues(Player player, ref float health, ref float stamina, ref float eitr);
        private static readonly FoodValues ReadFoodValues = AccessTools.MethodDelegate<FoodValues>(AccessTools.Method(typeof(Player), "GetTotalFoodValue"));
        private static int _buffHash;
        private static ConfigEntry<float> _maximumEitr;
        private static ConfigEntry<float> _regeneration;
        private static ConfigEntry<float> _damageBonus;
        private static ConfigEntry<float> _duration;

        internal static void Initialize(ConfigFile config)
        {
            var prefab = AssetBundleHelper.Bundle.LoadAsset<GameObject>(VisualPrefabName);
            var profile = PrefabSettings.Require<StoneTrinketProfile>(prefab);
            var eitr = PrefabSettings.Require<MageEitrProfile>(prefab);

            _maximumEitr = config.Bind(ConfigSection, "MaximumEitr", eitr.MaximumEitr, "Flat maximum Eitr while equipped. Regenerates without Eitr food.");
            _regeneration = config.Bind(ConfigSection, "RegenerationModifier", eitr.RegenerationModifier, "Extra Eitr regeneration; 0.15 means +15%.");
            _damageBonus = config.Bind(ConfigSection, "ParryWandDamageBonus", profile.WandDamageBonus, "Wand shot damage bonus after a successful parry; 0.25 means +25%.");
            _duration = config.Bind(ConfigSection, "ParryDuration", profile.ParryDuration, "Parry reward duration in seconds. Further parries refresh it without stacking.");
            PrefabManager.OnVanillaPrefabsAvailable += Register;
        }

        internal static void Shutdown()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;
        }

        internal static StoneTrinketProfile GetProfile(ItemDrop.ItemData item)
        {
            if (item?.m_dropPrefab == null)
            {
                return null;
            }

            return item.m_dropPrefab.GetComponent<StoneTrinketProfile>();
        }

        internal static ItemDrop.ItemData GetEquipped(Humanoid humanoid)
        {
            var item = TrinketSlot(humanoid);

            if (GetProfile(item) == null)
            {
                return null;
            }

            if (!item.m_equipped)
            {
                return null;
            }

            return humanoid.GetInventory().ContainsItem(item) ? item : null;
        }

        internal static float EitrBonus(Humanoid humanoid, bool regeneration)
        {
            float total = 0f;

            foreach (var item in humanoid.GetInventory().GetAllItems())
            {
                if (!item.m_equipped)
                {
                    continue;
                }

                if (item.m_dropPrefab == null)
                {
                    continue;
                }

                var profile = item.m_dropPrefab.GetComponent<MageEitrProfile>();

                if (profile == null)
                {
                    continue;
                }

                if (GetProfile(item) != null)
                {
                    if (GetEquipped(humanoid) != item)
                    {
                        continue;
                    }

                    total += Mathf.Max(0f, regeneration ? _regeneration.Value : _maximumEitr.Value);
                    continue;
                }

                total += Mathf.Max(0f, regeneration ? profile.RegenerationModifier : profile.MaximumEitr);
            }

            return total;
        }

        internal static void RefreshEitr(Humanoid humanoid)
        {
            var player = humanoid as Player;

            if (player == null)
            {
                return;
            }

            float health = 0f;
            float stamina = 0f;
            float eitr = 0f;

            ReadFoodValues(player, ref health, ref stamina, ref eitr);
            SetMaximumEitr(player, eitr, false);
            ClearReward(player);
        }

        internal static void ClearReward(Humanoid humanoid)
        {
            humanoid.GetSEMan()?.RemoveStatusEffect(_buffHash, true);
        }

        internal static void RewardParry(Humanoid humanoid)
        {
            var player = humanoid as Player;

            if (player == null)
            {
                return;
            }

            if (player.IsDead())
            {
                return;
            }

            if (GetEquipped(player) == null)
            {
                return;
            }

            var effect = player.GetSEMan().AddStatusEffect(_buffHash, true);

            if (effect != null)
            {
                effect.m_ttl = Mathf.Max(MinimumRewardDuration, _duration.Value);
            }
        }

        internal static float DamageMultiplier(Humanoid humanoid)
        {
            if (GetEquipped(humanoid) == null)
            {
                return 1f;
            }

            if (humanoid.IsDead())
            {
                return 1f;
            }

            return humanoid.GetSEMan().HaveStatusEffect(_buffHash) ? 1f + Mathf.Max(0f, _damageBonus.Value) : 1f;
        }

        internal static string Tooltip()
        {
            return $"\n<color=orange>Maximum Eitr: +{Mathf.Max(0f, _maximumEitr.Value):0.#}\nEitr regeneration: +{Mathf.Max(0f, _regeneration.Value) * 100f:0.#}%\nSuccessful parry: +{Mathf.Max(0f, _damageBonus.Value) * 100f:0.#}% wand damage for {Mathf.Max(MinimumRewardDuration, _duration.Value):0.#}s</color>";
        }

        private static void Register()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;

            var visual = AssetBundleHelper.Bundle.LoadAsset<GameObject>(VisualPrefabName);
            var profile = PrefabSettings.Require<StoneTrinketProfile>(visual);
            var eitr = PrefabSettings.Require<MageEitrProfile>(visual);
            var recipe = PrefabSettings.Require<MageRecipeProfile>(visual);
            var definition = PrefabSettings.Require<MageEquipmentDefinition>(visual);
            var requirements = PrefabSettings.GetRequirements(recipe);

            if (profile.Icon == null)
            {
                DebugLog.LogError("Cannot register stone trinket: its authored icon is missing.");
                return;
            }

            var prefab = PrefabManager.Instance.CreateClonedPrefab(profile.ItemPrefabName, profile.BaseItemPrefab);

            if (prefab == null)
            {
                DebugLog.LogError("Cannot clone the stone trinket's authored base item.");
                return;
            }

            for (int index = prefab.transform.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(prefab.transform.GetChild(index).gameObject);
            }

            foreach (var collider in prefab.GetComponents<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            var authoredProfile = prefab.AddComponent<StoneTrinketProfile>();

            foreach (var field in typeof(StoneTrinketProfile).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            {
                field.SetValue(authoredProfile, field.GetValue(profile));
            }
            var authoredEitr = prefab.AddComponent<MageEitrProfile>();

            authoredEitr.MaximumEitr = eitr.MaximumEitr;
            authoredEitr.RegenerationModifier = eitr.RegenerationModifier;

            var authoredDefinition = prefab.AddComponent<MageEquipmentDefinition>();

            authoredDefinition.Category = definition.Category;
            authoredDefinition.Slot = definition.Slot;
            authoredDefinition.ProgressionTier = definition.ProgressionTier;
            authoredDefinition.CatalogBiome = definition.CatalogBiome;

            var model = Object.Instantiate(visual, prefab.transform, false);

            model.name = "model";
            model.SetActive(true);

            var attach = new GameObject("attach_" + profile.AttachmentBone).transform;

            attach.SetParent(prefab.transform, false);

            var worn = Object.Instantiate(visual, attach, false).transform;

            worn.localPosition = profile.WornPosition;
            worn.localRotation = Quaternion.Euler(profile.WornRotation);
            worn.localScale = profile.WornScale;
            worn.gameObject.AddComponent<StoneTrinketWornVisual>();
            worn.gameObject.SetActive(true);
            attach.gameObject.SetActive(false);

            var dropCollider = prefab.AddComponent<BoxCollider>();

            dropCollider.size = profile.DropColliderSize;
            dropCollider.center = profile.DropColliderCenter;

            var drop = prefab.GetComponent<ItemDrop>();
            var shared = drop.m_itemData.m_shared;

            drop.m_itemData.m_dropPrefab = prefab;
            shared.m_name = profile.DisplayName;
            shared.m_description = profile.Description;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Trinket;
            shared.m_weight = profile.Weight;
            shared.m_maxQuality = profile.MaximumQuality;
            shared.m_maxStackSize = profile.MaximumStackSize;
            shared.m_useDurability = profile.UseDurability;
            shared.m_maxDurability = profile.Durability;
            shared.m_durabilityPerLevel = profile.DurabilityPerQuality;
            shared.m_durabilityDrain = profile.DurabilityDrain;
            shared.m_movementModifier = profile.MovementModifier;
            shared.m_value = profile.Value;
            drop.m_itemData.m_durability = profile.Durability;
            shared.m_icons = new[] { profile.Icon };
            shared.m_equipStatusEffect = null;
            shared.m_setStatusEffect = null;
            shared.m_eitrRegenModifier = 0f;

            var effect = ScriptableObject.CreateInstance<StatusEffect>();

            effect.name = BuffName;
            _buffHash = effect.NameHash();
            effect.m_name = profile.BuffDisplayName;
            effect.m_tooltip = profile.BuffDescription;
            effect.m_icon = profile.Icon;
            effect.m_ttl = profile.ParryDuration;
            if (!ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(effect, false)))
            {
                DebugLog.LogError("Could not register the trinket's parry reward status.");
                return;
            }

            var config = new ItemConfig
            {
                CraftingStation = recipe.CraftingStation,
                RepairStation = recipe.RepairStation,
                MinStationLevel = recipe.MinimumStationLevel,
                Amount = recipe.Amount,
                Requirements = requirements
            };
            var item = new CustomItem(prefab, true, config);

            if (item.Recipe?.Recipe == null)
            {
                DebugLog.LogError("Could not create the stone trinket's authored recipe.");
                return;
            }

            if (!ItemManager.Instance.AddItem(item))
            {
                DebugLog.LogError("Could not register the stone trinket.");
                return;
            }

            MageEquipmentCatalog.Register(item.ItemDrop, item.Recipe.Recipe, (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), definition.CatalogBiome));
            DebugLog.LogInfo("Stone of Reprisal registered in the native trinket slot with Eitr and successful-parry bonuses.");
        }
    }
}

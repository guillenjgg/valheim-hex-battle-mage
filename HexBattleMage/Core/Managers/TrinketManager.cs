using HexBattleMage.Core.Tuning;
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using HexBattleMage.Assets;
using HexBattleMage.Core.Trinkets;
using HexBattleMage.Core.Visuals;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HexBattleMage.Core.Managers
{
    internal static class TrinketManager
    {
        private static readonly Dictionary<string, TrinketSettings> Settings = new Dictionary<string, TrinketSettings>(StringComparer.Ordinal);
        private static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> TrinketSlot = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_trinketItem");
        private static readonly Func<PrefabManager, GameObject> GetPrefabContainer = AccessTools.MethodDelegate<Func<PrefabManager, GameObject>>(AccessTools.PropertyGetter(typeof(PrefabManager), "PrefabContainer"));
        internal static IEnumerable<TrinketSettings> AllSettings => Settings.Values;

        internal static void Initialize(ConfigFile config)
        {
            Configure(AssetBundleHelper.Bundle.LoadAllAssets<GameObject>(), config);
            PrefabManager.OnVanillaPrefabsAvailable += RegisterAll;
        }

        internal static void Shutdown()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterAll;
            Settings.Clear();
        }

        internal static void Configure(IEnumerable<GameObject> prefabs, ConfigFile config)
        {
            var profiles = prefabs.Where(prefab => prefab.GetComponent<HexBattleMageTrinketProfile>() != null).OrderBy(prefab => prefab.GetComponent<HexBattleMageTrinketProfile>().ItemPrefabName, StringComparer.Ordinal).ToArray();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var sections = new HashSet<string>(StringComparer.Ordinal);
            var rewardNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var prefab in profiles)
            {
                var profile = prefab.GetComponent<HexBattleMageTrinketProfile>();
                var definition = PrefabSettings.Require<MageEquipmentDefinition>(prefab);

                if (string.IsNullOrWhiteSpace(profile.ItemPrefabName))
                {
                    throw new InvalidOperationException($"Trinket '{prefab.name}' needs an item prefab identity.");
                }

                if (!identities.Add(profile.ItemPrefabName))
                {
                    throw new InvalidOperationException($"Duplicate trinket identity '{profile.ItemPrefabName}'.");
                }

                if (!sections.Add(TrinketSettings.GetConfigSection(profile)))
                {
                    throw new InvalidOperationException($"Trinkets must have independent config sections: '{profile.ItemPrefabName}'.");
                }

                if (definition.Category != MageEquipmentCategory.Trinket || definition.Slot != MageEquipmentSlot.Trinket)
                {
                    throw new InvalidOperationException($"Trinket '{profile.ItemPrefabName}' must use the native trinket category and slot.");
                }

                PrefabSettings.Require<MageEitrProfile>(prefab);
                PrefabSettings.Require<MageRecipeProfile>(prefab);

                if (!Enum.IsDefined(typeof(TrinketRewardTrigger), profile.RewardTrigger))
                {
                    throw new InvalidOperationException($"Unsupported trinket reward on '{profile.ItemPrefabName}'.");
                }

                if (profile.RewardTrigger == TrinketRewardTrigger.None)
                {
                    continue;
                }

                if (!rewardNames.Add(TrinketSettings.GetRewardStatusName(profile)))
                {
                    throw new InvalidOperationException($"Trinkets must have independent reward statuses: '{profile.ItemPrefabName}'.");
                }
            }

            var configured = profiles.Select(prefab => new TrinketSettings(prefab, config)).ToArray();

            Settings.Clear();

            foreach (var settings in configured)
            {
                Settings.Add(settings.Profile.ItemPrefabName, settings);
            }
        }

        internal static HexBattleMageTrinketProfile GetProfile(ItemDrop.ItemData item)
        {
            if (item?.m_dropPrefab == null)
            {
                return null;
            }

            return item.m_dropPrefab.GetComponent<HexBattleMageTrinketProfile>();
        }

        internal static TrinketSettings GetSettings(HexBattleMageTrinketProfile profile)
        {
            if (profile == null)
            {
                return null;
            }

            return Settings.TryGetValue(profile.ItemPrefabName, out var settings) ? settings : null;
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

        private static void RegisterAll()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterAll;

            foreach (var settings in Settings.Values)
            {
                Register(settings);
            }
        }

        private static void Register(TrinketSettings settings)
        {
            var visual = settings.VisualPrefab;
            var profile = settings.Profile;
            var eitr = PrefabSettings.Require<MageEitrProfile>(visual);
            var recipe = PrefabSettings.Require<MageRecipeProfile>(visual);
            var definition = PrefabSettings.Require<MageEquipmentDefinition>(visual);
            var requirements = PrefabSettings.GetRequirements(recipe);

            if (profile.Icon == null)
            {
                DebugLog.LogError($"Cannot register trinket '{profile.ItemPrefabName}': its authored icon is missing.");
                return;
            }

            var basePrefab = PrefabManager.Instance.GetPrefab(profile.BaseItemPrefab);

            if (basePrefab == null)
            {
                DebugLog.LogError($"Cannot find base item '{profile.BaseItemPrefab}' for trinket '{profile.ItemPrefabName}'.");
                return;
            }

            // CreateClonedPrefab also checks loaded visual assets, which share this item's authored identity.
            var prefab = AssetManager.Instance.ClonePrefab(basePrefab, profile.ItemPrefabName, GetPrefabContainer(PrefabManager.Instance).transform);

            for (int index = prefab.transform.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(prefab.transform.GetChild(index).gameObject);
            }

            foreach (var collider in prefab.GetComponents<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            var authoredProfile = (HexBattleMageTrinketProfile)prefab.AddComponent(profile.GetType());

            foreach (var field in profile.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
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
            authoredDefinition.CatalogRequiresBiomeDiscovery = definition.CatalogRequiresBiomeDiscovery;
            authoredDefinition.CatalogBossDefeatKey = definition.CatalogBossDefeatKey;
            authoredDefinition.DropCollisionDetection = definition.DropCollisionDetection;

            var model = Object.Instantiate(visual, prefab.transform, false);

            model.name = "model";
            model.SetActive(true);

            var attach = new GameObject("attach_" + profile.AttachmentBone).transform;

            attach.SetParent(prefab.transform, false);

            var worn = Object.Instantiate(visual, attach, false).transform;

            worn.localPosition = profile.WornPosition;
            worn.localRotation = Quaternion.Euler(profile.WornRotation);
            worn.localScale = profile.WornScale;
            worn.gameObject.AddComponent<TrinketWornVisual>();
            worn.gameObject.SetActive(true);
            attach.gameObject.SetActive(false);

            var dropCollider = prefab.AddComponent<BoxCollider>();

            dropCollider.size = profile.DropColliderSize;
            dropCollider.center = profile.DropColliderCenter;
            WorldDropPhysics.Configure(prefab, definition.DropCollisionDetection);

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

            if (profile.RewardTrigger == TrinketRewardTrigger.SuccessfulParry)
            {
                var effect = ScriptableObject.CreateInstance<TrinketRewardStatus>();

                effect.name = settings.RewardStatusName;
                effect.m_name = profile.BuffDisplayName;
                effect.m_tooltip = profile.BuffDescription;
                effect.m_icon = profile.Icon;
                effect.m_ttl = Mathf.Max(TrinketSettings.MinimumRewardDuration, settings.Duration.EffectiveValue());

                if (!ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(effect, false)))
                {
                    DebugLog.LogError($"Could not register the reward status for '{profile.ItemPrefabName}'.");
                    return;
                }
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
                DebugLog.LogError($"Could not create the authored recipe for trinket '{profile.ItemPrefabName}'.");
                return;
            }

            if (!ItemManager.Instance.AddItem(item))
            {
                DebugLog.LogError($"Could not register trinket '{profile.ItemPrefabName}'.");
                return;
            }

            MageEquipmentCatalog.Register(item.ItemDrop, item.Recipe.Recipe, (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), definition.CatalogBiome));
            DebugLog.LogInfo($"Trinket registered: {profile.ItemPrefabName}; reward: {profile.RewardTrigger}; config: {settings.ConfigSection}.");
        }
    }
}

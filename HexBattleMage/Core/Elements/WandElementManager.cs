using HexBattleMage.Core.Tuning;
using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HexBattleMage.Assets;
using HexBattleMage.Core.Managers;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Elements
{
    internal static class WandElementManager
    {
        internal const string ProjectileElementsKey = "HexBattleMage.ProjectileElements";
        private const string Section = "Development / Elements";
        private static readonly Dictionary<WandElement, WandElementSettings> Settings = new Dictionary<WandElement, WandElementSettings>();
        private static readonly Dictionary<WandElement, StatusEffect> Effects = new Dictionary<WandElement, StatusEffect>();
        private static readonly Dictionary<int, WandElementGroupStatus> GroupEffects = new Dictionary<int, WandElementGroupStatus>();
        private static HitData.DamageTypes _originalDamage;
        private static HitData.DamageTypes _originalPerQuality;
        private static StatusEffect _originalEffect;
        private static float _originalEffectChance;

        internal static ConfigEntry<bool> Enabled { get; private set; }
        internal static ConfigEntry<bool> BypassAttunements { get; private set; }
        internal static ConfigEntry<WandElement> ActiveElement { get; private set; }

        internal static void Initialize(ConfigFile config)
        {
            Configure(config);

            foreach (StatusEffect effect in System.Linq.Enumerable.Concat<StatusEffect>(Effects.Values, GroupEffects.Values))
            {
                if (!ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(effect, false)))
                {
                    throw new InvalidOperationException("Cannot register wand status: " + effect.name);
                }
            }
        }

        internal static void Configure(ConfigFile config)
        {
            bool save = config.SaveOnConfigSet;

            config.SaveOnConfigSet = false;

            try
            {
                Enabled = config.Bind(Section, nameof(Enabled), PrefabSettings.Wand.EnableElements, "Enable the custom wand elements. Disable to use physical wand damage only. Active custom effects expire immediately when disabled.");
                BypassAttunements = config.Bind(Section, nameof(BypassAttunements), PrefabSettings.Wand.DevelopmentBypassAttunements, "Development only: bypass character attunements and use ActiveElement. Leave false for progression testing.");
                ActiveElement = config.Bind(Section, nameof(ActiveElement), PrefabSettings.Wand.DefaultElement, "Development element selector; used only when BypassAttunements is enabled. Normal play uses the character's saved active attunements.");

                var configuredElements = new HashSet<WandElement>();

                foreach (var authoredProfile in PrefabSettings.Wand.Elements)
                {
                    if (authoredProfile == null)
                    {
                        throw new InvalidOperationException("Wand element profile reference is missing.");
                    }

                    var element = authoredProfile.Element;

                    if (!configuredElements.Add(element))
                    {
                        throw new InvalidOperationException("Duplicate wand element profile: " + element);
                    }

                    string name = "HexWandElement" + element;
                    var prefab = authoredProfile.gameObject;

                    if (prefab == null)
                    {
                        throw new InvalidOperationException("Missing elemental defaults prefab: " + name);
                    }

                    var profile = prefab.GetComponent<WandElementProfile>();

                    if (profile == null)
                    {
                        throw new InvalidOperationException("Missing elemental profile on " + name);
                    }

                    Settings[element] = new WandElementSettings(config, profile);

                    StatusEffect effect;

                    if (element == WandElement.Blight)
                    {
                        effect = ScriptableObject.CreateInstance<SE_HexBlight>();
                    }
                    else
                    {
                        var elemental = ScriptableObject.CreateInstance<WandElementStatus>();

                        elemental.Element = element;
                        effect = elemental;
                    }

                    effect.name = "HexWandStatus" + element;
                    effect.m_name = profile.StatusDisplayName;
                    effect.m_ttl = profile.Duration;

                    Effects[element] = effect;
                }

                GroupEffects.Clear();
                int combinations = 1 << Enum.GetValues(typeof(WandElement)).Length;

                for (int mask = 1; mask < combinations; mask++)
                {
                    if ((mask & (mask - 1)) == 0)
                    {
                        continue;
                    }

                    var effect = ScriptableObject.CreateInstance<WandElementGroupStatus>();

                    effect.name = "HexWandElements_" + mask;
                    effect.ElementMask = mask;
                    effect.m_name = "$hexbattlemage_attunements_title";
                    GroupEffects[mask] = effect;
                }

                if (configuredElements.Count != Enum.GetValues(typeof(WandElement)).Length)
                {
                    throw new InvalidOperationException("The wand must reference every elemental profile.");
                }
            }
            finally
            {
                config.SaveOnConfigSet = save;
            }

            config.Save();

            var shared = AssetBundleHelper.HexBattleMageWand.GetComponent<ItemDrop>().m_itemData.m_shared;

            _originalDamage = shared.m_damages;
            _originalPerQuality = shared.m_damagesPerLevel;
            _originalEffect = shared.m_attackStatusEffect;
            _originalEffectChance = shared.m_attackStatusEffectChance;
        }

        internal static WandElementSettings GetSettings(WandElement element)
        {
            return Settings[element];
        }

        internal static int GetHash(WandElement element)
        {
            return Effects[element].NameHash();
        }

        internal static bool TryGetElement(int hash, out WandElement element)
        {
            foreach (var pair in Effects)
            {
                if (pair.Value.NameHash() == hash)
                {
                    element = pair.Key;
                    return true;
                }
            }

            element = default(WandElement);
            return false;
        }

        internal static int GetElementMask(int hash)
        {
            if (TryGetElement(hash, out var element))
            {
                return 1 << (int)element;
            }

            foreach (var effect in GroupEffects.Values)
            {
                if (effect.NameHash() == hash)
                {
                    return effect.ElementMask;
                }
            }

            return 0;
        }

        internal static bool TryGetFlightColors(int mask, out Color core, out Color halo)
        {
            core = Color.clear;
            halo = Color.clear;
            int count = 0;

            foreach (var entry in Settings)
            {
                if ((mask & (1 << (int)entry.Key)) == 0)
                {
                    continue;
                }

                core += entry.Value.Profile.ProjectileCoreColor;
                halo += entry.Value.Profile.ProjectileHaloColor;
                count++;
            }

            if (count == 0)
            {
                return false;
            }

            core /= count;
            halo /= count;
            return true;
        }

        internal static bool HasElement(int hash, WandElement element)
        {
            if (GetHash(element) == hash)
            {
                return true;
            }

            foreach (var effect in GroupEffects.Values)
            {
                if (effect.NameHash() == hash && (effect.ElementMask & (1 << (int)element)) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsWandElementHash(int hash)
        {
            if (TryGetElement(hash, out _))
            {
                return true;
            }

            foreach (var effect in GroupEffects.Values)
            {
                if (effect.NameHash() == hash)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void ApplyToItem(ItemDrop.ItemData item)
        {
            if (Enabled == null)
            {
                return;
            }

            var shared = item.m_shared;
            float blunt = shared.m_damages.m_blunt;
            float pierce = shared.m_damages.m_pierce;
            float bluntPerQuality = shared.m_damagesPerLevel.m_blunt;
            float piercePerQuality = shared.m_damagesPerLevel.m_pierce;

            shared.m_damages = _originalDamage;
            shared.m_damages.m_blunt = blunt;
            shared.m_damages.m_pierce = pierce;
            shared.m_damagesPerLevel = _originalPerQuality;
            shared.m_damagesPerLevel.m_blunt = bluntPerQuality;
            shared.m_damagesPerLevel.m_pierce = piercePerQuality;
            shared.m_attackStatusEffect = _originalEffect;
            shared.m_attackStatusEffectChance = _originalEffectChance;

            ClearElements(ref shared.m_damages);
            ClearElements(ref shared.m_damagesPerLevel);
            shared.m_attackStatusEffect = null;
            shared.m_attackStatusEffectChance = 0f;

            if (!Enabled.EffectiveValue())
            {
                return;
            }

            var active = WandAttunements.GetCombatElements(item);
            int mask = 0;

            foreach (var element in active)
            {
                var settings = GetSettings(element);

                SetDamage(ref shared.m_damages, element, settings.DirectDamage.EffectiveValue());
                SetDamage(ref shared.m_damagesPerLevel, element, settings.DamagePerQuality.EffectiveValue());
                mask |= 1 << (int)element;
            }

            if (active.Count == 0)
            {
                return;
            }

            shared.m_attackStatusEffect = active.Count == 1 ? (StatusEffect)Effects[active[0]] : GroupEffects[mask];
            shared.m_attackStatusEffectChance = 1f;
        }

        internal static void SetDamage(ref HitData.DamageTypes damage, WandElement element, float amount)
        {
            switch (element)
            {
                case WandElement.Fire:
                    damage.m_fire = amount;
                    break;
                case WandElement.Frost:
                    damage.m_frost = amount;
                    break;
                case WandElement.Spirit:
                    damage.m_spirit = amount;
                    break;
                case WandElement.Lightning:
                    damage.m_lightning = amount;
                    break;
                case WandElement.Poison:
                    damage.m_poison = amount;
                    break;
            }
        }

        internal static HitData.DamageType GetDamageType(WandElement element)
        {
            switch (element)
            {
                case WandElement.Fire:
                    return HitData.DamageType.Fire;
                case WandElement.Frost:
                    return HitData.DamageType.Frost;
                case WandElement.Spirit:
                    return HitData.DamageType.Spirit;
                case WandElement.Lightning:
                    return HitData.DamageType.Lightning;
                default:
                    return HitData.DamageType.Poison;
            }
        }

        private static void ClearElements(ref HitData.DamageTypes damage)
        {
            damage.m_fire = 0f;
            damage.m_frost = 0f;
            damage.m_spirit = 0f;
            damage.m_lightning = 0f;
            damage.m_poison = 0f;
        }
    }
}

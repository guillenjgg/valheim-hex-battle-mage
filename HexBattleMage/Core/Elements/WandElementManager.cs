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
        private const string Section = "Development / Elements";
        private static readonly Dictionary<WandElement, WandElementSettings> Settings = new Dictionary<WandElement, WandElementSettings>();
        private static readonly Dictionary<WandElement, WandElementStatus> Effects = new Dictionary<WandElement, WandElementStatus>();
        private static HitData.DamageTypes _originalDamage;
        private static HitData.DamageTypes _originalPerQuality;
        private static StatusEffect _originalEffect;
        private static float _originalEffectChance;

        internal static ConfigEntry<bool> Enabled { get; private set; }
        internal static ConfigEntry<WandElement> ActiveElement { get; private set; }

        internal static void Initialize(ConfigFile config)
        {
            Configure(config);

            foreach (var effect in Effects.Values)
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
                Enabled = config.Bind(Section, nameof(Enabled), PrefabSettings.Wand.EnableElements, "Enable the custom wand elements. Disable to restore the original wand damage/effects. Active custom effects expire immediately when disabled.");
                ActiveElement = config.Bind(Section, nameof(ActiveElement), PrefabSettings.Wand.DefaultElement, "Testing selector: Fire, Frost, Spirit, Lightning or Poison. Applies on the next shot. Mage Table selection and tier unlocks are not implemented yet.");

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

                    var effect = ScriptableObject.CreateInstance<WandElementStatus>();

                    effect.name = "HexWandStatus" + element;
                    effect.Element = element;
                    effect.m_name = profile.StatusDisplayName;
                    effect.m_ttl = profile.Duration;

                    Effects[element] = effect;
                }

                if (configuredElements.Count != Enum.GetValues(typeof(WandElement)).Length)
                {
                    throw new InvalidOperationException("The wand must reference all five elemental profiles.");
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

        internal static void ApplyToItem(ItemDrop.ItemData item)
        {
            if (Enabled == null)
            {
                return;
            }

            var shared = item.m_shared;
            float blunt = shared.m_damages.m_blunt;
            float pierce = shared.m_damages.m_pierce;

            shared.m_damages = _originalDamage;
            shared.m_damages.m_blunt = blunt;
            shared.m_damages.m_pierce = pierce;
            shared.m_damagesPerLevel = _originalPerQuality;
            shared.m_attackStatusEffect = _originalEffect;
            shared.m_attackStatusEffectChance = _originalEffectChance;

            if (!Enabled.Value)
            {
                return;
            }

            var element = ActiveElement.Value;
            var settings = GetSettings(element);

            ClearElements(ref shared.m_damages);
            ClearElements(ref shared.m_damagesPerLevel);
            SetDamage(ref shared.m_damages, element, settings.DirectDamage.Value);
            SetDamage(ref shared.m_damagesPerLevel, element, settings.DamagePerQuality.Value);
            shared.m_attackStatusEffect = Effects[element];
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

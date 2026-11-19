using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Trinkets;
using Jotunn.Entities;
using UnityEngine;

public static class PlayerTextLocalizationValidation
{
    private const BindingFlags InternalStatic = BindingFlags.Static | BindingFlags.NonPublic;
    private const BindingFlags InternalInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>();
    private static readonly Assembly Mod = typeof(HexBattleMageTrinketProfile).Assembly;
    private static string _language = "English";
    private static int _checks;

    public static void InitializeForTests(Harmony harmony)
    {
        Translations.Clear();
        _language = "English";
        harmony.Patch(AccessTools.Method(typeof(CustomLocalization), "AddJsonFile"), prefix: new HarmonyMethod(typeof(PlayerTextLocalizationValidation), nameof(ReadTranslation)));
        harmony.Patch(AccessTools.Method(typeof(CustomLocalization), "TryTranslate"), prefix: new HarmonyMethod(typeof(PlayerTextLocalizationValidation), nameof(Translate)));
        harmony.Patch(AccessTools.Method(typeof(ItemDrop), "OnValidate"), prefix: new HarmonyMethod(typeof(PlayerTextLocalizationValidation), nameof(Skip)));

        // Isolate Jotunn's game bootstrap while exercising the mod's actual resource loader and formatting code.
        var localization = (CustomLocalization)FormatterServices.GetUninitializedObject(typeof(CustomLocalization));

        Mod.GetType("HexBattleMage.Core.ModLocalization").GetMethod("LoadTranslations", InternalStatic).Invoke(null, new object[] { localization });
    }

    public static void Run()
    {
        string bundlePath = Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE");

        if (string.IsNullOrWhiteSpace(bundlePath))
        {
            throw new InvalidOperationException("Set HEXBATTLEMAGE_TEST_BUNDLE to the production bundle path.");
        }

        var harmony = new Harmony("HexBattleMage.PlayerTextLocalizationValidation");
        var manager = Mod.GetType("HexBattleMage.Core.Managers.TrinketManager");
        string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "LocalizationValidation");

        Directory.CreateDirectory(directory);
        typeof(Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(directory, "core.cfg"));

        var config = new ConfigFile(Path.Combine(directory, "test.cfg"), false) { SaveOnConfigSet = false };
        AssetBundle bundle = null;

        try
        {
            InitializeForTests(harmony);
            Check(Translations.ContainsKey("English"), "The shipped English resource must load.");

            foreach (var entry in Translations["English"])
            {
                string.Format(CultureInfo.CurrentCulture, entry.Value, 1.5f, 2f, 3f);
                Check(!entry.Value.Contains("\uFFFD"), "Invalid text encoding: " + entry.Key);
            }

            bundle = AssetBundle.LoadFromFile(bundlePath);

            foreach (var prefab in bundle.LoadAllAssets<GameObject>())
            {
                Audit(prefab);
            }

            manager.GetMethod("Configure", InternalStatic).Invoke(null, new object[] { bundle.LoadAllAssets<GameObject>(), config });

            var profile = bundle.LoadAsset<GameObject>("HexBattleMage_Trinket_Meadows_01").GetComponent<HexBattleMageTrinketProfile>();
            var effects = Mod.GetType("HexBattleMage.Core.Trinkets.TrinketEffects");
            var tooltip = effects.GetMethod("Tooltip", InternalStatic);
            string english = (string)tooltip.Invoke(null, new object[] { profile });

            Check(english.Contains("Passive Effects") && english.Contains("Combat Reward") && english.Contains("Boss Effect"), "English headings must resolve.");
            Check(english.Contains("+25% wand damage for 2s") && english.Contains("+35% total for 2s"), "English tooltip must retain live reward values.");
            Check(!english.Contains("$"), "English tooltip must not expose raw tokens.");

            Translations["TestLanguage"] = new Dictionary<string, string>
            {
                { "hexbattlemage_passive_effects", "Effets passifs" },
                { "hexbattlemage_combat_reward", "Prime de combat" },
                { "hexbattlemage_boss_effect", "Effet de boss" },
                { "hexbattlemage_on_successful_parry", "Après une parade réussie :" },
                { "hexbattlemage_on_successful_boss_parry", "Après une parade de boss :" },
                { "hexbattlemage_parry_refresh", "Les parades renouvellent la durée." },
                { "hexbattlemage_trinket_maximum_eitr", "Eitr maximum : +{0:0.#}" },
                { "hexbattlemage_trinket_eitr_regeneration", "Régénération : +{0:0.#}%" },
                { "hexbattlemage_trinket_parry_damage", "Pendant {1:0.#} s : +{0:0.#}% dégâts de baguette" },
                { "hexbattlemage_trinket_boss_damage", "Pendant {2:0.#} s : total +{1:0.#}% (bonus +{0:0.#}%)" }
            };
            _language = "TestLanguage";

            var settings = manager.GetMethod("GetSettings", InternalStatic).Invoke(null, new object[] { profile });

            ((ConfigEntry<float>)settings.GetType().GetProperty("MaximumEitr", InternalInstance).GetValue(settings)).Value = 42f;
            ((ConfigEntry<float>)settings.GetType().GetProperty("Duration", InternalInstance).GetValue(settings)).Value = 1.5f;
            ((ConfigEntry<float>)settings.GetType().GetProperty("DamageBonus", InternalInstance).GetValue(settings)).Value = 0.4f;

            string translated = (string)tooltip.Invoke(null, new object[] { profile });

            Check(translated.Contains("Effets passifs") && translated.Contains("Prime de combat") && translated.Contains("Effet de boss"), "A language change must update all headings.");
            Check(translated.Contains("Eitr maximum : +42"), "Translated text must read current config values.");
            Check(translated.Contains(string.Format(CultureInfo.CurrentCulture, "Pendant {0:0.#} s : +40%", 1.5f)), "Translation must control placeholder order.");
            Check(translated.Contains("total +50% (bonus +10%)"), "Translated boss total must use current settings.");
            Check(!translated.Contains("wand damage") && !translated.Contains("Additional"), "Translated templates must not retain hardcoded English fragments.");
            Check(!translated.Contains("$"), "Translated text must not expose raw tokens.");

            var format = Mod.GetType("HexBattleMage.Core.ModLocalization").GetMethod("Format", InternalStatic);
            string fallback = (string)format.Invoke(null, new object[] { profile.DisplayName, new object[0] });

            Check(fallback == "Stone of Reprisal", "An untranslated item name must retain its English fallback.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "player-text-localization-report.txt"), $"PASS: {_checks} checks covering shipped translation resources, prefab tokens, formatting, live values, reordered translated placeholders and English fallback. Jotunn language lookup/bootstrap is isolated; no second language is shipped by this test.");
        }
        finally
        {
            manager.GetMethod("Configure", InternalStatic).Invoke(null, new object[] { new GameObject[0], config });
            harmony.UnpatchSelf();

            if (bundle != null)
            {
                bundle.Unload(true);
            }
        }
    }

    private static void Audit(GameObject prefab)
    {
        var profile = prefab.GetComponent<HexBattleMageTrinketProfile>();

        if (profile != null)
        {
            foreach (string field in new[] { "DisplayName", "Description", "PassiveHeading", "RewardHeading", "RewardCondition", "RewardDetails", "BossHeading", "BossCondition", "BuffDisplayName", "BuffDescription" })
            {
                Token((string)typeof(HexBattleMageTrinketProfile).GetField(field).GetValue(profile), prefab.name + "." + field);
            }
        }

        var piece = prefab.GetComponent<Piece>();

        if (piece != null)
        {
            Token(piece.m_name, prefab.name + ".m_name");
            Token(piece.m_description, prefab.name + ".m_description");
        }

        var element = prefab.GetComponent<WandElementProfile>();

        if (element != null)
        {
            Token(element.StatusDisplayName, prefab.name + ".StatusDisplayName");
        }

        var item = prefab.GetComponent<ItemDrop>();

        if (item != null)
        {
            Token(item.m_itemData.m_shared.m_name, prefab.name + ".itemName");
            Token(item.m_itemData.m_shared.m_description, prefab.name + ".itemDescription");
        }

        var bracer = prefab.GetComponent<MagicBracerProfile>();

        if (bracer != null)
        {
            Token(bracer.DisplayName, prefab.name + ".DisplayName");
            Token(bracer.Description, prefab.name + ".Description");
        }
    }

    private static void Token(string token, string field)
    {
        Check(!string.IsNullOrEmpty(token) && token.StartsWith("$", StringComparison.Ordinal), "Expected localization token on " + field);
        Check(Translations["English"].ContainsKey(token.Substring(1)), "Missing English translation on " + field + ": " + token);
    }

    private static bool ReadTranslation(string __0, string __1)
    {
        var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });

        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(__1)))
        {
            Translations[__0] = (Dictionary<string, string>)serializer.ReadObject(stream);
        }

        return false;
    }

    private static bool Translate(string __0, ref string __result)
    {
        if (string.IsNullOrEmpty(__0) || !__0.StartsWith("$", StringComparison.Ordinal))
        {
            __result = __0;
            return false;
        }

        string key = __0.Substring(1);

        if (Translations.TryGetValue(_language, out var selected) && selected.TryGetValue(key, out __result))
        {
            return false;
        }

        __result = Translations["English"].TryGetValue(key, out var english) ? english : "[" + __0 + "]";
        return false;
    }

    private static bool Skip()
    {
        return false;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }

        _checks++;
    }
}

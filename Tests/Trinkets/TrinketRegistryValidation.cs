using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Trinkets;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TrinketRegistryValidation
{
    private const BindingFlags InternalStatic = BindingFlags.Static | BindingFlags.NonPublic;
    private const BindingFlags InternalInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly Type Manager = typeof(HexBattleMageTrinketProfile).Assembly.GetType("HexBattleMage.Core.Managers.TrinketManager");

    public static void Run()
    {
        if (Manager == null)
        {
            throw new InvalidOperationException("Run this validation in the isolated project with the compiled mod DLL, not the authoring assembly.");
        }

        var fixtures = new List<GameObject>();
        string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "TrinketValidation");

        Directory.CreateDirectory(directory);
        typeof(BepInEx.Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(directory, "core.cfg"));

        var config = new ConfigFile(Path.Combine(directory, "trinkets.cfg"), false) { SaveOnConfigSet = false };

        try
        {
            var first = Create("HexTestTrinketA", 25f, TrinketRewardTrigger.SuccessfulParry, fixtures);
            var second = Create("HexTestTrinketB", 40f, TrinketRewardTrigger.SuccessfulParry, fixtures);
            var passive = Create("HexTestTrinketC", 10f, TrinketRewardTrigger.None, fixtures);
            var firstProfile = first.GetComponent<HexBattleMageTrinketProfile>();
            var secondProfile = second.GetComponent<HexBattleMageTrinketProfile>();
            var passiveProfile = passive.GetComponent<HexBattleMageTrinketProfile>();

            firstProfile.ConfigSection = "Equipment / Custom Test Trinket A";
            firstProfile.RewardStatusName = "HexTestTrinketAReward";
            secondProfile.WandDamageBonus = 0.5f;

            var overrideValue = config.Bind("Equipment / Trinkets / HexTestTrinketB", "ParryWandDamageBonus", 0.5f);

            overrideValue.Value = 0.9f;
            Configure(new[] { first, second, passive }, config);

            object firstSettings = Settings(firstProfile);
            object secondSettings = Settings(secondProfile);
            object passiveSettings = Settings(passiveProfile);

            Check((string)Property(firstSettings, "ConfigSection") == firstProfile.ConfigSection, "Explicit config section is respected.");
            Check((string)Property(firstSettings, "RewardStatusName") == firstProfile.RewardStatusName, "Explicit reward name is respected.");
            Check((int)Property(firstSettings, "RewardHash") != (int)Property(secondSettings, "RewardHash"), "Different trinkets require different reward hashes.");
            Check((int)Property(passiveSettings, "RewardHash") == 0, "Passive-only trinkets must not register a parry reward.");
            Check(Property(passiveSettings, "DamageBonus") == null, "Passive-only trinkets must not bind parry configuration.");
            Equal(Value(firstSettings, "MaximumEitr"), 25f, "First Eitr default");
            Equal(Value(secondSettings, "MaximumEitr"), 40f, "Second Eitr default");
            Equal(Value(passiveSettings, "MaximumEitr"), 10f, "Passive Eitr default");
            Equal(Value(secondSettings, "DamageBonus"), 0.9f, "Existing config overrides the second authored default");
            Equal(Value(firstSettings, "DamageBonus"), 0.25f, "Second config does not affect the first reward");

            ((ConfigEntry<float>)Property(firstSettings, "MaximumEitr")).Value = 31f;
            Equal(Value(secondSettings, "MaximumEitr"), 40f, "Editing the first trinket does not affect the second");
            Equal(Value(passiveSettings, "MaximumEitr"), 10f, "Editing the first trinket does not affect the passive trinket");

            var conflict = Create(firstProfile.ItemPrefabName, 5f, TrinketRewardTrigger.SuccessfulParry, fixtures);
            var conflictProfile = conflict.GetComponent<HexBattleMageTrinketProfile>();

            Reject(new[] { first, conflict }, config, "Duplicate item identity");
            conflictProfile.ItemPrefabName = "HexTestConflict";
            conflictProfile.ConfigSection = firstProfile.ConfigSection;
            Reject(new[] { first, conflict }, config, "Duplicate config section");
            conflictProfile.ConfigSection = string.Empty;
            conflictProfile.RewardStatusName = firstProfile.RewardStatusName;
            Reject(new[] { first, conflict }, config, "Duplicate reward identity");
            Check(ReferenceEquals(Settings(firstProfile), firstSettings), "Invalid definitions must not partially replace the registry.");

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "trinket-registry-report.txt"), "PASS: three trinket definitions, independent config/defaults/reward identities, config overrides, passive-only behavior and duplicate safeguards.");
        }
        finally
        {
            Configure(new GameObject[0], config);

            foreach (var fixture in fixtures)
            {
                Object.DestroyImmediate(fixture);
            }
        }
    }

    private static GameObject Create(string identity, float eitr, TrinketRewardTrigger trigger, List<GameObject> fixtures)
    {
        var root = new GameObject(identity);

        root.SetActive(false);
        fixtures.Add(root);

        var profile = root.AddComponent<HexBattleMageTrinketProfile>();
        var eitrProfile = root.AddComponent<MageEitrProfile>();
        var definition = root.AddComponent<MageEquipmentDefinition>();

        profile.ItemPrefabName = identity;
        profile.RewardTrigger = trigger;
        profile.WandDamageBonus = 0.25f;
        eitrProfile.MaximumEitr = eitr;
        definition.Category = MageEquipmentCategory.Trinket;
        definition.Slot = MageEquipmentSlot.Trinket;
        root.AddComponent<MageRecipeProfile>();
        return root;
    }

    private static void Configure(GameObject[] prefabs, ConfigFile config)
    {
        Manager.GetMethod("Configure", InternalStatic).Invoke(null, new object[] { prefabs, config });
    }

    private static object Settings(HexBattleMageTrinketProfile profile)
    {
        return Manager.GetMethod("GetSettings", InternalStatic).Invoke(null, new object[] { profile });
    }

    private static object Property(object settings, string name)
    {
        return settings.GetType().GetProperty(name, InternalInstance).GetValue(settings);
    }

    private static float Value(object settings, string name)
    {
        return ((ConfigEntry<float>)Property(settings, name)).Value;
    }

    private static void Reject(GameObject[] prefabs, ConfigFile config, string message)
    {
        try
        {
            Configure(prefabs, config);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException(message + " was accepted.");
    }

    private static void Equal(float actual, float expected, string message)
    {
        Check(Mathf.Abs(actual - expected) < 0.001f, message);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

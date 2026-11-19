using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Elements;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PrefabTuningValidation
{
    private const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Assembly Mod = typeof(MageEitrProfile).Assembly;

    public static void Run()
    {
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE"));
        var unrelated = new GameObject("Unrelated equipment");

        try
        {
            foreach (var source in bundle.LoadAllAssets<GameObject>())
            {
                var armorProfile = source.GetComponent<HexBattleMageArmorProfile>();

                if (armorProfile == null)
                {
                    continue;
                }

                var upgradeProfile = source.GetComponent<MageUpgradeProfile>();
                var sourceRecipe = source.GetComponent<MageRecipeProfile>();

                if (sourceRecipe.BakedRecipe == null)
                {
                    sourceRecipe.BakedRecipe = bundle.LoadAsset<TextAsset>("Recipe_" + source.name);
                }

                Check(upgradeProfile != null && upgradeProfile.FixedResourceCosts && !upgradeProfile.UseFixedStationLevel, "Fixed cost policy is baked into armor, with native station progression.");
                Check(sourceRecipe.GetResources().Length == 2 && sourceRecipe.Resources.All(resource => resource.ItemPrefab != "BoneFragments"), "Baked armor recipes exclude bones before code tuning.");
                Check(sourceRecipe.Resources.Single(resource => resource.ItemPrefab == "DeerHide").AmountPerQuality == 3, "Cheaper upgrade costs are baked.");
            }

            var wand = bundle.LoadAsset<GameObject>("HexBattleMage_Wand_Meadows_01");
            var profile = wand.GetComponent<HexBattleMageWandProfile>();
            float lightRange = profile.WandLightRange;

            profile.WandBluntDamage = 91f;
            profile.Elements.Single(element => element.Element == WandElement.Fire).DirectDamage = 92f;
            var armor = bundle.LoadAllAssets<GameObject>().Single(prefab => prefab.GetComponent<HexBattleMageArmorProfile>()?.ItemPrefabName == "HexBattleMage_Chest_Meadows_01");
            float armorValue = armor.GetComponent<HexBattleMageArmorProfile>().Armor;

            armor.GetComponent<HexBattleMageVitalityProfile>().MaximumHealth = 93f;
            var trinket = bundle.LoadAsset<GameObject>("HexBattleMage_Trinket_Meadows_01");
            var recipe = trinket.GetComponent<MageRecipeProfile>();

            recipe.BakedRecipe = null;
            recipe.Resources = new MageRecipeResource[0];
            Mod.GetType("HexBattleMage.Core.Tuning.PrefabTuning").GetMethod("Apply", Members).Invoke(null, new object[] { bundle });
            Check(profile.WandBluntDamage == 5f, "Specific wand overrides apply before configuration binds.");
            Check(profile.Elements.Single(element => element.Element == WandElement.Fire).DirectDamage == 15f, "Referenced elemental settings are tuned.");
            Check(profile.WandLightRange == lightRange, "Unspecified authored values are retained.");
            Check(armor.GetComponent<HexBattleMageVitalityProfile>().MaximumHealth == 2f, "Armor dispatch uses runtime item identity.");
            Check(armor.GetComponent<HexBattleMageArmorProfile>().Armor == armorValue, "Untuned armor stats remain authored.");
            Check(recipe.BakedRecipe != null && recipe.GetResources().Length == 4, "Recipe hydration precedes code changes.");
            recipe.ApplyBakedDefaults();
            Check(recipe.Resources[0].ItemPrefab == "TrophyBoar" && recipe.Resources[0].Amount == 1, "Later recipe reads retain tuning.");
            var otherArmor = unrelated.AddComponent<HexBattleMageArmorProfile>();

            otherArmor.ItemPrefabName = "OtherMod_Chest";
            otherArmor.Armor = 77f;
            Mod.GetType("HexBattleMage.Core.Tuning.ArmorTuning").GetMethod("Apply", Members).Invoke(null, new object[] { unrelated });
            Check(otherArmor.Armor == 77f && unrelated.GetComponent<HexBattleMageVitalityProfile>() == null, "Unmatched prefabs are untouched.");
            ValidateConfiguration(bundle);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "prefab-tuning-report.txt"), "PASS: typed per-item tuning, referenced element tuning, untouched authored fields, unknown item isolation, recipe hydration, config/default precedence, live mode switching and saved-value preservation.");
        }
        finally
        {
            Object.DestroyImmediate(unrelated);
            bundle.Unload(true);
        }
    }

    private static void ValidateConfiguration(AssetBundle bundle)
    {
        string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "PrefabTuning");

        Directory.CreateDirectory(directory);
        typeof(Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(directory, "core.cfg"));
        var config = new ConfigFile(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".cfg"), false) { SaveOnConfigSet = false };
        var settings = Mod.GetType("HexBattleMage.Core.Tuning.TuningConfiguration");

        settings.GetMethod("Initialize", Members).Invoke(null, new object[] { config });
        var mode = config.Bind("Tuning", "UseConfigOverrides", true, "");
        var damage = config.Bind("Test", "Damage", 5f, "");
        var effective = settings.GetMethod("EffectiveValue", Members).MakeGenericMethod(typeof(float));

        damage.Value = 12f;
        Check((float)effective.Invoke(null, new object[] { damage }) == 12f, "Existing saved tuning remains enabled by default.");
        var crafting = Mod.GetType("HexBattleMage.Core.Tuning.CraftingTuning");
        var rune = bundle.LoadAsset<GameObject>("HexMageRuneStone").GetComponent<MageRecipeProfile>();
        var stand = bundle.LoadAsset<GameObject>("HexMageReagentStand").GetComponent<MageRecipeProfile>();

        Check(rune.GetResources().Length == 2 && rune.Resources.Single(resource => resource.ItemPrefab == "Flint").Amount == 10, "Rune Stone uses chopping block costs.");
        Check(stand.GetResources().Length == 4 && stand.Resources.Single(resource => resource.ItemPrefab == "LeatherScraps").Amount == 20, "Reagent Stand uses tanning rack costs.");
        config.Bind("Crafting / Mage Table / HexMageRuneStone", "Wood", 10, "").Value = 7;
        crafting.GetMethod("Configure", Members).Invoke(null, new object[] { config, bundle });
        Check(rune.Resources.Single(resource => resource.ItemPrefab == "Wood").Amount == 7, "Table material costs respect saved configuration.");
        mode.Value = false;
        Mod.GetType("HexBattleMage.Core.Tuning.PrefabTuning").GetMethod("Apply", Members).Invoke(null, new object[] { bundle });
        crafting.GetMethod("Configure", Members).Invoke(null, new object[] { config, bundle });
        Check(rune.Resources.Single(resource => resource.ItemPrefab == "Wood").Amount == 10, "Code mode keeps table tuning ahead of saved counts.");
        Check((float)effective.Invoke(null, new object[] { damage }) == 5f, "Code mode uses the tuned binding default.");
        Check(damage.Value == 12f, "Code mode does not erase saved values.");
        mode.Value = true;
        damage.Value = 18f;
        Check((float)effective.Invoke(null, new object[] { damage }) == 18f, "Live config mode reads current values.");

        var helper = Mod.GetType("HexBattleMage.Assets.AssetBundleHelper");
        var wand = bundle.LoadAsset<GameObject>("HexBattleMage_Wand_Meadows_01");

        helper.GetProperty("Bundle", Members).SetValue(null, bundle);
        helper.GetProperty("HexBattleMageWand", Members).SetValue(null, wand);
        helper.GetProperty("HexMagicBolt", Members).SetValue(null, bundle.LoadAsset<GameObject>("HexMagicBolt"));
        config.Bind("Development / Wand Damage", "WandBluntDamage", 5f, "").Value = 45f;
        config.Bind("Development / Elements / Fire", "DirectDamage", 15f, "").Value = 65f;
        var devType = Mod.GetType("HexBattleMage.Core.DevConfig");
        var dev = (IDisposable)Activator.CreateInstance(devType, Members, null, new object[] { config }, null);

        try
        {
            Mod.GetType("HexBattleMage.Core.Elements.WandElementManager").GetMethod("Configure", Members).Invoke(null, new object[] { config });
            var apply = Mod.GetType("HexBattleMage.Core.Managers.ItemRegistrationManager").GetMethod("ApplyDamageSettings", Members);
            var item = wand.GetComponent<ItemDrop>().m_itemData;

            item.m_quality = 1;
            mode.Value = false;
            apply.Invoke(null, new object[] { item });
            Check(item.GetDamage().m_blunt == 5f && item.GetDamage().m_fire == 15f, "Native item damage uses tuned profiles in code mode despite saved overrides.");
            mode.Value = true;
            apply.Invoke(null, new object[] { item });
            Check(item.GetDamage().m_blunt == 45f && item.GetDamage().m_fire == 65f, "Native item damage uses saved settings when explicitly enabled.");
        }
        finally
        {
            dev.Dispose();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

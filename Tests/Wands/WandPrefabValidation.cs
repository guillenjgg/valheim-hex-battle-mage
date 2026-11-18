using System;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using HexBattleMage.Core.Managers;
using UnityEngine;

public static class WandPrefabValidation
{
    private const string WandIdentity = "HexBattleMage_Wand_Meadows_01";
    private const BindingFlags InternalStatic = BindingFlags.Static | BindingFlags.NonPublic;

    public static void Run()
    {
        string bundlePath = Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE");

        if (string.IsNullOrEmpty(bundlePath))
        {
            throw new InvalidOperationException("Set HEXBATTLEMAGE_TEST_BUNDLE to the production bundle.");
        }

        var bundle = AssetBundle.LoadFromFile(bundlePath);
        var assembly = typeof(HexBattleMageWandProfile).Assembly;
        var assets = assembly.GetType("HexBattleMage.Assets.AssetBundleHelper", true);
        var configType = assembly.GetType("HexBattleMage.Core.DevConfig", true);
        string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "WandPrefabValidation");

        Directory.CreateDirectory(directory);
        typeof(BepInEx.Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(directory, "core.cfg"));

        var config = new ConfigFile(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".cfg"), false) { SaveOnConfigSet = false };
        IDisposable settings = null;

        try
        {
            var wand = bundle.LoadAsset<GameObject>(WandIdentity);

            Check(wand != null, "Progression-named wand is bundled.");
            Check(wand.name == WandIdentity, "Asset and registered identity agree.");
            Check(bundle.LoadAsset<GameObject>("HexBeechWand") == null, "No old development item alias.");
            Check(bundle.LoadAsset<GameObject>("HexBattleMageWand") == null, "No old authoring prefab alias.");

            var profile = wand.GetComponent<HexBattleMageWandProfile>();
            var shared = wand.GetComponent<ItemDrop>().m_itemData.m_shared;
            var equipment = wand.GetComponent<MageEquipmentDefinition>();
            var recipe = wand.GetComponent<MageRecipeProfile>();

            Check(profile != null, "Shared mod-prefixed profile is attached.");
            Check(equipment.Category == MageEquipmentCategory.Wand && equipment.Slot == MageEquipmentSlot.MainHand, "Wand equipment role is authored.");
            Check(shared.m_name.StartsWith("$", StringComparison.Ordinal), "Item name is localized.");
            Check(shared.m_description.StartsWith("$", StringComparison.Ordinal), "Item description is localized.");
            Check(profile.Elements.Length == 5, "All five elemental profiles are inspectable references.");
            Check(profile.StyleMaterials.Length == 3, "Approved styles are retained.");
            Check(shared.m_icons.Length == shared.m_variants, "Every authored style has an inventory icon slot.");
            Check(profile.ProjectilePrefab != null, "Projectile defaults are referenced.");
            Equal(shared.m_damages.m_blunt, profile.WandBluntDamage, "Native blunt projection");
            Equal(shared.m_damages.m_pierce, profile.WandPierceDamage, "Native pierce projection");
            Equal(shared.m_attack.m_projectileVel, profile.ProjectileVelocity, "Native velocity projection");
            Check(shared.m_attack.m_attackAnimation == profile.AttackAnimation, "Native animation projection agrees.");
            Check(shared.m_attack.m_attackProjectile == profile.ProjectilePrefab, "Native projectile reference agrees.");

            foreach (var element in profile.Elements)
            {
                Check(element != null, "Element reference resolves.");
                Check(element.StatusDisplayName.StartsWith("$", StringComparison.Ordinal), "Element status is localized.");
            }

            string authoredProfile = JsonUtility.ToJson(profile);
            string authoredItem = JsonUtility.ToJson(wand.GetComponent<ItemDrop>());

            assets.GetField("<HexBattleMageWand>k__BackingField", InternalStatic).SetValue(null, wand);
            assets.GetField("<HexMagicBolt>k__BackingField", InternalStatic).SetValue(null, profile.ProjectilePrefab);
            assets.GetField("<Bundle>k__BackingField", InternalStatic).SetValue(null, bundle);

            var savedDamage = config.Bind("Development / Wand Damage", "WandBluntDamage", profile.WandBluntDamage);

            savedDamage.Value = profile.WandBluntDamage + 7f;
            settings = (IDisposable)Activator.CreateInstance(configType, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { config }, null);

            var damage = Entry<float>(configType, "WandBluntDamage");
            var velocity = Entry<float>(configType, "ProjectileVelocity");

            Equal((float)damage.DefaultValue, profile.WandBluntDamage, "Damage config baseline comes from prefab");
            Equal(damage.Value, profile.WandBluntDamage + 7f, "Saved override takes precedence");
            Equal(velocity.Value, profile.ProjectileVelocity, "Untuned velocity starts from prefab");
            velocity.Value = profile.ProjectileVelocity + 11f;
            damage.Value = profile.WandBluntDamage + 9f;

            var registration = assembly.GetType("HexBattleMage.Core.Managers.ItemRegistrationManager", true);
            var item = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData() };
            var attack = new Attack();

            registration.GetMethod("ApplyDamageSettings", InternalStatic).Invoke(null, new object[] { item });
            registration.GetMethod("ApplySettings", InternalStatic).Invoke(null, new object[] { attack });
            Equal(item.m_shared.m_damages.m_blunt, damage.Value, "Code tuning reaches runtime damage");
            Equal(attack.m_projectileVel, velocity.Value, "Code tuning reaches runtime attack");
            Check(JsonUtility.ToJson(profile) == authoredProfile, "Runtime tuning leaves authored profile unchanged.");
            Check(JsonUtility.ToJson(wand.GetComponent<ItemDrop>()) == authoredItem, "Applying settings to runtime data leaves native baseline unchanged.");

            var expectedResources = recipe.GetResources();

            Check(expectedResources.Length > 0, "Recipe resources are baked.");
            recipe.Resources = new MageRecipeResource[0];
            recipe.BakedRecipe = null;
            typeof(MageRecipeProfile).GetField("_defaultsApplied", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(recipe, false);

            var defaults = assembly.GetType("HexBattleMage.Core.Managers.PrefabSettings", true);
            var requirements = (Array)defaults.GetMethod("GetRequirements", InternalStatic).Invoke(null, new object[] { recipe });

            Check(requirements.Length == expectedResources.Length, "Cold recipe recovery uses the canonical prefab identity.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "wand-prefab-report.txt"), "PASS: production wand identity, profile/native projections, references, localization, prefab-derived config defaults, saved and code override precedence, unchanged authored data and cold recipe recovery.");
        }
        finally
        {
            settings?.Dispose();
            bundle.Unload(true);
        }
    }

    private static ConfigEntry<T> Entry<T>(Type type, string name)
    {
        return (ConfigEntry<T>)type.GetProperty(name, InternalStatic).GetValue(null);
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BracerArmorValidation
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly BepInPlugin Metadata = new BepInPlugin("test.hexbattlemage", "HexBattleMage Registration Test", "1.0.0");
    private static GameObject _basePrefab;
    private static GameObject _visual;

    public static void Run()
    {
        var harmony = new Harmony("HexBattleMage.BracerArmorValidation");
        var container = new GameObject("RegistrationTestContainer");

        container.SetActive(false);
        Patch(harmony, typeof(ItemDrop), "Awake", "Skip");
        Patch(harmony, typeof(ItemDrop), "OnValidate", "Skip");
        harmony.Patch(AccessTools.Method(typeof(AssetManager), "AddAsset", new[] { typeof(Object), typeof(Object) }), prefix: new HarmonyMethod(typeof(BracerArmorValidation), "Skip"));

        foreach (var method in typeof(Jotunn.Utils.BepInExUtils).GetMethods(Fields))
        {
            if (method.Name == "GetSourceModMetadata")
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(BracerArmorValidation), "SourceMod"));
            }
        }

        var prefabs = (PrefabManager)FormatterServices.GetUninitializedObject(typeof(PrefabManager));
        var items = (ItemManager)FormatterServices.GetUninitializedObject(typeof(ItemManager));
        var assets = (AssetManager)FormatterServices.GetUninitializedObject(typeof(AssetManager));

        Set(typeof(PrefabManager), null, "_instance", prefabs);
        Set(typeof(PrefabManager), prefabs, "<PrefabContainer>k__BackingField", container);
        Set(typeof(PrefabManager), prefabs, "<Prefabs>k__BackingField", new Dictionary<string, CustomPrefab>());
        Set(typeof(ItemManager), null, "_instance", items);
        Set(typeof(ItemManager), items, "Items", new Dictionary<string, CustomItem>());
        Set(typeof(ItemManager), items, "Recipes", new HashSet<CustomRecipe>());
        Set(typeof(ItemManager), items, "StatusEffects", new HashSet<CustomStatusEffect>());
        Set(typeof(AssetManager), null, "instance", assets);
        Patch(harmony, typeof(PrefabManager), "GetPrefab", "LookupBase");
        var mockType = typeof(PrefabManager).Assembly.GetType("Jotunn.Managers.MockManager", true);
        var mocks = FormatterServices.GetUninitializedObject(mockType);

        Set(mockType, null, "_instance", mocks);
        Set(mockType, mocks, "MockPrefabContainer", container);
        Set(mockType, mocks, "mockedPrefabs", new Dictionary<string, GameObject>());

        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE"));
        var mod = typeof(MagicBracerProfile).Assembly;
        var manager = mod.GetType("HexBattleMage.Core.Managers.MagicShieldManager");
        string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "TrinketRegistration");

        Directory.CreateDirectory(directory);
        typeof(Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(directory, "core.cfg"));

        var config = new ConfigFile(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".cfg"), false) { SaveOnConfigSet = false };

        try
        {
            _visual = bundle.LoadAsset<GameObject>("HexMagicBracerVisual");
            var profile = _visual.GetComponent<MagicBracerProfile>();

            _basePrefab = new GameObject(profile.BaseShieldPrefab);
            _basePrefab.SetActive(false);
            _basePrefab.layer = 1;
            var baseDrop = _basePrefab.AddComponent<ItemDrop>();

            baseDrop.m_itemData = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData() };
            var helper = mod.GetType("HexBattleMage.Assets.AssetBundleHelper");

            helper.GetField("<Bundle>k__BackingField", Fields).SetValue(null, bundle);
            Check(profile.Armor == 5f && profile.MaximumQuality == 3, "Five armor and three qualities are baked.");
            profile.Armor = 99f;
            mod.GetType("HexBattleMage.Core.Tuning.PrefabTuning").GetMethod("Apply", Fields).Invoke(null, new object[] { bundle });
            Check(profile.Armor == 5f, "Source tuning restores the bracer armor setting.");
            manager.GetMethod("RegisterShield", Fields).Invoke(null, null);
            var registered = (Dictionary<string, CustomItem>)typeof(ItemManager).GetField("Items", Fields).GetValue(items);
            var item = registered["HexMagicBracer"].ItemDrop.m_itemData;
            var shared = item.m_shared;

            Check(shared.m_armor == 5f && shared.m_armorPerLevel == 0f, "Registration retains five armor without quality scaling.");
            Check(shared.m_maxQuality == 3 && shared.m_timedBlockBonus == 1.5f, "Native shield qualities and parry multiplier survive registration.");
            harmony.CreateClassProcessor(mod.GetType("HexBattleMage.Patches.MagicBracerArmorPatch")).Patch();
            harmony.CreateClassProcessor(mod.GetType("HexBattleMage.Patches.MagicBracerTooltipPatch")).Patch();
            var playerObject = new GameObject("Armor fixture");

            playerObject.SetActive(false);
            playerObject.transform.SetParent(container.transform);
            Patch(harmony, typeof(Game), "Awake", "Skip");
            Patch(harmony, typeof(Game), "OnDestroy", "Skip");
            var game = playerObject.AddComponent<Game>();

            AccessTools.Field(typeof(Game), "<instance>k__BackingField").SetValue(null, game);
            var player = playerObject.AddComponent<TrinketValidationPlayer>();

            AccessTools.Field(typeof(Character), "m_seman").SetValue(player, new SEMan(player, playerObject.AddComponent<ZNetView>()));
            AccessTools.Field(typeof(Player), "m_localPlayer").SetValue(null, player);
            Patch(harmony, typeof(Character), "GetSkillLevel", "ZeroSkill");
            Patch(harmony, typeof(Player), "AppendEquipmentModifierTooltips", "Skip");
            PlayerTextLocalizationValidation.InitializeForTests(harmony);
            player.GetInventory().GetAllItems().Add(item);
            Check(player.GetBodyArmor() == 0f, "Carrying a bracer provides no armor.");
            item.m_equipped = true;

            for (int quality = 1; quality <= 3; quality++)
            {
                item.m_quality = quality;
                Check(item.GetArmor() == 5f && player.GetBodyArmor() == 5f, "Each shield upgrade keeps the passive armor fixed at five, without blocking.");
                Check(shared.m_blockPower + shared.m_blockPowerPerLevel * (quality - 1) == 6f * quality, "Block power follows wood shield 6/12/18.");
                Check(shared.m_deflectionForce + shared.m_deflectionForcePerLevel * (quality - 1) == 15f + quality * 5f, "Block force follows wood shield 20/25/30.");
                Check(item.GetMaxDurability() == 150f + quality * 50f, "Durability follows wood shield 200/250/300.");
                string tooltip = ItemDrop.ItemData.GetTooltip(item, quality, true, 0f, 0, false);

                Check(tooltip.Contains("$item_armor: <color=orange>5</color>") || tooltip.Contains("Armour: <color=orange>5</color>") || tooltip.Contains("Armor: <color=orange>5</color>"), "Bracer tooltip exposes fixed armor.");
            }

            item.m_equipped = false;
            Check(player.GetBodyArmor() == 0f, "Unequipping removes the armor.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "bracer-armor-report.txt"), "PASS: baked/source armor settings, actual mod/Jotunn registration, three wood shield qualities, fixed five armor through native Player.GetBodyArmor, tooltip, inventory and unequip checks. Native ShieldWood base and manager/network bootstrap use isolated fixtures.");
        }
        finally
        {
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(_basePrefab);
            bundle.Unload(true);
            harmony.UnpatchSelf();
        }
    }

    private static bool LookupBase(string name, ref GameObject __result)
    {
        if (_basePrefab != null && name == _basePrefab.name)
        {
            __result = _basePrefab;
            return false;
        }

        return true;
    }

    private static bool SourceMod(ref BepInPlugin __result)
    {
        __result = Metadata;
        return false;
    }

    private static bool ZeroSkill(ref float __result)
    {
        __result = 0f;
        return false;
    }

    private static bool Skip()
    {
        return false;
    }

    private static void Patch(Harmony harmony, Type type, string method, string prefix)
    {
        harmony.Patch(AccessTools.Method(type, method), prefix: new HarmonyMethod(typeof(BracerArmorValidation), prefix));
    }

    private static void Set(Type type, object target, string field, object value)
    {
        type.GetField(field, Fields).SetValue(target, value);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}


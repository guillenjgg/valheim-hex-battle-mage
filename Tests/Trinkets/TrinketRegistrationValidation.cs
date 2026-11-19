using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HexBattleMage.Core.Trinkets;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TrinketRegistrationValidation
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly BepInPlugin Metadata = new BepInPlugin("test.hexbattlemage", "HexBattleMage Registration Test", "1.0.0");
    private static GameObject _basePrefab;
    private static GameObject _visual;

    public static void Run()
    {
        var harmony = new Harmony("HexBattleMage.TrinketRegistrationValidation");
        var container = new GameObject("RegistrationTestContainer");

        container.SetActive(false);
        Patch(harmony, typeof(ItemDrop), "Awake", "Skip");
        Patch(harmony, typeof(ItemDrop), "OnValidate", "Skip");
        harmony.Patch(AccessTools.Method(typeof(AssetManager), "AddAsset", new[] { typeof(Object), typeof(Object) }), prefix: new HarmonyMethod(typeof(TrinketRegistrationValidation), "Skip"));

        foreach (var method in typeof(Jotunn.Utils.BepInExUtils).GetMethods(Fields))
        {
            if (method.Name == "GetSourceModMetadata")
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(TrinketRegistrationValidation), "SourceMod"));
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
        var mod = typeof(HexBattleMageTrinketProfile).Assembly;
        var manager = mod.GetType("HexBattleMage.Core.Managers.TrinketManager");
        string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "TrinketRegistration");

        Directory.CreateDirectory(directory);
        typeof(Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(directory, "core.cfg"));

        var config = new ConfigFile(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".cfg"), false) { SaveOnConfigSet = false };

        try
        {
            _visual = bundle.LoadAsset<GameObject>("HexBattleMage_Trinket_Meadows_01");
            var profile = _visual.GetComponent<HexBattleMageTrinketProfile>();

            _basePrefab = new GameObject(profile.BaseItemPrefab);
            _basePrefab.SetActive(false);
            _basePrefab.layer = 1;
            var baseDrop = _basePrefab.AddComponent<ItemDrop>();

            baseDrop.m_itemData = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData() };
            Check(prefabs.GetPrefab(profile.ItemPrefabName) == _visual, "Loaded visual occupies the authored name before registration.");
            manager.GetMethod("Configure", Fields).Invoke(null, new object[] { new[] { _visual }, config });
            manager.GetMethod("RegisterAll", Fields).Invoke(null, null);

            var registered = (Dictionary<string, CustomItem>)typeof(ItemManager).GetField("Items", Fields).GetValue(items);

            Check(registered.ContainsKey(profile.ItemPrefabName), "Actual registration adds the trinket despite the loaded visual name.");
            var item = registered[profile.ItemPrefabName];

            Check(item.ItemPrefab != _visual, "Gameplay item is a separate native-base clone.");
            Check(item.ItemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trinket, "Native trinket item type is applied.");
            Check(item.Recipe.Recipe.m_resources.Length == 3, "Mage Table recipe includes all authored costs.");
            Check(item.Recipe.Recipe.m_item.name.EndsWith(profile.ItemPrefabName, StringComparison.Ordinal), "Recipe targets the gameplay trinket identity for reference resolution.");
            Check(item.Recipe.Recipe.m_craftingStation.name.EndsWith("HexArcaneWorkbench", StringComparison.Ordinal), "Recipe targets the Mage Table.");
            Check(prefabs.GetPrefab(profile.ItemPrefabName) == item.ItemPrefab, "Registered gameplay item supersedes the visual lookup.");
            Check(item.ItemPrefab.transform.Find("attach_Neck") != null, "Worn attachment is constructed.");
            Check(item.ItemDrop.m_itemData.m_shared.m_icons[0] == profile.Icon, "Authored icon is retained.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "trinket-registration-report.txt"), "PASS: loaded visual name collision reproduced; actual mod RegisterAll, Jotunn ClonePrefab/AddItem/AddRecipe/AddPrefab complete and produce native trinket, authored three-resource recipe, icon and worn attachment. Manager bootstrap and soft-asset storage isolated; native base is a fixture and deferred mock reference resolution is not run.");
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

    private static bool Skip()
    {
        return false;
    }

    private static void Patch(Harmony harmony, Type type, string method, string prefix)
    {
        harmony.Patch(AccessTools.Method(type, method), prefix: new HarmonyMethod(typeof(TrinketRegistrationValidation), prefix));
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


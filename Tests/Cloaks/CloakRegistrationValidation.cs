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

public static class CloakRegistrationValidation
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly BepInPlugin Metadata = new BepInPlugin("test.hexbattlemage", "HexBattleMage Registration Test", "1.0.0");
    private static GameObject _basePrefab;
    private static GameObject _visual;

    public static void Run()
    {
        var harmony = new Harmony("HexBattleMage.CloakRegistrationValidation");
        var container = new GameObject("RegistrationTestContainer");

        container.SetActive(false);
        Patch(harmony, typeof(ItemDrop), "Awake", "Skip");
        Patch(harmony, typeof(ItemDrop), "OnValidate", "Skip");
        harmony.Patch(AccessTools.Method(typeof(AssetManager), "AddAsset", new[] { typeof(Object), typeof(Object) }), prefix: new HarmonyMethod(typeof(CloakRegistrationValidation), "Skip"));

        foreach (var method in typeof(Jotunn.Utils.BepInExUtils).GetMethods(Fields))
        {
            if (method.Name == "GetSourceModMetadata")
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(CloakRegistrationValidation), "SourceMod"));
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
        var mod = typeof(HexBattleMageCloakProfile).Assembly;
        var manager = mod.GetType("HexBattleMage.Core.Managers.CloakManager");
        string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "TrinketRegistration");

        Directory.CreateDirectory(directory);
        typeof(Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(directory, "core.cfg"));

        var config = new ConfigFile(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".cfg"), false) { SaveOnConfigSet = false };

        try
        {
            _visual = bundle.LoadAsset<GameObject>("HexBattleMage_Cloak_Meadows_01");
            var profile = _visual.GetComponent<HexBattleMageCloakProfile>();

            _basePrefab = new GameObject(profile.BaseItemPrefab);
            _basePrefab.SetActive(false);
            _basePrefab.layer = 1;
            var baseDrop = _basePrefab.AddComponent<ItemDrop>();

            baseDrop.m_itemData = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData() };
            Check(prefabs.GetPrefab(profile.ItemPrefabName) == _visual, "Loaded visual occupies the authored name before registration.");
            mod.GetType("HexBattleMage.Assets.AssetBundleHelper").GetProperty("Bundle", Fields).SetValue(null, bundle);
            manager.GetMethod("Register", Fields).Invoke(null, null);

            var registered = (Dictionary<string, CustomItem>)typeof(ItemManager).GetField("Items", Fields).GetValue(items);

            Check(registered.ContainsKey(profile.ItemPrefabName), "Actual registration adds the trinket despite the loaded visual name.");
            var item = registered[profile.ItemPrefabName];

            Check(item.ItemPrefab != _visual, "Gameplay item is a separate native-base clone.");
            Check(item.ItemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder, "Native trinket item type is applied.");
            Check(item.Recipe.Recipe.m_resources.Length == 2, "Mage Table recipe includes all authored costs.");
            Check(item.Recipe.Recipe.m_item.name.EndsWith(profile.ItemPrefabName, StringComparison.Ordinal), "Recipe targets the gameplay trinket identity for reference resolution.");
            Check(item.Recipe.Recipe.m_craftingStation.name.EndsWith("HexArcaneWorkbench", StringComparison.Ordinal), "Recipe targets the Mage Table.");
            Check(prefabs.GetPrefab(profile.ItemPrefabName) == item.ItemPrefab, "Registered gameplay item supersedes the visual lookup.");
            ValidateBodyFits(manager, item, harmony);
            Check(item.ItemDrop.m_itemData.m_shared.m_icons[0] == profile.Icon, "Authored icon is retained.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "cloak-registration-report.txt"), "PASS: actual cloak registration and Jotunn AddItem/AddRecipe; authored icon and two-resource Mage Table recipe; native cape category; male/female lookup, vanilla pass-through and native AttachArmor transpiler verified. Native base/manager bootstrap are fixtures. Full game cloth/movement remains an in-game check.");
        }
        finally
        {
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(_basePrefab);
            bundle.Unload(true);
            harmony.UnpatchSelf();
        }
    }

    private static void ValidateBodyFits(Type manager, CustomItem item, Harmony harmony)
    {
        var authored = item.ItemPrefab.GetComponent<HexBattleMageCloakProfile>();
        var host = new GameObject("Body selection test");

        host.SetActive(false);
        Patch(harmony, typeof(VisEquipment), "Awake", "Skip");
        var equipment = host.AddComponent<VisEquipment>();
        var resolve = manager.GetMethod("ResolveArmorPrefab", Fields);
        var patch = manager.Assembly.GetType("HexBattleMage.Patches.CloakAttachmentPatch");

        harmony.Patch(AccessTools.Method(typeof(VisEquipment), "AttachArmor", new[] { typeof(int), typeof(int), typeof(int) }), transpiler: new HarmonyMethod(patch.GetMethod("Transpiler", Fields)));
        Set(typeof(VisEquipment), equipment, "m_currentModelIndex", 0);
        Check(resolve.Invoke(null, new object[] { item.ItemPrefab, equipment }) == authored.MaleVisual, "Male selects male attachment.");
        Set(typeof(VisEquipment), equipment, "m_currentModelIndex", 1);
        Check(resolve.Invoke(null, new object[] { item.ItemPrefab, equipment }) == authored.FemaleVisual, "Female selects female attachment.");
        Check(resolve.Invoke(null, new object[] { _basePrefab, equipment }) == _basePrefab, "Vanilla armor passes through unchanged.");
        Check(authored.MaleVisual.transform.Find("attach_skin") != null, "Male fit has native skin attachment.");
        Check(authored.FemaleVisual.transform.Find("attach_skin") != null, "Female fit has native skin attachment.");
        Check(item.ItemPrefab.transform.Find("model").GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0, "World drop uses baked meshes.");
        Object.DestroyImmediate(host);
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
        harmony.Patch(AccessTools.Method(type, method), prefix: new HarmonyMethod(typeof(CloakRegistrationValidation), prefix));
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

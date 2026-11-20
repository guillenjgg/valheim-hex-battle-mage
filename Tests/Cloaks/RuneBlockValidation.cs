using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using BepInEx;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

public static class RuneBlockValidation
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Assembly Mod = typeof(HexBattleMageRuneBlockProfile).Assembly;
    private static readonly BepInPlugin Metadata = new BepInPlugin("test.hexbattlemage.runeblock", "Rune Block Validation", "1.0.0");

    private static GameObject _fragmentRoot;
    private static bool _visibleFragmentsOnly;

    public static void Run()
    {
        var harmony = new Harmony("HexBattleMage.RuneBlockValidation");
        var previousZone = ZoneSystem.instance;
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE"));
        var container = new GameObject("Rune Block Fixture");

        container.SetActive(false);

        try
        {
            foreach (var type in new[] { typeof(CraftingStation), typeof(Piece), typeof(ZNetView), typeof(ZoneSystem), typeof(ItemDrop) })
            {
                foreach (string name in new[] { "Awake", "OnDestroy", "OnValidate", "Reset" })
                {
                    var method = AccessTools.DeclaredMethod(type, name);

                    if (method != null)
                    {
                        harmony.Patch(method, prefix: new HarmonyMethod(typeof(RuneBlockValidation), nameof(Skip)));
                    }
                }
            }

            string configDirectory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "RuneBlock");

            Directory.CreateDirectory(configDirectory);
            typeof(Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(configDirectory, "core.cfg"));

            foreach (var method in typeof(Jotunn.Utils.BepInExUtils).GetMethods(Members).Where(method => method.Name == "GetSourceModMetadata"))
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(RuneBlockValidation), nameof(SourceMod)));
            }

            PlayerTextLocalizationValidation.InitializeForTests(harmony);
            harmony.CreateClassProcessor(Mod.GetType("HexBattleMage.Patches.RuneBlockLevelPatch")).Patch();
            var zone = container.AddComponent<ZoneSystem>();
            var keys = new Dictionary<string, string>();

            Set(typeof(ZoneSystem), null, "s_instance", zone);
            Set(typeof(ZoneSystem), zone, "m_globalKeysValues", keys);
            var source = bundle.LoadAsset<GameObject>("HexBattleMage_RuneBlock");
            var profile = source.GetComponent<HexBattleMageRuneBlockProfile>();

            Check(profile != null && profile.BossDefeatKeys.Length == 7 && profile.StageVisualPaths.Length == 7, "All boss keys and stages are baked.");
            Check(source.GetComponentsInChildren<StationExtension>(true).Length == 0, "No extension pieces are required.");
            Check(source.GetComponent<ZNetView>() != null && source.GetComponent<WearNTear>() != null && source.GetComponent<Collider>() != null, "Native network, wear and collision components are present.");
            Check(source.GetComponent<Piece>().m_icon.texture.width <= 256 && source.GetComponent<Piece>().m_icon.texture.isReadable, "Icon follows the texture policy.");
            Check(source.GetComponent<Piece>().m_icon.texture.GetPixels32().Any(pixel => pixel.a == 0), "Icon background is transparent.");
            var authoredRecipe = source.GetComponent<MageRecipeProfile>();
            var resources = authoredRecipe.GetResources();

            Check(resources.Length == 1 && resources[0].ItemPrefab == "Wood" && resources[0].Amount == 10, "Only ten Wood is required.");
            var instance = Object.Instantiate(source, container.transform, false);
            var station = instance.GetComponent<CraftingStation>();

            harmony.Patch(AccessTools.Method(typeof(WearNTear), "ResetHighlight"), prefix: new HarmonyMethod(typeof(RuneBlockValidation), nameof(Skip)));
            harmony.Patch(AccessTools.Method(typeof(Destructible), "CreateFragments"), prefix: new HarmonyMethod(typeof(RuneBlockValidation), nameof(CaptureFragments)));
            var wear = instance.GetComponent<WearNTear>();
            var fragmentRpc = AccessTools.Method(typeof(WearNTear), "RPC_CreateFragments");

            Check(wear.m_fragmentRoots.Length == 0, "Rune Block has no stale references to removed Mage Table meshes.");
            fragmentRpc.Invoke(wear, new object[] { 0L });
            Check(_fragmentRoot == instance && _visibleFragmentsOnly, "Native removal RPC fragments the visible current stump without null dereferences.");

            Set(typeof(CraftingStation), station, "m_attachedExtensions", new List<StationExtension>());
            Check(station.GetLevel(false) == 1, "Meadows starts at level one.");
            station.m_areaMarker = null;
            Set(typeof(CraftingStation), station, "m_updateExtensionTimer", 2f);
            Vector3 bodySize = instance.GetComponent<BoxCollider>().size;
            var body = instance.GetComponent<BoxCollider>();

            Check(Mathf.Abs(body.center.y - body.size.y * 0.5f) < 0.001f, "Solid collision ends at the mesh ground origin, preventing the 28 cm placement lift.");

            Check(station.m_effectAreaCollider == null, "The physical collider is not a native expanding effect collider.");
            Check(station.GetLevel(true) == 1 && instance.GetComponent<BoxCollider>().size == bodySize, "Native extension refresh leaves physical stump collision unchanged.");
            var placementProbe = new GameObject("Placement collision probe");
            var placementBody = placementProbe.AddComponent<BoxCollider>();

            try
            {
                placementBody.center = body.center;
                placementBody.size = body.size;
                Physics.SyncTransforms();
                Check(Mathf.Abs(placementBody.ClosestPoint(Vector3.down * 5f).y) < 0.001f, "Native placement closest-point query touches ground at the mesh origin.");
            }
            finally
            {
                Object.DestroyImmediate(placementProbe);
            }

            var visualsType = Mod.GetType("HexBattleMage.Core.Managers.RuneBlockVisuals");
            var visuals = instance.AddComponent(visualsType);
            var apply = visualsType.GetMethod("ApplyLevel", Members);

            for (int index = 0; index < profile.BossDefeatKeys.Length; index++)
            {
                keys[profile.BossDefeatKeys[index]] = string.Empty;
                int level = index + 2;

                Check(station.GetLevel(false) == level, "Native station level updates from real world-key queries.");
                apply.Invoke(visuals, new object[] { instance.GetComponent<HexBattleMageRuneBlockProfile>(), level });

                for (int stage = 0; stage < profile.StageVisualPaths.Length; stage++)
                {
                    Check(instance.transform.Find(profile.StageVisualPaths[stage]).gameObject.activeSelf == (stage <= index), "Visual stages are cumulative and automatic.");
                }
            }

            keys.Clear();
            apply.Invoke(visuals, new object[] { instance.GetComponent<HexBattleMageRuneBlockProfile>(), station.GetLevel(false) });
            Check(profile.StageVisualPaths.All(path => !instance.transform.Find(path).gameObject.activeSelf), "Meadows visuals restore with fresh world progression.");
            var vanillaObject = new GameObject("Vanilla station");

            vanillaObject.transform.SetParent(container.transform, false);
            var vanilla = vanillaObject.AddComponent<CraftingStation>();

            Set(typeof(CraftingStation), vanilla, "m_attachedExtensions", new List<StationExtension>());
            Check(vanilla.GetLevel(false) == 1, "Vanilla station levels are unaffected.");
            ValidateNotifications(container);
            ValidateRecipes(bundle, container);
            ValidateRegistration(bundle, container, harmony);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "rune-block-validation-report.txt"), "PASS: native Jotunn registration and 10-Wood requirements; one networked station with no extensions; transparent readable icon; real world-key queries/native GetLevel at levels 1-8; cumulative visuals; vanilla pass-through; persistent per-player/per-world notification deduplication; wand/trinket recipe migration and station-scoped catalog. World/manager/lifecycle bootstrap are isolated fixtures.");
        }
        finally
        {
            Set(typeof(ZoneSystem), null, "s_instance", previousZone);
            Object.DestroyImmediate(container);
            bundle.Unload(true);
            harmony.UnpatchSelf();
        }
    }

    private static void ValidateNotifications(GameObject container)
    {
        var player = container.AddComponent<TrinketValidationPlayer>();

        player.m_customData = new Dictionary<string, string>();
        var record = Mod.GetType("HexBattleMage.Core.RuneBlockProgression").GetMethod("RecordLevel", Members);
        Func<long, int, bool> notify = (world, level) => (bool)record.Invoke(null, new object[] { player, world, level, 1 });

        Check(!(bool)record.Invoke(null, new object[] { player, 3L, 2, 2 }), "A tuned base level is silent on first discovery.");
        Check((bool)record.Invoke(null, new object[] { player, 3L, 3, 2 }), "A tuned base level still announces its next boss advancement.");
        Check(!notify(1L, 1), "Initial Meadows level is silent.");
        Check(notify(1L, 2) && !notify(1L, 2), "Boss advancement notifies once.");
        Check(notify(2L, 2), "Notification state is separate per world.");
        player.m_customData = new Dictionary<string, string>(player.m_customData);
        Check(!notify(1L, 2) && notify(1L, 4), "Restored saved notification state avoids duplicates and reports later progress.");
    }

    private static void ValidateRecipes(AssetBundle bundle, GameObject container)
    {
        foreach (string identity in new[] { "HexBattleMage_Wand_Meadows_01", "HexBattleMage_Trinket_Meadows_01" })
        {
            var recipe = bundle.LoadAsset<GameObject>(identity).GetComponent<MageRecipeProfile>();

            recipe.GetResources();
            string craftStation = identity.Contains("Wand") ? string.Empty : "HexBattleMage_RuneBlock";

            Check(recipe.CraftingStation == craftStation && recipe.RepairStation == "HexBattleMage_RuneBlock", "Starter wand crafts by hand; trinket crafts and both repair at Rune Block.");
        }

        var armor = bundle.LoadAsset<GameObject>("HexBattleMage_Chest_Meadows_01_Definition").GetComponent<MageRecipeProfile>();

        armor.GetResources();
        Check(armor.CraftingStation == "HexArcaneWorkbench", "Armor remains at the Mage Table.");
        var catalogEntry = Mod.GetType("HexBattleMage.Core.MageEquipmentCatalog+Entry");
        var entry = Activator.CreateInstance(catalogEntry, true);
        var rune = bundle.LoadAsset<GameObject>("HexBattleMage_RuneBlock").GetComponent<CraftingStation>();
        var mage = bundle.LoadAsset<GameObject>("HexArcaneWorkbench").GetComponent<CraftingStation>();
        var recipeObject = ScriptableObject.CreateInstance<Recipe>();

        recipeObject.m_craftingStation = rune;
        catalogEntry.GetField("Recipe", Members).SetValue(entry, recipeObject);
        var filter = Mod.GetType("HexBattleMage.Core.UI.MageUnlocksTab").GetMethod("IsForStation", Members);

        Check((bool)filter.Invoke(null, new object[] { entry, rune }) && !(bool)filter.Invoke(null, new object[] { entry, mage }), "Unlock catalogs are scoped to their station.");
        Object.DestroyImmediate(recipeObject);
        Mod.GetType("HexBattleMage.Assets.AssetBundleHelper").GetProperty("HexArcaneWorkbench", Members).SetValue(null, mage.gameObject);
        var player = container.GetComponent<TrinketValidationPlayer>();
        var style = Mod.GetType("HexBattleMage.Patches.WandStyleSelection");

        Set(typeof(Player), player, "m_currentStation", rune);
        Check((bool)style.GetMethod("IsAtRuneBlock", Members).Invoke(null, new object[] { player }), "Wand appearance controls work at the Rune Block.");
        Check(!(bool)style.GetMethod("IsAtMageTable", Members).Invoke(null, new object[] { player }), "Armor appearance controls remain separate.");
        Set(typeof(Player), player, "m_currentStation", mage);
        Check(!(bool)style.GetMethod("IsAtRuneBlock", Members).Invoke(null, new object[] { player }) && (bool)style.GetMethod("IsAtMageTable", Members).Invoke(null, new object[] { player }), "Style guards distinguish native current stations.");
    }

    private static void ValidateRegistration(AssetBundle bundle, GameObject container, Harmony harmony)
    {
        var prefabs = (PrefabManager)FormatterServices.GetUninitializedObject(typeof(PrefabManager));
        var pieces = (PieceManager)FormatterServices.GetUninitializedObject(typeof(PieceManager));
        var assets = (AssetManager)FormatterServices.GetUninitializedObject(typeof(AssetManager));

        Set(typeof(PrefabManager), null, "_instance", prefabs);
        Set(typeof(PrefabManager), prefabs, "<PrefabContainer>k__BackingField", container);
        Set(typeof(PrefabManager), prefabs, "<Prefabs>k__BackingField", new Dictionary<string, CustomPrefab>());
        Set(typeof(AssetManager), null, "instance", assets);
        harmony.Patch(AccessTools.Method(typeof(AssetManager), "AddAsset", new[] { typeof(Object), typeof(Object) }), prefix: new HarmonyMethod(typeof(RuneBlockValidation), nameof(Skip)));
        Set(typeof(PieceManager), null, "_instance", pieces);
        Set(typeof(PieceManager), pieces, "Pieces", new Dictionary<string, CustomPiece>());
        Set(typeof(PieceManager), pieces, "PieceCategories", new Dictionary<string, Piece.PieceCategory> { { "Battle Mage", (Piece.PieceCategory)99 } });
        Set(typeof(PieceManager), pieces, "OtherPieceCategories", new Dictionary<string, Piece.PieceCategory>());
        Set(typeof(PieceManager), pieces, "vanillaLabels", new Dictionary<Piece.PieceCategory, string>());
        Set(typeof(PieceManager), pieces, "PieceTables", new List<CustomPieceTable>());
        Set(typeof(PieceManager), pieces, "PieceTableMap", new Dictionary<string, PieceTable>());
        Set(typeof(PieceManager), pieces, "PieceTableNameMap", new Dictionary<string, string>());
        var mockType = typeof(PrefabManager).Assembly.GetType("Jotunn.Managers.MockManager", true);
        var mocks = FormatterServices.GetUninitializedObject(mockType);

        Set(mockType, null, "_instance", mocks);
        Set(mockType, mocks, "MockPrefabContainer", container);
        Set(mockType, mocks, "mockedPrefabs", new Dictionary<string, GameObject>());

        foreach (var method in typeof(Jotunn.Utils.BepInExUtils).GetMethods(Members).Where(method => method.Name == "GetSourceModMetadata"))
        {
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(RuneBlockValidation), nameof(SourceMod)));
        }

        Mod.GetType("HexBattleMage.Assets.AssetBundleHelper").GetProperty("Bundle", Members).SetValue(null, bundle);
        Mod.GetType("HexBattleMage.Core.Managers.RuneBlockManager").GetMethod("Register", Members).Invoke(null, null);
        var registered = (Dictionary<string, CustomPiece>)typeof(PieceManager).GetField("Pieces", Members).GetValue(pieces);

        Check(registered.Count == 1 && registered.ContainsKey("HexBattleMage_RuneBlock"), "Real Jotunn AddPiece registers one station.");
        var piece = registered["HexBattleMage_RuneBlock"];

        Check(piece.PieceTable == "_HammerPieceTable" && piece.Piece.m_category == (Piece.PieceCategory)99, "Station appears in the Battle Mage Hammer category: " + piece.PieceTable + "/" + piece.Category + "/" + piece.Piece.m_category);
        Check(piece.Piece.m_craftingStation == null && piece.Piece.m_resources.Length == 1 && piece.Piece.m_resources[0].m_amount == 10, "Building needs ten Wood and no previous station.");
    }

    private static void Set(Type type, object instance, string name, object value)
    {
        AccessTools.Field(type, name).SetValue(instance, value);
    }

    private static bool CaptureFragments(GameObject rootObject, bool visibleOnly)
    {
        _fragmentRoot = rootObject;
        _visibleFragmentsOnly = visibleOnly;
        return false;
    }

    private static bool Skip() => false;

    private static bool SourceMod(ref BepInPlugin __result)
    {
        __result = Metadata;
        return false;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

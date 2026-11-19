using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

public static class EquipmentUiValidation
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Assembly Mod = typeof(MageEitrProfile).Assembly;
    private static readonly List<GameObject> Objects = new List<GameObject>();

    private static bool _meadowsKnown = true;
    private static Heightmap.Biome _currentBiome = Heightmap.Biome.Meadows;
    private static Dictionary<string, string> _worldKeys;

    public static void Run()
    {
        var harmony = new Harmony("HexBattleMage.EquipmentUiValidation");
        var previousPlayer = Player.m_localPlayer;
        var previousZone = ZoneSystem.instance;
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE"));

        try
        {
            PlayerTextLocalizationValidation.InitializeForTests(harmony);
            PatchSkip(harmony, typeof(ItemDrop), "Awake");
            PatchSkip(harmony, typeof(InventoryGui), "Awake");
            PatchSkip(harmony, typeof(InventoryGui), "OnDestroy");
            PatchSkip(harmony, typeof(ZoneSystem), "Awake");
            PatchSkip(harmony, typeof(ZoneSystem), "Reset");
            PatchSkip(harmony, typeof(ZoneSystem), "OnDestroy");
            var zone = Create("World progression").AddComponent<ZoneSystem>();

            _worldKeys = new Dictionary<string, string>();
            AccessTools.Field(typeof(ZoneSystem), "s_instance").SetValue(null, zone);
            AccessTools.Field(typeof(ZoneSystem), "m_globalKeysValues").SetValue(zone, _worldKeys);
            harmony.Patch(AccessTools.Method(typeof(Player), "IsBiomeKnown", new[] { typeof(BiomeSector) }), prefix: new HarmonyMethod(typeof(EquipmentUiValidation), nameof(ReadBiomeKnowledge)));
            harmony.Patch(AccessTools.Method(typeof(Player), "GetCurrentBiome"), prefix: new HarmonyMethod(typeof(EquipmentUiValidation), nameof(ReadCurrentBiome)));
            _meadowsKnown = true;
            _currentBiome = Heightmap.Biome.Meadows;
            var player = Create("Player").AddComponent<TrinketValidationPlayer>();

            Player.m_localPlayer = player;
            AccessTools.Field(typeof(Player), "m_knownMaterial").SetValue(player, new HashSet<string> { "Wood" });
            var learned = new HashSet<string> { "Wayfarer Cloak" };

            AccessTools.Field(typeof(Player), "m_knownRecipes").SetValue(player, learned);
            var weapon = Item("Apprentice Wand");

            weapon.m_dropPrefab.name = "ValidationPrefabIdentity";
            weapon.m_shared.m_itemType = ItemDrop.ItemData.ItemType.TwoHandedWeapon;
            weapon.m_shared.m_damages = new HitData.DamageTypes { m_blunt = 10f, m_pierce = 10f, m_fire = 30f };
            AccessTools.Field(typeof(Humanoid), "m_rightItem").SetValue(player, weapon);
            Mod.GetType("HexBattleMage.Assets.AssetBundleHelper").GetProperty("HexBattleMageWand", Members).SetValue(null, weapon.m_dropPrefab);
            var shield = Item("Apprentice Ward Bracer");

            shield.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Hands;
            shield.m_equipped = true;
            var bracerDefinition = shield.m_dropPrefab.AddComponent<MageEquipmentDefinition>();
            bracerDefinition.Category = MageEquipmentCategory.Bracer;
            bracerDefinition.Slot = MageEquipmentSlot.Bracer;
            bracerDefinition.SupportsBlocking = true;
            player.GetInventory().GetAllItems().Add(shield);
            harmony.CreateClassProcessor(Mod.GetType("HexBattleMage.Patches.MagicShieldBlockerPatch")).Patch();
            shield.m_shared.m_blockPower = 6f;
            shield.m_shared.m_deflectionForce = 20f;
            shield.m_shared.m_timedBlockBonus = 1.5f;
            AccessTools.Field(typeof(Humanoid), "m_leftItem").SetValue(player, null);
            GearEitr(player, 5f, true);
            GearEitr(player, 7f, true);
            GearEitr(player, 100f, false);
            string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "EquipmentUi");

            Directory.CreateDirectory(directory);
            typeof(Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(directory, "core.cfg"));
            var config = new ConfigFile(Path.Combine(directory, "ui.cfg"), false);
            var dev = Mod.GetType("HexBattleMage.Core.DevConfig");

            dev.GetProperty("FullDamageRange", Members).SetValue(null, config.Bind("Range", "Full", 15f));
            dev.GetProperty("DamageFalloffDistance", Members).SetValue(null, config.Bind("Range", "Falloff", 20f));
            dev.GetProperty("MinimumDamageMultiplier", Members).SetValue(null, config.Bind("Range", "Minimum", 0.2f));
            var statsType = Mod.GetType("HexBattleMage.Core.MageGearStats");
            string stats = (string)statsType.GetMethod("GetText", Members).Invoke(null, null);

            string plain = Regex.Replace(stats, "<[^>]*>", string.Empty);
            Debug.Log(stats);
            Check(plain.Contains("Up to 15 m: 50"), "Full-damage range uses current tuning.");
            Check(plain.Contains("At 25 m: 30") && plain.Contains("35 m and beyond: 10"), "Range samples follow actual falloff.");
            Check(plain.Contains("+12 maximum Eitr") && !plain.Contains("+112"), "Only equipped gear contributes to the Eitr total.");
            Check(plain.Contains("Block armour: 6") && plain.Contains("Parry: 1.5x"), "Native blocker stats are shown.");
            Check(!plain.Contains("ValidationPrefabIdentity") && !plain.Contains("Latest impact"), "Technical identities and impact telemetry are removed.");
            config.Bind("Range", "Full", 15f).Value = 18f;
            string tuned = (string)statsType.GetMethod("GetText", Members).Invoke(null, null);

            Check(Regex.Replace(tuned, "<[^>]*>", string.Empty).Contains("Up to 18 m"), "Compendium reads live range tuning.");
            var unarmed = Item("Unarmed");

            unarmed.m_shared.m_damages = new HitData.DamageTypes { m_blunt = 99f };
            AccessTools.Field(typeof(Humanoid), "m_unarmedWeapon").SetValue(player, unarmed.m_dropPrefab.GetComponent<ItemDrop>());
            AccessTools.Field(typeof(Humanoid), "m_rightItem").SetValue(player, null);
            string unarmedSummary = (string)statsType.GetMethod("GetText", Members).Invoke(null, null);

            Check(!unarmedSummary.Contains("Unarmed") && !unarmedSummary.Contains("99") && !unarmedSummary.Contains("WAND RANGE"), "Native unarmed fallback is not displayed as a weapon.");
            Check(stats.Contains("<color=#FFA500>"), "Headings and values use native orange.");
            AccessTools.Field(typeof(Humanoid), "m_rightItem").SetValue(player, weapon);
            var catalog = Mod.GetType("HexBattleMage.Core.MageEquipmentCatalog");
            var register = catalog.GetMethod("Register", Members);
            var wood = Item("Wood").m_dropPrefab.GetComponent<ItemDrop>();
            var resin = Item("Resin").m_dropPrefab.GetComponent<ItemDrop>();
            var icon = bundle.LoadAsset<GameObject>("HexBattleMage_Cloak_Meadows_01").GetComponent<HexBattleMageCloakProfile>().Icon;

            foreach (string name in new[] { "Wayfarer Cloak", "Wayfarer Tunic", "Wayfarer Hood", "Wayfarer Leggings", "Apprentice Ward Bracer", "Apprentice Wand", "Stone of Reprisal" })
            {
                var item = Item(name);
                item.m_dropPrefab.AddComponent<MageEquipmentDefinition>();
                var recipe = ScriptableObject.CreateInstance<Recipe>();

                item.m_shared.m_icons = new[] { icon };
                recipe.m_item = item.m_dropPrefab.GetComponent<ItemDrop>();
                recipe.m_resources = new[] { new Piece.Requirement { m_resItem = wood, m_amount = 1 }, new Piece.Requirement { m_resItem = resin, m_amount = 1 } };
                register.Invoke(null, new object[] { recipe.m_item, recipe, Heightmap.Biome.Meadows });
            }

            ValidateProgression(player, catalog, register, icon);
            ValidateLayout(player, stats);
            var authoredRange = bundle.LoadAsset<GameObject>("HexBattleMage_Wand_Meadows_01").GetComponent<HexBattleMageWandProfile>().FullDamageRange;

            Check(authoredRange == 15f, "The 15-metre default is authored on the bundled wand.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "equipment-ui-report.txt"), "PASS: live range samples and tuning; native blocking/parry; equipped-only Eitr; no prefab identities or impact telemetry; unarmed fallback hidden; orange headings/values; Meadows discovery and actual native world boss keys; future recipe knowledge does not bypass catalog gates; seven gear rows under a collapsible biome; native learned-recipe lock labels; whole-biome collapse/expand; live unlock refresh; wrapped bounded UI; authored 15m range. Player/inventory GUI bootstrap and font are fixtures; live game navigation remains a manual check.");
        }
        finally
        {
            Player.m_localPlayer = previousPlayer;
            AccessTools.Field(typeof(ZoneSystem), "s_instance").SetValue(null, previousZone);

            foreach (var item in Objects)
            {
                Object.DestroyImmediate(item);
            }

            Objects.Clear();
            bundle.Unload(true);
            harmony.UnpatchSelf();
        }
    }

    private static int GearRows(RectTransform content)
    {
        return content.GetComponentsInChildren<RectTransform>().Count(rect => rect.name.StartsWith("Gear_", StringComparison.Ordinal));
    }

    private static void ValidateProgression(Player player, Type catalog, MethodInfo register, Sprite icon)
    {
        foreach (var rule in new[] { new { Biome = Heightmap.Biome.BlackForest, Key = "defeated_eikthyr" }, new { Biome = Heightmap.Biome.Swamp, Key = "defeated_gdking" } })
        {
            var item = Item(rule.Biome + " Future Gear");
            var definition = item.m_dropPrefab.AddComponent<MageEquipmentDefinition>();

            definition.CatalogBiome = rule.Biome.ToString();
            definition.CatalogRequiresBiomeDiscovery = false;
            definition.CatalogBossDefeatKey = rule.Key;
            item.m_shared.m_icons = new[] { icon };
            var recipe = ScriptableObject.CreateInstance<Recipe>();

            recipe.m_item = item.m_dropPrefab.GetComponent<ItemDrop>();
            recipe.m_resources = new Piece.Requirement[0];
            register.Invoke(null, new object[] { recipe.m_item, recipe, rule.Biome });
            ((HashSet<string>)AccessTools.Field(typeof(Player), "m_knownRecipes").GetValue(player)).Add(item.m_shared.m_name);
        }

        var get = catalog.GetMethod("GetEntries", Members);
        var visible = catalog.GetMethod("IsVisible", Members);
        Func<int> count = () => ((System.Collections.IEnumerable)get.Invoke(null, null)).Cast<object>().Count(entry => (bool)visible.Invoke(null, new object[] { entry, player }));

        _meadowsKnown = false;
        _currentBiome = Heightmap.Biome.None;
        Check(count() == 0, "Undiscovered Meadows and future tiers are hidden.");
        _currentBiome = Heightmap.Biome.Meadows;
        Check(count() == 7, "First arrival in Meadows reveals its complete catalog.");
        _meadowsKnown = true;
        _currentBiome = Heightmap.Biome.None;
        Check(count() == 7, "Meadows remains revealed after leaving it.");
        _worldKeys.Add("defeated_eikthyr", string.Empty);
        Check(count() == 8, "Actual native world-key lookup reveals Black Forest after Eikthyr.");
        _worldKeys.Add("defeated_gdking", string.Empty);
        Check(count() == 9, "Elder defeat reveals Swamp.");
        _worldKeys.Clear();
        Check(count() == 7, "World progression is read live and learned recipes do not bypass hidden tiers.");
    }

    private static bool ReadBiomeKnowledge(ref bool __result)
    {
        __result = _meadowsKnown;
        return false;
    }

    private static bool ReadCurrentBiome(ref Heightmap.Biome __result)
    {
        __result = _currentBiome;
        return false;
    }

    private static void ValidateLayout(Player player, string stats)
    {
        var settings = ScriptableObject.CreateInstance<TMP_Settings>();
        AccessTools.Field(typeof(TMP_Settings), "s_Instance").SetValue(null, settings);
        var font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Validation/Arial.ttf"));
        AccessTools.Field(typeof(TMP_Settings), "m_defaultFontAsset").SetValue(settings, font);
        AccessTools.Field(typeof(TMP_Settings), "m_defaultFontSize").SetValue(settings, 18f);
        AccessTools.Field(typeof(TMP_Settings), "m_fallbackFontAssets").SetValue(settings, new List<TMP_FontAsset>());
        var canvas = Create("Canvas").AddComponent<Canvas>();
        var camera = Create("Camera").AddComponent<Camera>();
        var target = new RenderTexture(600, 800, 24);

        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.orthographic = true;
        camera.targetTexture = target;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        var panel = new GameObject("Crafting", typeof(RectTransform));

        panel.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)panel.transform;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var gui = Create("Inventory GUI").AddComponent<InventoryGui>();

        gui.m_crafting = rect;
        gui.m_tabUpgrade = Tab(panel.transform, 140f);
        gui.m_tabCraft = Tab(panel.transform, 20f);
        var template = new GameObject("Description", typeof(RectTransform));

        template.transform.SetParent(panel.transform, false);
        template.SetActive(false);
        gui.m_recipeDecription = template.AddComponent<TextMeshProUGUI>();
        gui.m_recipeDecription.font = font;
        var type = Mod.GetType("HexBattleMage.Core.UI.MageUnlocksTab");
        var component = (Behaviour)gui.gameObject.AddComponent(type);

        type.GetMethod("Initialize", Members).Invoke(component, new object[] { gui });
        component.enabled = false;
        Canvas.ForceUpdateCanvases();
        type.GetMethod("Open", Members).Invoke(component, null);
        var content = (RectTransform)type.GetField("_content", Members).GetValue(component);

        Check(GearRows(content) == 7, "A revealed biome lists all seven gear entries.");
        Check(content.GetComponentsInChildren<Button>().Length == 1, "Only the biome heading is collapsible.");
        Check(content.GetComponentsInChildren<TMP_Text>().Any(text => text.gameObject.name == "Requirements"), "Locked item requirements are shown directly.");
        Check(content.GetComponentsInChildren<TMP_Text>().Count(text => text.text == "UNLOCKED") == 1, "Learned recipes alone determine unlocked state.");
        var row = content.GetComponentsInChildren<Button>().First();

        row.onClick.Invoke();
        Check(GearRows(content) == 0, "Collapsing Meadows hides every item together.");
        content.GetComponentsInChildren<Button>().First().onClick.Invoke();
        Check(GearRows(content) == 7, "Expanding Meadows restores all gear rows.");
        ((HashSet<string>)AccessTools.Field(typeof(Player), "m_knownRecipes").GetValue(player)).Add("Wayfarer Tunic");
        type.GetMethod("RefreshText", Members).Invoke(component, new object[] { player });
        Check(content.GetComponentsInChildren<TMP_Text>().Count(text => text.text == "UNLOCKED") == 2, "Unlock status refreshes from current player state.");
        Canvas.ForceUpdateCanvases();

        foreach (var text in content.GetComponentsInChildren<TMP_Text>())
        {
            text.ForceMeshUpdate();
            Check(!text.isTextOverflowing, "Labels remain inside their allotted width and height.");
        }

        var scroll = (ScrollRect)type.GetField("_scroll", Members).GetValue(component);
        var scrollbar = scroll.verticalScrollbar;
        var collapseAll = (Button)type.GetField("_collapseAllButton", Members).GetValue(component);

        Check(scrollbar != null && scrollbar.gameObject.activeInHierarchy && scrollbar.handleRect.rect.width >= 12f, "A visible draggable scrollbar has a usable handle.");
        var viewportCorners = new Vector3[4];
        var scrollbarCorners = new Vector3[4];

        scroll.viewport.GetWorldCorners(viewportCorners);
        scrollbar.GetComponent<RectTransform>().GetWorldCorners(scrollbarCorners);
        Check(scrollbarCorners[0].x >= viewportCorners[3].x, "Scrollbar space is reserved beside the viewport.");
        scroll.verticalNormalizedPosition = 1f;
        scroll.OnScroll(new PointerEventData(null) { scrollDelta = Vector2.down });
        Check(content.anchoredPosition.y >= 100f, "One wheel notch moves the list by more than a row.");
        scrollbar.value = 0f;
        Check(scroll.verticalNormalizedPosition < 0.01f, "Dragging the scrollbar can reach the bottom.");
        collapseAll.onClick.Invoke();
        Check(GearRows(content) == 0 && collapseAll.GetComponentInChildren<TMP_Text>().text == "Expand All", "Collapse All hides the entire biome and offers Expand All.");
        collapseAll.onClick.Invoke();
        Check(GearRows(content) == 7 && collapseAll.GetComponentInChildren<TMP_Text>().text == "Collapse All", "Expand All restores the list.");
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 1f;
        typeof(ScrollRect).GetMethod("LateUpdate", Members).Invoke(scroll, null);
        Save(camera, target, "unlock-list-review.png");
        type.GetMethod("Close", Members).Invoke(component, null);
        var statsObject = new GameObject("Stats", typeof(RectTransform));

        statsObject.transform.SetParent(panel.transform, false);
        var statsRect = (RectTransform)statsObject.transform;

        statsRect.anchorMin = Vector2.zero;
        statsRect.anchorMax = Vector2.one;
        statsRect.offsetMin = new Vector2(18f, 18f);
        statsRect.offsetMax = new Vector2(-18f, -90f);
        var label = statsObject.AddComponent<TextMeshProUGUI>();

        label.font = font;
        label.fontSize = 20f;
        label.text = stats;
        label.textWrappingMode = TextWrappingModes.Normal;
        Canvas.ForceUpdateCanvases();
        label.ForceMeshUpdate();
        Check(!label.isTextOverflowing, "Equipment summary fits a compact compendium-sized panel.");
        Save(camera, target, "equipment-summary-review.png");
        camera.targetTexture = null;
        Object.DestroyImmediate(target);
    }

    private static Button Tab(Transform parent, float x)
    {
        var root = new GameObject("Tab", typeof(RectTransform), typeof(Image), typeof(Button));

        root.transform.SetParent(parent, false);
        var rect = (RectTransform)root.transform;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -40f);
        rect.sizeDelta = new Vector2(110f, 38f);
        new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).transform.SetParent(root.transform, false);
        return root.GetComponent<Button>();
    }

    private static void Save(Camera camera, RenderTexture target, string file)
    {
        camera.Render();
        RenderTexture.active = target;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);

        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath)), file), texture.EncodeToPNG());
        RenderTexture.active = null;
        Object.DestroyImmediate(texture);
    }

    private static void GearEitr(Humanoid player, float value, bool equipped)
    {
        var item = Item("Gear Eitr");

        item.m_equipped = equipped;
        item.m_dropPrefab.AddComponent<MageEitrProfile>().MaximumEitr = value;
        player.GetInventory().GetAllItems().Add(item);
    }

    private static ItemDrop.ItemData Item(string name)
    {
        var root = Create(name);
        var item = root.AddComponent<ItemDrop>().m_itemData;

        item.m_shared = new ItemDrop.ItemData.SharedData { m_name = name };
        item.m_quality = 1;
        item.m_dropPrefab = root;
        return item;
    }

    private static GameObject Create(string name)
    {
        var root = new GameObject(name);

        Objects.Add(root);
        return root;
    }

    private static void PatchSkip(Harmony harmony, Type type, string name)
    {
        var method = AccessTools.Method(type, name);

        if (method != null)
        {
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(EquipmentUiValidation), nameof(Skip)));
        }
    }

    private static bool Skip() => false;

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;
using Jotunn.Entities;
using Jotunn.Managers;
using TMPro;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

public static class StarterWandProgressionValidation
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Assembly Mod = typeof(HexBattleMageWandProfile).Assembly;
    private static readonly BepInPlugin Metadata = new BepInPlugin("test.starterwand", "Starter Wand Validation", "1.0.0");

    private static readonly Dictionary<int, StatusEffect> GroupChildren = new Dictionary<int, StatusEffect>();
    private static readonly List<int> GroupCalls = new List<int>();

    public static void Run()
    {
        var harmony = new Harmony("HexBattleMage.StarterWandProgressionValidation");
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE"));
        var root = new GameObject("Starter wand fixture");
        var previousPlayer = Player.m_localPlayer;
        var previousDb = ObjectDB.instance;
        var previousZone = ZoneSystem.instance;

        root.SetActive(false);

        try
        {
            foreach (var type in new[] { typeof(ItemDrop), typeof(ObjectDB), typeof(CraftingStation), typeof(ZoneSystem), typeof(ZNetView), typeof(Piece), typeof(InventoryGui) })
            {
                foreach (string name in new[] { "Awake", "OnDestroy", "OnValidate", "Reset" })
                {
                    var method = AccessTools.DeclaredMethod(type, name);

                    if (method != null)
                    {
                        harmony.Patch(method, prefix: new HarmonyMethod(typeof(StarterWandProgressionValidation), nameof(Skip)));
                    }
                }
            }

            harmony.Patch(AccessTools.Method(typeof(ItemDrop), "EnsureValid"), postfix: new HarmonyMethod(typeof(StarterWandProgressionValidation), nameof(RestoreDropReference)));
            harmony.Patch(AccessTools.Method(typeof(Character), "Message"), prefix: new HarmonyMethod(typeof(StarterWandProgressionValidation), nameof(Skip)));
            PlayerTextLocalizationValidation.InitializeForTests(harmony);
            var helper = Mod.GetType("HexBattleMage.Assets.AssetBundleHelper");
            var wand = bundle.LoadAsset<GameObject>("HexBattleMage_Wand_Meadows_01");

            helper.GetProperty("Bundle", Members).SetValue(null, bundle);
            helper.GetProperty("HexBattleMageWand", Members).SetValue(null, wand);
            Mod.GetType("HexBattleMage.Plugin").GetField("Log", Members).SetValue(null, new ManualLogSource("StarterWandTest"));
            var db = root.AddComponent<ObjectDB>();

            Set(typeof(ObjectDB), null, "m_instance", db);
            db.m_items = new List<GameObject> { wand };

            foreach (string name in new[] { "Wood", "Stone", "Resin", "Feathers", "TrophySkeleton", "SurtlingCore", "MushroomYellow" })
            {
                var material = new GameObject(name);

                material.transform.SetParent(root.transform, false);
                var drop = material.AddComponent<ItemDrop>();

                drop.m_itemData.m_shared = new ItemDrop.ItemData.SharedData { m_name = "$item_" + name.ToLowerInvariant(), m_maxStackSize = 50, m_maxQuality = 1 };
                drop.m_itemData.m_dropPrefab = material;
                db.m_items.Add(material);
            }

            AccessTools.Method(typeof(ObjectDB), "UpdateRegisters").Invoke(db, null);
            var zone = root.AddComponent<ZoneSystem>();

            Set(typeof(ZoneSystem), null, "s_instance", zone);
            Set(typeof(ZoneSystem), zone, "m_globalKeysValues", new Dictionary<string, string>());
            var player = root.AddComponent<TrinketValidationPlayer>();
            Player.m_localPlayer = player;
            player.m_customData = new Dictionary<string, string>();
            var known = new HashSet<string>();

            Set(typeof(Player), player, "m_knownMaterial", known);
            Set(typeof(Player), player, "m_knownRecipes", new HashSet<string>());
            Mod.GetType("HexBattleMage.Core.Tuning.PrefabTuning").GetMethod("Apply", Members).Invoke(null, new object[] { bundle });
            string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "StarterWandProgression");

            Directory.CreateDirectory(directory);
            typeof(Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(directory, "core.cfg"));
            var config = new ConfigFile(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".cfg"), false) { SaveOnConfigSet = false };
            var dev = Mod.GetType("HexBattleMage.Core.DevConfig");

            foreach (var pair in new Dictionary<string, float> { { "WandBluntDamage", 5f }, { "WandPierceDamage", 5f }, { "WandBluntDamagePerQuality", 1f }, { "WandPierceDamagePerQuality", 1f } })
            {
                dev.GetProperty(pair.Key, Members).SetValue(null, config.Bind("Physical", pair.Key, pair.Value));
            }

            var elements = Mod.GetType("HexBattleMage.Core.Elements.WandElementManager");

            elements.GetMethod("Configure", Members).Invoke(null, new object[] { config });
            Check(!((ConfigEntry<bool>)elements.GetProperty("BypassAttunements", Members).GetValue(null)).Value, "Progression is not bypassed by the old global element selector.");
            var registration = Register(wand, root, harmony);
            elements.GetMethod("Initialize", Members).Invoke(null, new object[] { config });
            int effectCount = ((HashSet<CustomStatusEffect>)AccessTools.Field(typeof(ItemManager), "StatusEffects").GetValue(ItemManager.Instance)).Count;

            Check(effectCount == (1 << Enum.GetValues(typeof(WandElement)).Length) - 1, "Actual Jotunn registration includes every single and grouped effect hash.");
            var craft = registration.Item1;
            var upgrade = registration.Item2;
            var rune = Object.Instantiate(bundle.LoadAsset<GameObject>("HexBattleMage_RuneBlock"), root.transform).GetComponent<CraftingStation>();

            Set(typeof(CraftingStation), rune, "m_attachedExtensions", new List<StationExtension>());
            craft.m_item = wand.GetComponent<ItemDrop>();
            upgrade.m_item = craft.m_item;
            craft.m_craftingStation = null;
            craft.m_repairStation = rune;
            upgrade.m_craftingStation = rune;
            upgrade.m_repairStation = rune;

            foreach (var recipe in new[] { craft, upgrade })
            {
                foreach (var resource in recipe.m_resources)
                {
                    resource.m_resItem = db.m_items.Single(item => resource.m_resItem.name.EndsWith(item.name, StringComparison.Ordinal)).GetComponent<ItemDrop>();
                }
            }

            db.m_recipes = new List<Recipe> { craft, upgrade };
            harmony.CreateClassProcessor(Mod.GetType("HexBattleMage.Core.StarterWandRecipeRules+UpgradeStationPatch")).Patch();
            harmony.CreateClassProcessor(Mod.GetType("HexBattleMage.Core.StarterWandRecipeRules+InventoryCraftingStationPatch")).Patch();
            harmony.CreateClassProcessor(Mod.GetType("HexBattleMage.Patches.RuneBlockLevelPatch")).Patch();
            harmony.CreateClassProcessor(Mod.GetType("HexBattleMage.Patches.WandAttunedDamagePatch")).Patch();
            Check(craft.m_resources.Length == 2 && craft.m_resources[0].m_amount == 10 && craft.m_resources[1].m_amount == 2, "Baked/tuned inventory costs are ten Wood and two Stone.");
            Check(!player.HaveRequirements(craft, true, 1, 1), "Unknown materials do not reveal the starter wand.");
            known.Add(db.GetItemPrefab("Wood").GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
            Check(!player.HaveRequirements(craft, true, 1, 1), "Wood alone is insufficient for discovery.");
            known.Add(db.GetItemPrefab("Stone").GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
            Check(player.HaveRequirements(craft, true, 1, 1), "Native discovery unlocks the wand from Wood and Stone, without a station.");
            Set(typeof(Player), player, "m_knownRecipes", new HashSet<string> { wand.GetComponent<ItemDrop>().m_itemData.m_shared.m_name });
            var available = new List<Recipe>();

            player.GetAvailableRecipes(ref available);
            Check(available.SequenceEqual(new[] { craft }), "Native inventory recipe selection includes only the handcraft recipe.");
            Check(!player.RequiredCraftingStation(craft, 2, true), "Upgrading the handcraft recipe cannot bypass the Rune Block.");
            var vanillaObject = new GameObject("piece_workbench");

            vanillaObject.transform.SetParent(root.transform, false);
            var vanilla = vanillaObject.AddComponent<CraftingStation>();

            vanilla.m_showBasicRecipies = true;
            vanilla.m_name = "$piece_workbench";
            Set(typeof(Player), player, "m_currentStation", vanilla);
            player.GetAvailableRecipes(ref available);
            Check(available.Count == 0, "Vanilla Workbench must not inherit the inventory wand recipe.");
            var unrelated = ScriptableObject.CreateInstance<Recipe>();

            Check(player.RequiredCraftingStation(unrelated, 1, false), "Vanilla inventory recipes still appear at the normal Workbench.");
            Object.DestroyImmediate(unrelated);
            Set(typeof(Player), player, "m_currentStation", rune);
            player.GetAvailableRecipes(ref available);
            Check(available.SequenceEqual(new[] { upgrade }) && !upgrade.m_noCraftOnlyUpgrade, "Rune Block exposes one craftable wand recipe without duplicating the inventory recipe.");
            Check(upgrade.m_resources.Select(resource => resource.m_amount).SequenceEqual(craft.m_resources.Select(resource => resource.m_amount)), "Station crafting costs match inventory crafting.");
            var sourceItem = wand.GetComponent<ItemDrop>().m_itemData;

            sourceItem.m_dropPrefab = wand;
            var first = sourceItem.Clone();
            var second = sourceItem.Clone();

            player.GetInventory().GetAllItems().Add(first);
            player.GetInventory().GetAllItems().Add(second);
            first.m_gridPos = new Vector2i(0, 0);
            second.m_gridPos = new Vector2i(1, 0);
            first.m_quality = 2;
            first.m_variant = 1;
            first.m_durability = 79f;
            var profile = bundle.LoadAsset<GameObject>(HexBattleMageRuneBlockProfile.PrefabName).GetComponent<HexBattleMageRuneBlockProfile>();
            Check(profile.AttunementPreviews.Select(preview => preview.Id).SequenceEqual(new[] { "Spirit", "Frost", "Earth" }), "The Rune Block exposes the future preview catalogue.");
            var fire = profile.Attunements.Select(reference => reference.GetComponent<HexBattleMageWandAttunementProfile>()).Single(definition => definition.Element == WandElement.Fire);
            var attunements = Mod.GetType("HexBattleMage.Core.Elements.WandAttunements");
            var learnMethod = attunements.GetMethod("TryLearn", Members);
            var activateMethod = attunements.GetMethod("TryActivate", Members);
            Func<HexBattleMageWandAttunementProfile, bool> learn = definition => (bool)learnMethod.Invoke(null, new object[] { player, definition });
            Func<HexBattleMageWandAttunementProfile, WandElement?, bool> activate = (definition, replace) => (bool)activateMethod.Invoke(null, new object[] { player, definition, replace });

            first.m_customData["HexBattleMage.Wand.Element"] = "Fire";
            first.m_customData["HexBattleMage.Wand.Learned.Fire"] = "1";
            Check(first.GetDamage().m_fire == 0f && first.GetDamage().m_blunt == 6f, "Legacy item data cannot grant character attunements.");
            Check(!learn(fire), "Fire cannot be learned before discovery and payment.");
            var feathers = db.GetItemPrefab("Feathers").GetComponent<ItemDrop>().m_itemData.Clone();
            var resin = db.GetItemPrefab("Resin").GetComponent<ItemDrop>().m_itemData.Clone();

            var wood = db.GetItemPrefab("Wood").GetComponent<ItemDrop>().m_itemData.Clone();

            wood.m_stack = 10;
            player.GetInventory().GetAllItems().Add(wood);
            feathers.m_stack = 3;
            resin.m_stack = 1;
            player.GetInventory().GetAllItems().Add(feathers);
            player.GetInventory().GetAllItems().Add(resin);
            known.Add(feathers.m_shared.m_name);
            known.Add(resin.m_shared.m_name);
            Check(!learn(fire) && feathers.m_stack == 3 && resin.m_stack == 1 && wood.m_stack == 10 && player.m_customData.Count == 0, "Failed payment never partially spends or learns.");
            resin.m_stack = 2;
            Set(typeof(Player), player, "m_currentStation", null);
            Check(!learn(fire) && !activate(fire, null), "Learning and activating require the Rune Block.");
            Set(typeof(Player), player, "m_currentStation", rune);
            player.GetInventory().GetAllItems().Remove(first);
            player.GetInventory().GetAllItems().Remove(second);
            Check(learn(fire), "Character learning does not require carrying a wand.");
            Check(!learn(fire), "Already learned elements cannot be charged twice.");
            Check(player.GetInventory().CountItems(feathers.m_shared.m_name) == 0 && player.GetInventory().CountItems(resin.m_shared.m_name) == 0 && player.GetInventory().CountItems(wood.m_shared.m_name) == 0, "Learning spends exactly two resin, three feathers and ten wood.");
            Check(first.GetDamage().m_fire == 0f, "Learning and activation are distinct actions.");
            Check(activate(fire, null), "Learned Fire activates freely at the Rune Block.");
            player.GetInventory().GetAllItems().Add(first);
            player.GetInventory().GetAllItems().Add(second);
            Check(first.GetDamage().m_fire == 17f && second.GetDamage().m_fire == 15f, "All wands follow the character's active Fire with their own quality scaling.");
            Check(first.m_quality == 2 && first.m_variant == 1 && first.m_durability == 79f, "Character attunement preserves item quality, style and durability.");
            first.m_shared = sourceItem.m_shared;
            Check(first.GetDamage().m_fire == 17f, "Native equipment-stat reset cannot erase character attunement.");
            var savedCharacter = new Dictionary<string, string>(player.m_customData);

            player.m_customData.Clear();
            Check(first.GetDamage().m_fire == 0f && second.GetDamage().m_fire == 0f, "Stored item stats cannot bypass the character's current selection.");
            player.m_customData = savedCharacter;
            Check(first.GetDamage().m_fire == 17f, "Restoring saved character custom data restores active learning.");
            var otherObject = new GameObject("Other character");

            otherObject.SetActive(false);
            otherObject.transform.SetParent(root.transform, false);
            var other = otherObject.AddComponent<TrinketValidationPlayer>();

            other.m_customData = new Dictionary<string, string>();
            player.GetInventory().GetAllItems().Remove(first);
            other.GetInventory().GetAllItems().Add(first);
            Player.m_localPlayer = other;
            Check(first.GetDamage().m_fire == 0f, "Another character cannot inherit the wand owner's attunement from the item.");
            other.GetInventory().GetAllItems().Remove(first);
            player.GetInventory().GetAllItems().Add(first);
            Player.m_localPlayer = player;
            ValidateCapacity(profile, player, root, first, second, fire, learn, activate, attunements, harmony);
            var package = new ZPackage();

            player.GetInventory().Save(package);
            var restored = new Inventory("Restored", null, 8, 4);

            package.SetPos(0);
            restored.Load(package);
            var restoredWands = restored.GetAllItems().Where(item => item.m_dropPrefab == wand).ToArray();
            Check(restoredWands.Length == 2 && restoredWands.All(item => item.GetDamage().m_fire > 0f), "Native item save/load still follows character Fire on every copy.");
            player.GetInventory().GetAllItems().Clear();
            var replacement = sourceItem.Clone();

            player.GetInventory().GetAllItems().Add(replacement);
            Check(replacement.GetDamage().m_fire > 0f, "A fresh replacement wand automatically uses the character's attunement.");
            ValidateElementDamage(player, replacement, harmony);
            ValidateUi(player, root, harmony, false);
            ValidateUi(player, root, harmony, true);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "starter-wand-progression-report.txt"), "PASS: native inventory/Rune Block recipes and Workbench exclusion; character learning/payment, separate activation, no-wand learning, shared effect across spare/replacement wands, character isolation, restored character data, item save/load, capacity/replacement enforcement at limits 1 and 2, grouped damage and character-focused station UI. World/manager/material/message bootstrap remains isolated.");
        }
        finally
        {
            Player.m_localPlayer = previousPlayer;
            Set(typeof(ObjectDB), null, "m_instance", previousDb);
            Set(typeof(ZoneSystem), null, "s_instance", previousZone);
            Object.DestroyImmediate(root);
            bundle.Unload(true);
            harmony.UnpatchSelf();
        }
    }

    private static void ValidateCapacity(HexBattleMageRuneBlockProfile profile, Player player, GameObject root, ItemDrop.ItemData first, ItemDrop.ItemData second, HexBattleMageWandAttunementProfile fire, Func<HexBattleMageWandAttunementProfile, bool> learn, Func<HexBattleMageWandAttunementProfile, WandElement?, bool> activate, Type attunements, Harmony harmony)
    {
        var authored = profile.Attunements;
        var spiritObject = new GameObject("Fixture Spirit");

        spiritObject.transform.SetParent(root.transform, false);
        var spirit = spiritObject.AddComponent<HexBattleMageWandAttunementProfile>();
        var recipe = spiritObject.AddComponent<MageRecipeProfile>();

        spirit.Element = WandElement.Spirit;
        recipe.CraftingStation = HexBattleMageRuneBlockProfile.PrefabName;
        recipe.Resources = new MageRecipeResource[0];
        profile.Attunements = new[] { fire.gameObject, spiritObject };
        Check(learn(spirit), "Additional authored definitions use the same character learning path.");
        Check(!activate(spirit, null), "At limit one, activating another element requires explicit replacement.");
        Check(activate(spirit, WandElement.Fire), "Explicit replacement switches Fire to Spirit.");
        Check(first.GetDamage().m_fire == 0f && second.GetDamage().m_fire == 0f && first.GetDamage().m_spirit > 0f && second.GetDamage().m_spirit > 0f, "Switching the character changes every wand; spares cannot retain Fire.");
        profile.MaximumActiveAttunements = 2;
        Check(activate(fire, null), "Increasing the source-authored cap allows a second active element.");
        Check(first.GetDamage().m_fire > 0f && first.GetDamage().m_spirit > 0f && first.m_shared.m_attackStatusEffect is WandElementGroupStatus, "Multiple active elements combine damage and use a grouped elemental status.");
        ValidateGroup((WandElementGroupStatus)first.m_shared.m_attackStatusEffect, root, harmony);
        profile.MaximumActiveAttunements = 1;
        Check(((System.Collections.ICollection)attunements.GetMethod("GetActive", Members).Invoke(null, new object[] { player })).Count == 1, "Reducing the limit never exposes excess saved active entries.");
        player.m_customData["HexBattleMage.Attunements.Active"] = "1";
        profile.Attunements = authored;
    }

    private static void ValidateGroup(WandElementGroupStatus group, GameObject root, Harmony harmony)
    {
        var targetObject = new GameObject("Grouped effect target");

        targetObject.SetActive(false);
        targetObject.transform.SetParent(root.transform, false);
        var target = targetObject.AddComponent<TrinketValidationEnemy>();
        var effects = (SEMan)FormatterServices.GetUninitializedObject(typeof(SEMan));

        Set(typeof(Character), target, "m_seman", effects);
        Set(typeof(StatusEffect), group, "m_character", target);
        GroupCalls.Clear();
        GroupChildren.Clear();
        harmony.Patch(AccessTools.Method(typeof(SEMan), "AddStatusEffect", new[] { typeof(int), typeof(bool), typeof(int), typeof(float), typeof(short) }), prefix: new HarmonyMethod(typeof(StarterWandProgressionValidation), nameof(CaptureGroupAdd)));
        harmony.Patch(AccessTools.Method(typeof(SEMan), "GetStatusEffect", new[] { typeof(int) }), prefix: new HarmonyMethod(typeof(StarterWandProgressionValidation), nameof(CaptureGroupGet)));
        var manager = Mod.GetType("HexBattleMage.Core.Elements.WandElementManager");
        int fire = (int)manager.GetMethod("GetHash", Members).Invoke(null, new object[] { WandElement.Fire });
        int spirit = (int)manager.GetMethod("GetHash", Members).Invoke(null, new object[] { WandElement.Spirit });

        GroupChildren[fire] = ScriptableObject.CreateInstance<WandElementStatus>();
        GroupChildren[spirit] = ScriptableObject.CreateInstance<WandElementStatus>();
        group.SetLevel(1, 0f);
        group.SetAttacker(target);
        Check(GroupCalls.Count == 2 && GroupCalls.Contains(fire) && GroupCalls.Contains(spirit), "Grouped impact dispatches each selected effect exactly once.");
        Check(GroupChildren.Values.All(effect => AccessTools.Field(typeof(WandElementStatus), "_attacker").GetValue(effect) == target), "Grouped effects retain their actual attacker.");
        Check(group.IsDone() && (bool)manager.GetMethod("IsWandElementHash", Members).Invoke(null, new object[] { group.NameHash() }), "Grouped impacts are recognized by custom native damage routing and expire immediately.");

        foreach (var effect in GroupChildren.Values)
        {
            Object.DestroyImmediate(effect);
        }

        Set(typeof(StatusEffect), group, "m_character", null);
    }

    private static bool CaptureGroupAdd(int __0, ref StatusEffect __result)
    {
        GroupCalls.Add(__0);
        __result = GroupChildren[__0];
        return false;
    }

    private static bool CaptureGroupGet(int __0, ref StatusEffect __result)
    {
        __result = GroupChildren[__0];
        return false;
    }

    private static void ValidateElementDamage(Player player, ItemDrop.ItemData wand, Harmony harmony)
    {
        var formatter = Mod.GetType("HexBattleMage.Core.Elements.WandElementText").GetMethod("Damage", Members);
        var patch = Mod.GetType("HexBattleMage.Patches.WandElementTooltipPatch");
        var postfix = patch.GetMethod("Postfix", Members);
        var elements = Mod.GetType("HexBattleMage.Core.Elements.WandElementManager");
        var settings = elements.GetMethod("GetSettings", Members).Invoke(null, new object[] { WandElement.Blight });
        var total = (ConfigEntry<float>)settings.GetType().GetProperty("TotalDamage", Members).GetValue(settings);
        var duration = (ConfigEntry<float>)settings.GetType().GetProperty("Duration", Members).GetValue(settings);
        var saved = new Dictionary<string, string>(player.m_customData);
        float originalTotal = total.Value;
        float originalDuration = duration.Value;

        harmony.CreateClassProcessor(patch).Patch();

        try
        {
            string fire = (string)formatter.Invoke(null, new object[] { WandElement.Fire, 3, true });

            Check(fire.Contains("Hit damage: <color=orange>19</color>") && fire.Contains("<color=orange>6</color> every <color=orange>1s</color> for <color=orange>5s</color>"), "Fire text distinguishes quality-scaled hit damage from its configured burn ticks.");
            player.m_customData["HexBattleMage.Attunements.Learned.Blight"] = "1";
            player.m_customData["HexBattleMage.Attunements.Active"] = (1 << (int)WandElement.Blight).ToString();
            const string native = "Description\n$item_blunt: 5\n$item_pierce: 5\n$item_knockback: 100\n$item_durability: 200";
            object[] arguments = { wand, 1, native };

            postfix.Invoke(null, arguments);
            string tooltip = (string)arguments[2];

            Check(tooltip.Contains("<color=orange>60</color> over <color=orange>3s</color> (<color=orange>20/s</color>)"), "Wand tooltips display Blight's full damage budget, duration and per-second rate.");
            Check(tooltip.IndexOf("$item_pierce", StringComparison.Ordinal) < tooltip.IndexOf("Damage over time", StringComparison.Ordinal) && tooltip.IndexOf("Damage over time", StringComparison.Ordinal) < tooltip.IndexOf("$item_knockback", StringComparison.Ordinal), "Element stats sit alongside weapon damage, before secondary stats.");
            total.Value = 120f;
            duration.Value = 6f;
            string tuned = (string)formatter.Invoke(null, new object[] { WandElement.Blight, 4, false });

            Check(tuned.Contains("<color=orange>120</color> over <color=orange>6s</color> (<color=orange>20/s</color>)"), "Element text reads current tuned values rather than caching damage or inventing Blight quality scaling.");
            arguments = new object[] { wand, 4, native };
            postfix.Invoke(null, arguments);
            Check(((string)arguments[2]).Contains("<color=orange>120</color>"), "Wand tooltip uses the same live effect formatter as attunement details.");
            player.m_customData["HexBattleMage.Attunements.Active"] = "0";
            arguments = new object[] { wand, 1, native };
            postfix.Invoke(null, arguments);
            Check((string)arguments[2] == native, "An unattuned wand does not advertise inactive elemental damage.");
        }
        finally
        {
            total.Value = originalTotal;
            duration.Value = originalDuration;
            player.m_customData = saved;
        }
    }

    private static void ValidateUi(Player player, GameObject root, Harmony harmony, bool narrow)
    {
        var previousPlayer = Player.m_localPlayer;
        var settings = ScriptableObject.CreateInstance<TMP_Settings>();

        AccessTools.Field(typeof(TMP_Settings), "s_Instance").SetValue(null, settings);
        var font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Validation/Arial.ttf"));

        AccessTools.Field(typeof(TMP_Settings), "m_defaultFontAsset").SetValue(settings, font);
        AccessTools.Field(typeof(TMP_Settings), "m_defaultFontSize").SetValue(settings, 18f);
        AccessTools.Field(typeof(TMP_Settings), "m_fallbackFontAssets").SetValue(settings, new List<TMP_FontAsset>());
        var canvasObject = new GameObject("AttunementCanvas", typeof(RectTransform), typeof(Canvas));
        var cameraObject = new GameObject("AttunementCamera", typeof(Camera));
        var target = new RenderTexture(narrow ? 380 : 720, 780, 24);

        try
        {
            Player.m_localPlayer = player;
            var camera = cameraObject.GetComponent<Camera>();
            var canvas = canvasObject.GetComponent<Canvas>();

            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var crafting = new GameObject("Crafting", typeof(RectTransform));

            crafting.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)crafting.transform;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var guiObject = new GameObject("Attunement GUI fixture");

            guiObject.SetActive(false);
            guiObject.transform.SetParent(root.transform, false);
            var gui = guiObject.AddComponent<InventoryGui>();

            gui.m_crafting = rect;
            gui.m_tabCraft = Tab(rect, 10f, font);
            gui.m_tabCraft.interactable = false;
            gui.m_tabUpgrade = Tab(rect, 100f, font);
            var template = new GameObject("Description", typeof(RectTransform));

            template.SetActive(false);
            gui.m_recipeDecription = template.AddComponent<TextMeshProUGUI>();
            gui.m_recipeDecription.font = font;
            var tabType = Mod.GetType("HexBattleMage.Core.UI.WandAttunementTab");
            var component = gui.gameObject.AddComponent(tabType);

            tabType.GetMethod("Initialize", Members).Invoke(component, new object[] { gui });
            Canvas.ForceUpdateCanvases();
            tabType.GetMethod("Open", Members).Invoke(component, null);
            Canvas.ForceUpdateCanvases();
            var panel = (GameObject)tabType.GetField("_panel", Members).GetValue(component);
            var content = (RectTransform)tabType.GetField("_details", Members).GetValue(component);
            var list = (RectTransform)tabType.GetField("_list", Members).GetValue(component);
            var action = (Button)tabType.GetField("_action", Members).GetValue(component);
            var summary = (TMP_Text)tabType.GetField("_summary", Members).GetValue(component);

            Check(panel.activeSelf && summary.text.Contains("ACTIVE ON WAND") && summary.text.Contains("1 / 1"), "The station window prominently shows the character's active limit.");
            Check(action.GetComponentInChildren<TMP_Text>().text == "Currently active" && !action.interactable, "The active element has a distinct disabled action.");
            Check(!panel.GetComponentsInChildren<TMP_Text>().Any(label => label.text.Contains("Viewing")) && panel.GetComponentsInChildren<TMP_Dropdown>().Length == 0, "There is no wand selector or per-item quality UI.");

            foreach (var label in content.GetComponentsInChildren<TMP_Text>())
            {
                label.ForceMeshUpdate();
                Check(!label.isTextOverflowing, "Selected element text wraps within the detail column.");
            }

            string activeDetails = string.Join(" ", content.GetComponentsInChildren<TMP_Text>().Select(label => label.text));

            Check(activeDetails.Contains("materials already paid") && !activeDetails.Contains("Unlock requirements"), "Learned attunements show paid status without repeating their learning checklist.");
            var blightRow = list.GetComponentsInChildren<Button>().First(button => button.gameObject.name == "Element_Blight");

            Check(blightRow.IsInteractable(), "Unlearned attunements remain selectable when the native Craft tab template is disabled.");
            blightRow.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(null) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left });
            var activeRow = list.GetComponentsInChildren<Button>().First(button => button.gameObject.name == "Element_Fire");

            Check(activeRow.transform.Find("ActiveBorder") != null && blightRow.transform.Find("ActiveBorder") == null, "Only the equipped attunement has the thick green frame.");
            var selectedRow = list.GetComponentsInChildren<Button>().First(button => button.gameObject.name == "Element_Blight");

            Check(selectedRow.transform.Find("InspectionBorder") != null && activeRow.transform.Find("InspectionBorder") == null, "Gold inspection state is independent from the equipped green frame.");
            Check(!action.interactable && action.GetComponentInChildren<TMP_Text>().text.Contains("Missing requirements"), "Missing requirements disable learning and show validation text.");
            Check(action.colors.disabledColor.r > action.colors.disabledColor.g, "Missing requirements use a warm warning button surface.");
            string blightDetails = string.Join(" ", content.GetComponentsInChildren<TMP_Text>().Select(label => label.text));

            Check(blightDetails.Contains("<color=orange>60</color> over <color=orange>3s</color> (<color=orange>20/s</color>)"), "The attunement window shows the same Blight damage as the wand tooltip.");
            var elementManager = Mod.GetType("HexBattleMage.Core.Elements.WandElementManager");
            var blightSettings = elementManager.GetMethod("GetSettings", Members).Invoke(null, new object[] { WandElement.Blight });
            var blightTotal = (ConfigEntry<float>)blightSettings.GetType().GetProperty("TotalDamage", Members).GetValue(blightSettings);
            float originalBlightTotal = blightTotal.Value;

            blightTotal.Value = 90f;
            tabType.GetMethod("Refresh", Members).Invoke(component, new object[] { player });
            Check(string.Join(" ", content.GetComponentsInChildren<TMP_Text>().Select(label => label.text)).Contains("<color=orange>90</color>"), "Changing effect damage invalidates the open attunement window's cached details.");
            blightTotal.Value = originalBlightTotal;
            tabType.GetMethod("Refresh", Members).Invoke(component, new object[] { player });

            Check(blightDetails.Contains("Rune Block level 2") && blightDetails.Contains("Defeat") && blightDetails.IndexOf("trophyskeleton", StringComparison.OrdinalIgnoreCase) >= 0 && blightDetails.IndexOf("surtlingcore", StringComparison.OrdinalIgnoreCase) >= 0 && blightDetails.IndexOf("mushroomyellow", StringComparison.OrdinalIgnoreCase) >= 0, "Blight lists its boss, station and all material requirements before learning.");
            foreach (string preview in new[] { "Spirit", "Frost", "Earth" })
            {
                var row = list.GetComponentsInChildren<Button>().First(button => button.gameObject.name == "Element_" + preview);

                Check(row.IsInteractable(), "Every future attunement can be inspected.");
                row.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(null) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left });
                Check(!action.interactable && action.GetComponentInChildren<TMP_Text>().text == "Not yet available", "Preview entries cannot grant effects or consume materials.");
                Check(summary.text.Contains("Fire"), "Inspecting a future attunement leaves Fire equipped.");
            }

            list.GetComponentsInChildren<Button>().First(button => button.gameObject.name == "Element_Fire").onClick.Invoke();
            int children = content.childCount;

            tabType.GetMethod("Refresh", Members).Invoke(component, new object[] { player });
            Check(content.childCount == children, "Unchanged state does not recreate selection or action controls.");
            Canvas.ForceUpdateCanvases();
            var runeProfile = (HexBattleMageRuneBlockProfile)Mod.GetType("HexBattleMage.Core.Elements.WandAttunements").GetProperty("Profile", Members).GetValue(null);
            var authored = runeProfile.Attunements;
            var spiritObject = new GameObject("UI fixture Spirit");

            spiritObject.transform.SetParent(root.transform, false);
            var spirit = spiritObject.AddComponent<HexBattleMageWandAttunementProfile>();
            var spiritRecipe = spiritObject.AddComponent<MageRecipeProfile>();

            spirit.Element = WandElement.Spirit;
            spirit.DisplayName = "$hexbattlemage_element_spirit";
            spirit.Description = "$hexbattlemage_attunement_spirit_description";
            spiritRecipe.CraftingStation = HexBattleMageRuneBlockProfile.PrefabName;
            spiritRecipe.Resources = new MageRecipeResource[0];
            runeProfile.Attunements = new[] { authored[0], spiritObject };
            tabType.GetMethod("Refresh", Members).Invoke(component, new object[] { player });
            var spiritRow = list.GetComponentsInChildren<Button>().First(button => button.gameObject.name == "Element_Spirit");

            var learnedKey = "HexBattleMage.Attunements.Learned.Spirit";
            bool wasLearned = player.m_customData.ContainsKey(learnedKey);

            player.m_customData.Remove(learnedKey);
            spiritRow.onClick.Invoke();
            Check(action.interactable && action.GetComponentInChildren<TMP_Text>().text == "Learn Spirit", "Meeting learning requirements enables the named Learn action.");
            var validation = (TMP_Text)tabType.GetField("_validation", Members).GetValue(component);

            Check(validation.text == "All requirements met", "The enabled learning button confirms successful validation.");

            if (wasLearned)
            {
                player.m_customData[learnedKey] = "1";
            }

            tabType.GetMethod("Refresh", Members).Invoke(component, new object[] { player });
            Check(summary.text.Contains("1 / 1") && summary.text.Contains("Fire"), "Inspecting another element does not change the active Fire summary.");
            Check(action.GetComponentInChildren<TMP_Text>().text == "Activate Spirit" && action.interactable, "At capacity, the action explicitly names the replacement.");
            Canvas.ForceUpdateCanvases();

            foreach (var bar in panel.GetComponentsInChildren<Scrollbar>(true))
            {
                Check(bar.handleRect.rect.width <= 12f, "Scrollbar handles stay inside their narrow rail.");
            }

            foreach (var label in content.GetComponentsInChildren<TMP_Text>())
            {
                label.ForceMeshUpdate();
                Check(!label.isTextOverflowing, "Replacement information fits the detail column at both window widths.");
            }

            foreach (var scroll in panel.GetComponentsInChildren<ScrollRect>())
            {
                typeof(ScrollRect).GetMethod("LateUpdate", Members).Invoke(scroll, null);
            }

            camera.Render();
            runeProfile.Attunements = authored;
            tabType.GetField("_contentKey", Members).SetValue(component, null);
            tabType.GetField("_previewInspected", Members).SetValue(component, null);
            tabType.GetField("_inspected", Members).SetValue(component, (WandElement?)WandElement.Blight);
            tabType.GetMethod("Refresh", Members).Invoke(component, new object[] { player });
            Canvas.ForceUpdateCanvases();
            camera.Render();

            foreach (var footer in new[] { (TMP_Text)tabType.GetField("_validation", Members).GetValue(component), (TMP_Text)tabType.GetField("_actionNote", Members).GetValue(component) })
            {
                footer.ForceMeshUpdate();
                Check(!footer.isTextOverflowing, "Validation and action guidance fit at both crafting window widths.");
            }

            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);

            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath)), narrow ? "character-attunement-narrow.png" : "character-attunement-wide.png"), texture.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(template);
        }
        finally
        {
            Player.m_localPlayer = previousPlayer;
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(target);
        }
    }

    private static Button Tab(RectTransform parent, float x, TMP_FontAsset font)
    {
        var root = new GameObject("Tab", typeof(RectTransform), typeof(Image), typeof(Button));

        root.GetComponent<Image>().color = new Color(0.32f, 0.22f, 0.12f);
        root.transform.SetParent(parent, false);
        var rect = (RectTransform)root.transform;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -30f);
        rect.sizeDelta = new Vector2(84f, 36f);
        var label = new GameObject("Label", typeof(RectTransform));

        label.SetActive(false);
        label.transform.SetParent(root.transform, false);
        var text = label.AddComponent<TextMeshProUGUI>();

        text.font = font;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Center;
        var labelRect = (RectTransform)label.transform;

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.SetActive(true);
        return root.GetComponent<Button>();
    }

    private static Tuple<Recipe, Recipe> Register(GameObject wand, GameObject root, Harmony harmony)
    {
        foreach (var method in typeof(Jotunn.Utils.BepInExUtils).GetMethods(Members).Where(method => method.Name == "GetSourceModMetadata"))
        {
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(StarterWandProgressionValidation), nameof(SourceMod)));
        }

        var prefabs = (PrefabManager)FormatterServices.GetUninitializedObject(typeof(PrefabManager));
        var items = (ItemManager)FormatterServices.GetUninitializedObject(typeof(ItemManager));
        var assets = (AssetManager)FormatterServices.GetUninitializedObject(typeof(AssetManager));

        Set(typeof(PrefabManager), null, "_instance", prefabs);
        Set(typeof(PrefabManager), prefabs, "<PrefabContainer>k__BackingField", root);
        Set(typeof(PrefabManager), prefabs, "<Prefabs>k__BackingField", new Dictionary<string, CustomPrefab>());
        Set(typeof(ItemManager), null, "_instance", items);
        Set(typeof(ItemManager), items, "Items", new Dictionary<string, CustomItem>());
        Set(typeof(ItemManager), items, "Recipes", new HashSet<CustomRecipe>());
        Set(typeof(ItemManager), items, "StatusEffects", new HashSet<CustomStatusEffect>());
        Set(typeof(AssetManager), null, "instance", assets);
        harmony.Patch(AccessTools.Method(typeof(AssetManager), "AddAsset", new[] { typeof(Object), typeof(Object) }), prefix: new HarmonyMethod(typeof(StarterWandProgressionValidation), nameof(Skip)));
        var mockType = typeof(PrefabManager).Assembly.GetType("Jotunn.Managers.MockManager", true);
        var mocks = FormatterServices.GetUninitializedObject(mockType);

        Set(mockType, null, "_instance", mocks);
        Set(mockType, mocks, "MockPrefabContainer", root);
        Set(mockType, mocks, "mockedPrefabs", new Dictionary<string, GameObject>());
        var settings = Mod.GetType("HexBattleMage.Core.Managers.PrefabSettings");
        var requirements = settings.GetMethod("GetRequirements", Members).Invoke(null, new object[] { wand.GetComponent<MageRecipeProfile>() });
        bool result = (bool)Mod.GetType("HexBattleMage.Core.Managers.ItemRegistrationManager").GetMethod("RegisterEquipment", Members).Invoke(null, new object[] { wand, Heightmap.Biome.Meadows, requirements });

        Check(result, "Actual Jotunn item/upgrade recipe registration succeeds.");
        var registered = (Dictionary<string, CustomItem>)AccessTools.Field(typeof(ItemManager), "Items").GetValue(items);
        var recipes = (HashSet<CustomRecipe>)AccessTools.Field(typeof(ItemManager), "Recipes").GetValue(items);

        return Tuple.Create(registered[wand.name].Recipe.Recipe, recipes.Single(recipe => recipe.Recipe.name.EndsWith("_Upgrade", StringComparison.Ordinal)).Recipe);
    }

    private static void Set(Type type, object instance, string name, object value)
    {
        AccessTools.Field(type, name).SetValue(instance, value);
    }

    private static bool SourceMod(ref BepInPlugin __result)
    {
        __result = Metadata;
        return false;
    }

    private static void RestoreDropReference(ItemDrop __instance)
    {
        if (ObjectDB.instance != null)
        {
            __instance.m_itemData.m_dropPrefab = ObjectDB.instance.GetItemPrefab(__instance.gameObject.name.Replace("(Clone)", string.Empty));
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





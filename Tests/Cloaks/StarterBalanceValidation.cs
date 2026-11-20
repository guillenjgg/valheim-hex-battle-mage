using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

public static class StarterBalanceValidation
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Assembly Mod = typeof(MageEitrProfile).Assembly;

    private static bool _owner = true;

    public static void Run()
    {
        _owner = true;
        var harmony = new Harmony("HexBattleMage.StarterBalanceValidation");
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE"));
        var root = new GameObject("Starter balance fixture");
        var previousPlayer = Player.m_localPlayer;

        root.SetActive(false);

        try
        {
            PlayerTextLocalizationValidation.InitializeForTests(harmony);
            ValidateWand(bundle, root);
            ValidateRecipe(bundle);
            ValidateVitals(bundle, root, harmony);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "starter-balance-report.txt"), "PASS: baked four-resource trinket recipe; native wand damage at qualities 1-4 is 5/5/15, 6/6/17, 7/7/19, 8/8/21; physical quality tuning survives element changes; native food totals, equip/unequip, replacement and clamping verify +6 flat armor health and +5 cloak stamina, no quality scaling, no refill exploit, food stacking and retained Eitr. World/network/visual bootstrap are fixtures.");
        }
        finally
        {
            Player.m_localPlayer = previousPlayer;
            Object.DestroyImmediate(root);
            bundle.Unload(true);
            harmony.UnpatchSelf();
        }
    }

    private static void ValidateWand(AssetBundle bundle, GameObject root)
    {
        var wand = bundle.LoadAsset<GameObject>("HexBattleMage_Wand_Meadows_01");
        var helper = Mod.GetType("HexBattleMage.Assets.AssetBundleHelper");

        helper.GetProperty("HexBattleMageWand", Members).SetValue(null, wand);
        helper.GetProperty("Bundle", Members).SetValue(null, bundle);
        string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "StarterBalance");

        Directory.CreateDirectory(directory);
        typeof(Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(directory, "core.cfg"));
        var config = new ConfigFile(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".cfg"), false) { SaveOnConfigSet = false };
        var dev = Mod.GetType("HexBattleMage.Core.DevConfig");

        Bind(dev, config, "WandBluntDamage", 5f);
        Bind(dev, config, "WandPierceDamage", 5f);
        var bluntQuality = Bind(dev, config, "WandBluntDamagePerQuality", 1f);
        var pierceQuality = Bind(dev, config, "WandPierceDamagePerQuality", 1f);
        var elements = Mod.GetType("HexBattleMage.Core.Elements.WandElementManager");

        elements.GetMethod("Configure", Members).Invoke(null, new object[] { config });
        var apply = Mod.GetType("HexBattleMage.Core.Managers.ItemRegistrationManager").GetMethod("ApplyDamageSettings", Members);
        var item = wand.GetComponent<ItemDrop>().m_itemData;

        item.m_dropPrefab = wand;
        var playerObject = new GameObject("Wand balance player");

        playerObject.SetActive(false);
        playerObject.transform.SetParent(root.transform, false);
        var player = playerObject.AddComponent<TrinketValidationPlayer>();

        player.m_customData = new Dictionary<string, string>
        {
            { "HexBattleMage.Attunements.Learned.Fire", "1" },
            { "HexBattleMage.Attunements.Active", "1" }
        };
        Player.m_localPlayer = player;
        Check(item.m_shared.m_maxQuality == 4, "The first wand permits three upgrades.");
        apply.Invoke(null, new object[] { item });

        for (int quality = 1; quality <= 4; quality++)
        {
            item.m_quality = quality;
            var damage = item.GetDamage();

            Check(damage.m_blunt == 4f + quality && damage.m_pierce == 4f + quality && damage.m_fire == 13f + 2f * quality, "Native quality scaling matches the requested wand progression.");
        }

        bluntQuality.Value = 2f;
        pierceQuality.Value = 3f;
        apply.Invoke(null, new object[] { item });
        ((ConfigEntry<bool>)elements.GetProperty("BypassAttunements", Members).GetValue(null)).Value = true;
        ((ConfigEntry<WandElement>)elements.GetProperty("ActiveElement", Members).GetValue(null)).Value = WandElement.Frost;
        elements.GetMethod("ApplyToItem", Members).Invoke(null, new object[] { item });
        Check(item.m_shared.m_damagesPerLevel.m_blunt == 2f && item.m_shared.m_damagesPerLevel.m_pierce == 3f, "Attunement preserves tuned physical upgrades.");
        bluntQuality.Value = 1f;
        pierceQuality.Value = 1f;
        ((ConfigEntry<WandElement>)elements.GetProperty("ActiveElement", Members).GetValue(null)).Value = WandElement.Fire;
        apply.Invoke(null, new object[] { item });
        item.m_quality = 1;
    }

    private static ConfigEntry<float> Bind(Type dev, ConfigFile config, string name, float value)
    {
        var entry = config.Bind("Physical", name, value);

        dev.GetProperty(name, Members).SetValue(null, entry);
        return entry;
    }

    private static void ValidateRecipe(AssetBundle bundle)
    {
        var source = bundle.LoadAsset<GameObject>("HexBattleMage_Trinket_Meadows_01");
        var profile = source.GetComponent<MageRecipeProfile>();

        profile.Resources = new MageRecipeResource[0];
        var resources = profile.GetResources().ToDictionary(resource => resource.ItemPrefab, resource => resource.Amount);

        Check(resources.Count == 4 && resources["TrophyBoar"] == 1 && resources["Stone"] == 10 && resources["BoneFragments"] == 5 && resources["LeatherScraps"] == 2, "Managed baked recipe contains the exact four requested materials.");
    }

    private static void ValidateVitals(AssetBundle bundle, GameObject root, Harmony harmony)
    {
        foreach (string name in new[] { "GearVitalityFoodValuesPatch", "GearVitalityEquipPatch", "GearVitalityUnequipPatch", "MageGearMaximumEitrPatch", "CloakEitrEquipPatch", "CloakEitrUnequipPatch" })
        {
            harmony.CreateClassProcessor(Mod.GetType("HexBattleMage.Patches." + name)).Patch();
        }

        foreach (string name in new[] { "SetupEquipment", "TriggerEquipEffect" })
        {
            harmony.Patch(AccessTools.Method(typeof(Humanoid), name), prefix: new HarmonyMethod(typeof(StarterBalanceValidation), nameof(Skip)));
        }

        harmony.Patch(AccessTools.Method(typeof(Character), "GetZDOID"), prefix: new HarmonyMethod(typeof(StarterBalanceValidation), nameof(Identity)));
        harmony.Patch(AccessTools.Method(typeof(ZNetView), "OnDestroy"), prefix: new HarmonyMethod(typeof(StarterBalanceValidation), nameof(Skip)));
        harmony.Patch(AccessTools.Method(typeof(ZDO), "IncreaseDataRevision"), prefix: new HarmonyMethod(typeof(StarterBalanceValidation), nameof(Skip)));
        harmony.Patch(AccessTools.Method(typeof(ZNetView), "IsValid"), prefix: new HarmonyMethod(typeof(StarterBalanceValidation), nameof(Valid)));
        harmony.Patch(AccessTools.Method(typeof(ZNetView), "IsOwner"), prefix: new HarmonyMethod(typeof(StarterBalanceValidation), nameof(Owner)));
        var player = root.AddComponent<TrinketValidationPlayer>();
        var view = root.AddComponent<ZNetView>();
        var data = new ZDO();

        AccessTools.Field(typeof(ZDO), "m_uid").SetValue(data, new ZDOID(987654321L, 5678U));
        AccessTools.Field(typeof(ZNetView), "m_zdo").SetValue(view, data);
        AccessTools.Field(typeof(Character), "m_nview").SetValue(player, view);
        var effects = new SEMan(player, view);

        AccessTools.Field(typeof(Character), "m_seman").SetValue(player, effects);
        AccessTools.Field(typeof(SEMan), "m_statusEffectAttributesOld").SetValue(effects, 0);
        Player.m_localPlayer = player;
        AccessTools.Field(typeof(Player), "m_baseHP").SetValue(player, 25f);
        AccessTools.Field(typeof(Player), "m_baseStamina").SetValue(player, 50f);
        player.SetMaxHealth(25f, false);
        player.SetMaxStamina(50f, false);
        player.SetHealth(20f);
        AccessTools.Field(typeof(Player), "m_stamina").SetValue(player, 40f);
        var items = new List<ItemDrop.ItemData>();

        foreach (string category in new[] { "Helmet", "Chest", "Legs", "Cloak" })
        {
            string identity = "HexBattleMage_" + category + "_Meadows_01";
            var source = bundle.LoadAsset<GameObject>(category == "Cloak" ? identity : identity + "_Definition");
            var type = category == "Helmet" ? ItemDrop.ItemData.ItemType.Helmet : category == "Chest" ? ItemDrop.ItemData.ItemType.Chest : category == "Legs" ? ItemDrop.ItemData.ItemType.Legs : ItemDrop.ItemData.ItemType.Shoulder;
            var item = GearItem(source, root, type);

            player.GetInventory().GetAllItems().Add(item);
            items.Add(item);
        }

        Check(player.GetMaxHealth() == 25f && player.GetMaxStamina() == 50f, "Inventory-only gear grants no vitality.");

        foreach (var item in items)
        {
            Check(player.EquipItem(item, false), "Native equipment accepts the gear.");
        }


        Check(player.GetMaxHealth() == 31f && player.GetMaxStamina() == 55f, "Equipped armor grants +6 health and cloak grants +5 stamina immediately.");
        Check(player.GetHealth() == 20f && player.GetStamina() == 40f, "Equipping adds capacity without refilling resources.");
        Check(player.GetMaxEitr() == 20f, "Existing equipped Eitr remains additive.");

        foreach (var item in items)
        {
            item.m_quality = 4;
        }

        Refresh(player);
        Check(player.GetMaxHealth() == 31f && player.GetMaxStamina() == 55f, "Vitality bonuses do not increase with quality.");
        _owner = false;
        items[0].m_dropPrefab.GetComponent<HexBattleMageVitalityProfile>().MaximumHealth = 100f;
        Refresh(player);
        Check(player.GetMaxHealth() == 31f, "A non-owner cannot rewrite replicated health caps.");
        items[0].m_dropPrefab.GetComponent<HexBattleMageVitalityProfile>().MaximumHealth = 2f;
        _owner = true;
        ValidatePresentation(player, items[0], items[3]);
        var foods = (List<Player.Food>)AccessTools.Field(typeof(Player), "m_foods").GetValue(player);

        foods.Add(new Player.Food { m_health = 10f, m_stamina = 20f, m_eitr = 0f });
        Refresh(player);
        Check(player.GetMaxHealth() == 41f && player.GetMaxStamina() == 75f, "Flat gear bonuses stack with native food values.");
        foods.Clear();
        Refresh(player);
        player.SetHealth(31f);
        AccessTools.Field(typeof(Player), "m_stamina").SetValue(player, 55f);
        var helmet = GearItem(bundle.LoadAsset<GameObject>("HexBattleMage_Helmet_Meadows_01_Definition"), root, ItemDrop.ItemData.ItemType.Helmet);
        var cloak = GearItem(bundle.LoadAsset<GameObject>("HexBattleMage_Cloak_Meadows_01"), root, ItemDrop.ItemData.ItemType.Shoulder);

        player.GetInventory().GetAllItems().Add(helmet);
        player.GetInventory().GetAllItems().Add(cloak);
        Check(player.EquipItem(helmet, false), "Native helmet replacement succeeds.");
        Check(player.GetHealth() == 31f, "Replacing a vitality item preserves existing health.");
        Check(player.EquipItem(cloak, false), "Native cloak replacement succeeds.");
        Check(player.GetStamina() == 55f, "Replacing the cloak preserves existing stamina.");

        foreach (var item in player.GetInventory().GetAllItems().ToArray())
        {
            player.UnequipItem(item, false);
        }

        Check(player.GetMaxHealth() == 25f && player.GetHealth() == 25f && player.GetMaxStamina() == 50f && player.GetStamina() == 50f, "Unequip removes bonuses and clamps resources to native caps.");
        Check(player.EquipItem(helmet, false) && player.EquipItem(cloak, false), "Re-equipping succeeds.");
        Check(player.GetHealth() == 25f && player.GetStamina() == 50f, "Re-equipping cannot repeatedly refill health or stamina.");
    }

    private static void ValidatePresentation(Player player, ItemDrop.ItemData armor, ItemDrop.ItemData cloak)
    {
        var tooltip = Mod.GetType("HexBattleMage.Patches.MageGearTooltipPatch").GetMethod("Postfix", Members);

        foreach (var item in new[] { armor, cloak })
        {
            var arguments = new object[] { item, 4, "$item_armor: 2\nDurability: 100" };

            tooltip.Invoke(null, arguments);
            string text = Regex.Replace((string)arguments[2], "<[^>]*>", string.Empty);
            string bonus = item == armor ? "Maximum health: +2" : "Maximum stamina: +5";

            Check(text.Contains(bonus), "Upgraded preview shows the same flat bonus.");
            Check(text.IndexOf(bonus, StringComparison.Ordinal) < text.IndexOf("Durability", StringComparison.Ordinal), "Vitality perks appear before durability.");
        }

        string summary = (string)Mod.GetType("HexBattleMage.Core.MageGearStats").GetMethod("GetText", Members).Invoke(null, null);

        summary = Regex.Replace(summary, "<[^>]*>", string.Empty);
        Check(summary.Contains("Maximum health: +6") && summary.Contains("Maximum stamina: +5"), "Compendium totals include equipped vitality bonuses.");
    }
    private static ItemDrop.ItemData GearItem(GameObject source, GameObject root, ItemDrop.ItemData.ItemType type)
    {
        var target = new GameObject(source.name);

        target.transform.SetParent(root.transform, false);
        Mod.GetType("HexBattleMage.Core.GearVitals").GetMethod("CopyProfile", Members).Invoke(null, new object[] { source, target });
        var vitality = target.GetComponent<HexBattleMageVitalityProfile>();

        Check(type == ItemDrop.ItemData.ItemType.Shoulder ? vitality.MaximumStamina == 5f && vitality.MaximumHealth == 0f : vitality.MaximumHealth == 2f && vitality.MaximumStamina == 0f, "Exact vitality defaults are baked and copied from the prefab.");
        var authoredEitr = source.GetComponent<MageEitrProfile>();
        var eitr = target.AddComponent<MageEitrProfile>();

        eitr.MaximumEitr = authoredEitr.MaximumEitr;
        eitr.MaximumEitrPerQuality = authoredEitr.MaximumEitrPerQuality;
        eitr.RegenerationModifier = authoredEitr.RegenerationModifier;

        if (type == ItemDrop.ItemData.ItemType.Shoulder)
        {
            target.AddComponent<HexBattleMageCloakProfile>();
        }
        else
        {
            target.AddComponent<HexBattleMageArmorProfile>();
        }

        var item = new ItemDrop.ItemData { m_dropPrefab = target, m_quality = 1, m_shared = new ItemDrop.ItemData.SharedData { m_name = source.name, m_itemType = type, m_maxStackSize = 1, m_maxQuality = 4 } };

        target.AddComponent<ItemDrop>().m_itemData = item;
        return item;
    }

    private static void Refresh(Player player)
    {
        Mod.GetType("HexBattleMage.Core.GearVitals").GetMethod("Refresh", Members).Invoke(null, new object[] { player, player.GetHealth(), player.GetStamina() });
    }

    private static bool Skip() => false;

    private static bool Valid(ref bool __result)
    {
        __result = true;
        return false;
    }

    private static bool Owner(ref bool __result)
    {
        __result = _owner;
        return false;
    }

    private static bool Identity(ref ZDOID __result)
    {
        __result = default(ZDOID);
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

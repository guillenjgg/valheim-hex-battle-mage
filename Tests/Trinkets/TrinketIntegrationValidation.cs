using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Trinkets;
using UnityEngine;
using Object = UnityEngine.Object;
public static class TrinketIntegrationValidation
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static readonly Assembly Mod = typeof(MageEitrProfile).Assembly;
    private static readonly Type Manager = Mod.GetType("HexBattleMage.Core.Managers.TrinketManager");
    private static StatusEffect _effect;
    private static readonly Dictionary<int, StatusEffect> RewardTemplates = new Dictionary<int, StatusEffect>();
    private static int _checks;
    private static readonly List<GameObject> Roots = new List<GameObject>();
    private static object Call(string name, params object[] args) { var method = Manager.GetMethod(name, Flags) ?? Mod.GetType("HexBattleMage.Core.Trinkets.TrinketEffects").GetMethod(name, Flags); return method.Invoke(null, args); }
    private static bool Skip() { return false; }
    private static bool NetworkEffect(SEMan __instance, int nameHash, bool resetTime, ref StatusEffect __result)
    {
        __result = RewardTemplates.TryGetValue(nameHash, out var template) ? __instance.AddStatusEffect(template, resetTime) : null;
        return false;
    }
    private static bool Stagger(Character __instance, ref bool __result)
    {
        __result = __instance is TrinketValidationPlayer player && player.Overwhelmed;
        return false;
    }
    private static bool Identity(ref ZDOID __result) { __result = default(ZDOID); return false; }
    private static GameObject Root(string name) { var root = new GameObject(name); root.SetActive(false); Roots.Add(root); return root; }
    private static void Check(bool value, string name) { if (!value) { throw new Exception(name); } _checks++; }
    private static void Equal(float actual, float expected, string name) { Check(Mathf.Abs(actual - expected) < 0.001f, name + ": " + actual + " expected " + expected); }
    private static void Patch(Harmony harmony, Type type, string method, string prefix, Type[] args = null)
    {
        harmony.Patch(AccessTools.Method(type, method, args), prefix: new HarmonyMethod(typeof(TrinketIntegrationValidation).GetMethod(prefix, Flags)));
    }
    private static void Configure(ConfigFile config, string field, float value)
    {
        Manager.GetField(field, Flags).SetValue(null, config.Bind("Validation", field, value));
    }
    public static void Run()
    {
        var harmony = new Harmony("HexBattleMage.TrinketIntegrationValidation");
        var bundle = AssetBundle.LoadFromFile("C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/hexbattlemage");
        try
        {
            typeof(BepInEx.Paths).GetProperty("BepInExConfigPath").SetValue(null, "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/build/test-core.cfg");
            var config = new ConfigFile("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/build/trinket-test.cfg", false);
            config.SaveOnConfigSet = false;
            PlayerTextLocalizationValidation.InitializeForTests(harmony);
            var visual = bundle.LoadAsset<GameObject>("HexBattleMage_Trinket_Meadows_01");
            var profile = visual.GetComponent<HexBattleMageTrinketProfile>();
            var eitr = visual.GetComponent<MageEitrProfile>();
            Check(profile != null && profile.Icon != null && eitr != null, "Bundled data profiles and icon load");
            visual.GetComponent<MageRecipeProfile>().ApplyBakedDefaults();
            Check(visual.GetComponent<MageRecipeProfile>().Resources.Length == 4, "Baked recipe loads");
            Patch(harmony, typeof(ItemDrop), "OnValidate", "Skip"); Call("Configure", bundle.LoadAllAssets<GameObject>(), config);
            _effect = (StatusEffect)ScriptableObject.CreateInstance(Mod.GetType("HexBattleMage.Core.Trinkets.TrinketRewardStatus"));
            _effect.name = "HexBattleMage_Trinket_Meadows_01ParryEmpowerment";
            _effect.m_ttl = profile.ParryDuration;
            _effect.m_effectsOnlyOnPlayer = true;
            RewardTemplates.Add(_effect.NameHash(), _effect);
            var stoneSettings = Call("GetSettings", profile); Check((int)stoneSettings.GetType().GetProperty("RewardHash", Flags).GetValue(stoneSettings) == _effect.NameHash(), "Progression-based reward identity resolves");
            Patch(harmony, typeof(ItemDrop), "OnValidate", "Skip");
            var assets = Mod.GetType("HexBattleMage.Assets.AssetBundleHelper");
            assets.GetField("<HexBattleMageWand>k__BackingField", Flags).SetValue(null, bundle.LoadAsset<GameObject>("HexBattleMage_Wand_Meadows_01"));
            assets.GetField("<Bundle>k__BackingField", Flags).SetValue(null, bundle);
            var coldPrefab = Object.Instantiate(visual);
            try
            {
                coldPrefab.name = "HexBattleMage_Trinket_Meadows_01";
                var coldRecipe = coldPrefab.GetComponent<MageRecipeProfile>();
                coldRecipe.Resources = new MageRecipeResource[0];
                coldRecipe.BakedRecipe = null;
                AccessTools.Field(typeof(MageRecipeProfile), "_defaultsApplied").SetValue(coldRecipe, false);
                var requirements = (Array)Mod.GetType("HexBattleMage.Core.Managers.PrefabSettings").GetMethod("GetRequirements", Flags).Invoke(null, new object[] { coldRecipe });
                if (requirements.Length != 4) { foreach (var assetName in bundle.GetAllAssetNames()) { if (assetName.Contains("Recipe_") || assetName.Contains("recipe_")) { Debug.Log(assetName); } } Debug.Log("Cold recipe root=" + coldPrefab.name + " text=" + coldRecipe.BakedRecipe + " count=" + requirements.Length); }
                Check(requirements.Length == 4, "Renamed recipe cold recovery finds new TextAsset identity");
                Check(coldRecipe.CraftingStation == "HexBattleMage_RuneBlock", "Renamed recipe retains Mage Table station");
            }
            finally { Object.DestroyImmediate(coldPrefab); }
            Check(Mod.GetType("HexBattleMage.Core.Managers.StoneTrinketProfile") == null, "Stone-specific wrapper removed");
            Check(visual.name == profile.ItemPrefabName && profile.ItemPrefabName == "HexBattleMage_Trinket_Meadows_01", "Authored and registered identities use the progression name");
            Check(profile.ConfigSection == string.Empty && profile.RewardStatusName == string.Empty, "Config and reward identities derive from progression name");
            foreach (string name in new[] { "MageGearMaximumEitrPatch", "MageGearEitrRegenerationPatch", "TrinketStaminaRegenerationPatch", "TrinketEquipPatch", "TrinketUnequipPatch", "TrinketDeathPatch", "TrinketParryPatch", "TrinketWandDamagePatch", "TrinketTooltipPatch" })
            {
                harmony.CreateClassProcessor(Mod.GetType("HexBattleMage.Patches." + name)).Patch();
            }
            Patch(harmony, typeof(SEMan), "AddStatusEffect", "NetworkEffect", new[] { typeof(int), typeof(bool), typeof(int), typeof(float), typeof(short) });
            Patch(harmony, typeof(Humanoid), "SetupEquipment", "Skip");
            Patch(harmony, typeof(Humanoid), "TriggerEquipEffect", "Skip");
            Patch(harmony, typeof(Character), "GetZDOID", "Identity");
            Patch(harmony, typeof(Character), "AddStaggerDamage", "Stagger");
            Patch(harmony, typeof(Player), "AddAdrenaline", "Skip");
            Patch(harmony, typeof(Player), "AddStamina", "Skip");
            Patch(harmony, typeof(Character), "Stagger", "Skip");
            Patch(harmony, typeof(Character), "Damage", "Skip");
            var damageText = Root("DamageText").AddComponent<DamageText>();
            typeof(DamageText).GetField("m_instance", Flags).SetValue(null, damageText);
            Patch(harmony, typeof(DamageText), "ShowText", "Skip", new[] { typeof(DamageText.TextType), typeof(Vector3), typeof(float), typeof(bool) });
            var player = Root("TrinketPlayer").AddComponent<TrinketValidationPlayer>();
            var seman = new SEMan(player, player.gameObject.AddComponent<ZNetView>());
            AccessTools.Field(typeof(Character), "m_seman").SetValue(player, seman);
            AccessTools.Field(typeof(SEMan), "m_statusEffectAttributesOld").SetValue(seman, 0);
            var itemPrefab = Object.Instantiate(visual);
            itemPrefab.SetActive(false);
            Roots.Add(itemPrefab);
            var item = new ItemDrop.ItemData { m_dropPrefab = itemPrefab, m_shared = new ItemDrop.ItemData.SharedData { m_name = profile.DisplayName, m_itemType = ItemDrop.ItemData.ItemType.Trinket, m_maxStackSize = 1 } };
            itemPrefab.AddComponent<ItemDrop>().m_itemData = item;
            player.GetInventory().GetAllItems().Add(item);
            Equal((float)Call("EitrBonus", player, false), 0f, "Carried trinket grants no Eitr");
            Check(player.EquipItem(item, false), "Native trinket equips");
            Check(ReferenceEquals(Call("GetEquipped", player), item), "Native trinket slot resolves equipped item");
            Equal(eitr.MaximumEitr, 0f, "Baked trinket adds no flat Eitr");
            Equal(profile.StaminaRegenerationModifier, 0.10f, "Stamina regeneration is baked");
            Equal(player.GetMaxEitr(), 0f, "Trinket does not create a pool without gear or food");
            float staminaMultiplier = 1f;

            seman.ModifyStaminaRegen(ref staminaMultiplier);
            Equal(staminaMultiplier, 1.10f, "Native stamina regeneration receives ten percent");
            string passiveTooltip = (string)Call("Tooltip", profile);

            Check(passiveTooltip.Contains("Stamina regeneration") && !passiveTooltip.Contains("Maximum Eitr"), "Tooltip lists regeneration, without a flat pool");
            Equal((float)typeof(Player).GetMethod("GetEquipmentEitrRegenModifier", Flags).Invoke(player, null), 0.10f, "Eitr regeneration modifier");
            Equal(player.GetEitr(), 0f, "Equipping does not refill Eitr");
            var second = item.Clone();
            player.GetInventory().GetAllItems().Add(second);
            Check(player.EquipItem(second, false), "Second native trinket equips");
            Check(!item.m_equipped && second.m_equipped, "Only one native trinket stays equipped");
            var shield = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData { m_itemType = ItemDrop.ItemData.ItemType.Shield, m_blockPower = 100f, m_timedBlockBonus = 1.5f, m_useDurability = false } };
            AccessTools.Field(typeof(Humanoid), "m_leftItem").SetValue(player, shield);
            var enemy = Root("BossImmuneToStagger").AddComponent<TrinketValidationEnemy>();
            AccessTools.Field(typeof(Character), "m_staggerWhenBlocked").SetValue(enemy, false);
            AccessTools.Field(typeof(Character), "m_boss").SetValue(enemy, true);
            var block = AccessTools.Method(typeof(Humanoid), "BlockAttack");
            Action<float> hit = timer =>
            {
                AccessTools.Field(typeof(Humanoid), "m_blockTimer").SetValue(player, timer);
                var data = new HitData { m_dir = Vector3.back };
                data.m_damage.m_blunt = 10f;
                block.Invoke(player, new object[] { data, enemy });
            };
            AccessTools.Field(typeof(Character), "m_boss").SetValue(enemy, false); hit(0.1f); Equal((float)Call("DamageMultiplier", player), 1.25f, "Normal parry retains base 25 percent"); Call("ClearRewards", player); AccessTools.Field(typeof(Character), "m_boss").SetValue(enemy, true);
            hit(1f);
            Equal((float)Call("DamageMultiplier", player), 1f, "Held block grants no reward");
            hit(0.1f);
            Equal((float)Call("DamageMultiplier", player), 1.35f, "Native parry against boss immune to stagger grants reward");
            var active = seman.GetStatusEffect(_effect.NameHash());
            Equal(active.m_ttl, 2f, "Two-second duration");
            active.UpdateStatusEffect(1f);
            hit(0.1f);
            Equal((float)Call("DamageMultiplier", player), 1.35f, "Further parry does not stack damage");
            Check(ReferenceEquals(active, seman.GetStatusEffect(_effect.NameHash())), "Further parry refreshes same status");
            active.UpdateStatusEffect(1.5f);
            Check(!active.IsDone(), "Refresh renews duration");
            active.UpdateStatusEffect(0.6f);
            Check(active.IsDone(), "Reward expires after two seconds");
            seman.Update(null, 0f);
            Equal((float)Call("DamageMultiplier", player), 1f, "Expired reward no longer boosts damage");
            player.StaminaAvailable = false;
            hit(0.1f);
            Equal((float)Call("DamageMultiplier", player), 1f, "Failed stamina block grants no reward");
            player.StaminaAvailable = true;
            player.Overwhelmed = true;
            hit(0.1f);
            Equal((float)Call("DamageMultiplier", player), 1f, "Guard break grants no reward");
            player.Overwhelmed = false;
            hit(0.1f);
            var wandHit = new HitData();
            wandHit.m_damage.m_fire = 40f;
            var wand = new ItemDrop.ItemData { m_dropPrefab = bundle.LoadAsset<GameObject>("HexBattleMage_Wand_Meadows_01") };
            var damagePatch = Mod.GetType("HexBattleMage.Patches.TrinketWandDamagePatch").GetMethod("Prefix", Flags);
            var shotArguments = new object[] { player, wandHit, wand }; damagePatch.Invoke(null, shotArguments);
            Equal(((HitData)shotArguments[1]).m_damage.m_fire, 54f, "Wand projectile receives reward"); Equal(wandHit.m_damage.m_fire, 40f, "Shared input unchanged"); var repeatedArguments = new object[] { player, wandHit, wand }; damagePatch.Invoke(null, repeatedArguments); Equal(((HitData)repeatedArguments[1]).m_damage.m_fire, 54f, "Repeated projectile does not compound");
            var otherHit = new HitData();
            otherHit.m_damage.m_blunt = 40f;
            damagePatch.Invoke(null, new object[] { player, otherHit, shield });
            Equal(otherHit.m_damage.m_blunt, 40f, "Other weapons unchanged");
            player.UnequipItem(second, false);
            Equal(player.GetMaxEitr(), 0f, "Unequipping leaves the native pool unchanged");
            staminaMultiplier = 1f;
            seman.ModifyStaminaRegen(ref staminaMultiplier);
            Equal(staminaMultiplier, 1f, "Unequip removes stamina regeneration");
            Equal((float)typeof(Player).GetMethod("GetEquipmentEitrRegenModifier", Flags).Invoke(player, null), 0f, "Unequip removes Eitr regeneration");
            Equal((float)Call("DamageMultiplier", player), 1f, "Unequipping removes reward");
            player.EquipItem(second, false);
            Check(!seman.HaveStatusEffect(_effect.NameHash()), "Re-equipping cannot restore reward");
            Call("RewardParry", player, enemy);
            Check(seman.HaveStatusEffect(_effect.NameHash()), "Reward can activate again");
            Mod.GetType("HexBattleMage.Patches.TrinketDeathPatch").GetMethod("Prefix", Flags).Invoke(null, new object[] { player });
            Check(!seman.HaveStatusEffect(_effect.NameHash()), "Death hook clears reward");
            player.Dead = true;
            Call("RewardParry", player, enemy);
            Check(!seman.HaveStatusEffect(_effect.NameHash()), "Dead player cannot receive reward");
            player.Dead = false;
            var neck = new GameObject("Neck").transform;
            neck.SetParent(player.transform, false);
            neck.localPosition = new Vector3(0f, 1.5f, 0f);
            neck.localRotation = Quaternion.Euler(40f, 50f, 20f);
            var worn = Object.Instantiate(visual, neck, false);
            var wornType = Mod.GetType("HexBattleMage.Core.Visuals.TrinketWornVisual");
            var controller = worn.AddComponent(wornType);
            wornType.GetMethod("LateUpdate", Flags).Invoke(controller, null);
            Equal(Vector3.Distance(worn.transform.position, neck.position + player.transform.TransformDirection(profile.WornPosition)), 0f, "Worn position respects neck anchor and character axes");
            Equal(Vector3.Dot(-worn.transform.forward, player.transform.forward), 1f, "Rune faces out from chest despite rotated neck bone");
            neck.localPosition += Vector3.up * 0.1f;
            wornType.GetMethod("LateUpdate", Flags).Invoke(controller, null);
            Equal(Vector3.Distance(worn.transform.position, neck.position + player.transform.TransformDirection(profile.WornPosition)), 0f, "Attachment reads current body anchor");
            AccessTools.Field(typeof(Character), "m_boss").SetValue(enemy, false); hit(0.1f); Equal((float)Call("DamageMultiplier", player), 1.25f, "Normal parry replaces boss reward without stacking"); AccessTools.Field(typeof(Character), "m_boss").SetValue(enemy, true); hit(0.1f); Equal((float)Call("DamageMultiplier", player), 1.35f, "Boss parry restores 35 percent");
            var tooltip = (string)Call("Tooltip", profile); Check(tooltip.Contains("Boss Effect") && tooltip.Contains("+35% total"), "Boss effect tooltip reports total");
            var alternateRoot = Root("DifferentParryTrinket");
            var alternate = alternateRoot.AddComponent<HexBattleMageTrinketProfile>();
            alternate.ItemPrefabName = "HexValidationParryTrinket";
            alternate.DisplayName = "Alternate Test Trinket";
            alternate.RewardTrigger = TrinketRewardTrigger.SuccessfulParry;
            alternate.WandDamageBonus = 0.5f;
            alternate.BossParryDamageBonus = 0.2f;
            alternate.ParryDuration = 3f;
            var alternateEitr = alternateRoot.AddComponent<MageEitrProfile>();
            alternateEitr.MaximumEitr = 40f;
            alternateEitr.RegenerationModifier = 0.3f;
            var alternateDefinition = alternateRoot.AddComponent<MageEquipmentDefinition>();
            alternateDefinition.Category = MageEquipmentCategory.Trinket;
            alternateDefinition.Slot = MageEquipmentSlot.Trinket;
            alternateRoot.AddComponent<MageRecipeProfile>();
            var alternateItem = new ItemDrop.ItemData { m_dropPrefab = alternateRoot, m_shared = new ItemDrop.ItemData.SharedData { m_name = alternate.DisplayName, m_itemType = ItemDrop.ItemData.ItemType.Trinket, m_maxStackSize = 1 } };
            alternateRoot.AddComponent<ItemDrop>().m_itemData = alternateItem;
            player.GetInventory().GetAllItems().Add(alternateItem);
            var passiveRoot = Root("PassiveOnlyTrinket");
            var passive = passiveRoot.AddComponent<HexBattleMageTrinketProfile>();
            passive.ItemPrefabName = "HexValidationPassiveTrinket";
            passive.RewardTrigger = TrinketRewardTrigger.None;
            var passiveEitr = passiveRoot.AddComponent<MageEitrProfile>();
            passiveEitr.MaximumEitr = 12f;
            passiveEitr.RegenerationModifier = 0.08f;
            var passiveDefinition = passiveRoot.AddComponent<MageEquipmentDefinition>();
            passiveDefinition.Category = MageEquipmentCategory.Trinket;
            passiveDefinition.Slot = MageEquipmentSlot.Trinket;
            passiveRoot.AddComponent<MageRecipeProfile>();
            var passiveItem = new ItemDrop.ItemData { m_dropPrefab = passiveRoot, m_shared = new ItemDrop.ItemData.SharedData { m_name = "Passive Test Trinket", m_itemType = ItemDrop.ItemData.ItemType.Trinket, m_maxStackSize = 1 } };
            passiveRoot.AddComponent<ItemDrop>().m_itemData = passiveItem;
            player.GetInventory().GetAllItems().Add(passiveItem);
            Call("Configure", new[] { visual, alternateRoot, passiveRoot }, config);
            var alternateSettings = Call("GetSettings", alternate);
            var alternateStatus = (StatusEffect)ScriptableObject.CreateInstance(Mod.GetType("HexBattleMage.Core.Trinkets.TrinketRewardStatus"));
            alternateStatus.name = (string)alternateSettings.GetType().GetProperty("RewardStatusName", Flags).GetValue(alternateSettings);
            alternateStatus.m_ttl = 3f;
            alternateStatus.m_effectsOnlyOnPlayer = true;
            RewardTemplates.Add(alternateStatus.NameHash(), alternateStatus);
            Check(player.EquipItem(alternateItem, false), "Different trinket definition equips through native slot");
            Check(!second.m_equipped && alternateItem.m_equipped, "Different types share one native equipped slot");
            Equal(player.GetMaxEitr(), 40f, "Alternate trinket uses its own Eitr value");
            Equal((float)typeof(Player).GetMethod("GetEquipmentEitrRegenModifier", Flags).Invoke(player, null), 0.3f, "Alternate trinket uses its own regeneration value");
            Check(!seman.HaveStatusEffect(_effect.NameHash()), "Swapping removes the old stone reward");
            AccessTools.Field(typeof(Character), "m_boss").SetValue(enemy, false);
            hit(0.1f);
            Equal((float)Call("DamageMultiplier", player), 1.5f, "Alternate normal parry uses its own 50 percent bonus");
            Equal(seman.GetStatusEffect(alternateStatus.NameHash()).m_ttl, 3f, "Alternate duration is independent");
            AccessTools.Field(typeof(Character), "m_boss").SetValue(enemy, true);
            hit(0.1f);
            Equal((float)Call("DamageMultiplier", player), 1.7f, "Alternate boss reward uses its own additional 20 percent");
            Check(!seman.HaveStatusEffect(_effect.NameHash()), "Alternate parry does not grant the stone status");
            AccessTools.Field(typeof(Player), "m_eitr").SetValue(player, 35f);
            Check(player.EquipItem(passiveItem, false), "Passive-only trinket equips");
            Equal(player.GetMaxEitr(), 12f, "Passive-only trinket uses its own Eitr cap");
            Equal(player.GetEitr(), 12f, "Swap to smaller Eitr pool clamps current Eitr");
            Check(!seman.HaveStatusEffect(alternateStatus.NameHash()), "Swap clears alternate reward");
            hit(0.1f);
            Equal((float)Call("DamageMultiplier", player), 1f, "Passive-only trinket grants no parry damage bonus");
            Check(!seman.HaveStatusEffect(_effect.NameHash()) && !seman.HaveStatusEffect(alternateStatus.NameHash()), "Passive-only parry grants no registered combat status");
            Check(!((string)Call("Tooltip", passive)).Contains("Combat Reward"), "Passive-only tooltip hides combat reward");
            Check(player.EquipItem(second, false), "Original stone re-equips after different trinkets");
            Equal(player.GetMaxEitr(), 0f, "Original stone removes the alternate flat pool");
            Equal(player.GetEitr(), 0f, "Zero pool clamps remaining Eitr");
            Equal((float)Call("DamageMultiplier", player), 1f, "Swap back does not restore old reward");
            hit(0.1f);
            Equal((float)Call("DamageMultiplier", player), 1.35f, "Original boss bonus is unchanged by alternate settings");
            player.UnequipItem(second, false);
            second.m_equipped = true;
            AccessTools.Method(typeof(Player), "EquipInventoryItems").Invoke(player, null);
            Check(ReferenceEquals(Call("GetEquipped", player), second), "Native saved-equipment restoration selects the correct trinket definition");
            File.WriteAllText("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/trinket-integration-report.txt", "PASS: " + _checks + " checks on raw compiled mod DLL, installed Valheim native equipment/parry methods and production bundle. Network status transport, visual setup, damage text, analytics and stagger physics isolated. Native boss parry, failed blocks, refresh/expiry, one slot, food-independent max Eitr, regen modifier, wand-only damage and unequip verified.");
        }
        finally
        {
            harmony.UnpatchSelf();
            foreach (var root in Roots) { Object.DestroyImmediate(root); }
            bundle.Unload(true);
        }
    }
}

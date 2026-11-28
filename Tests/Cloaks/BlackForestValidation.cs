using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Elements;
using System.Linq;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BlackForestValidation
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly BepInPlugin Metadata = new BepInPlugin("test.hexbattlemage", "HexBattleMage Registration Test", "1.0.0");
    private static GameObject _basePrefab;
    private static GameObject _visual;

    public static void Run()
    {
        var harmony = new Harmony("HexBattleMage.BlackForestValidation");
        var container = new GameObject("RegistrationTestContainer");

        container.SetActive(false);
        Patch(harmony, typeof(ItemDrop), "Awake", "Skip");
        Patch(harmony, typeof(ItemDrop), "OnValidate", "Skip");
        harmony.Patch(AccessTools.Method(typeof(AssetManager), "AddAsset", new[] { typeof(Object), typeof(Object) }), prefix: new HarmonyMethod(typeof(BlackForestValidation), "Skip"));

        foreach (var method in typeof(Jotunn.Utils.BepInExUtils).GetMethods(Fields))
        {
            if (method.Name == "GetSourceModMetadata")
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(BlackForestValidation), "SourceMod"));
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
        var manager = mod.GetType("HexBattleMage.Core.Managers.StaffManager");
        string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "TrinketRegistration");

        Directory.CreateDirectory(directory);
        typeof(Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(directory, "core.cfg"));

        var config = new ConfigFile(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".cfg"), false) { SaveOnConfigSet = false };

        try
        {
            _visual = bundle.LoadAsset<GameObject>("HexBattleMage_Wand_Meadows_01");
            var helper = mod.GetType("HexBattleMage.Assets.AssetBundleHelper");

            helper.GetField("<Bundle>k__BackingField", Fields).SetValue(null, bundle);
            helper.GetField("<HexBattleMageWand>k__BackingField", Fields).SetValue(null, _visual);
            var fireColors = bundle.LoadAsset<GameObject>("HexWandElementFire").GetComponent<WandElementProfile>();
            var blightColors = bundle.LoadAsset<GameObject>("HexWandElementBlight").GetComponent<WandElementProfile>();

            Check(fireColors.ProjectileCoreColor.r > 0.9f && fireColors.ProjectileCoreColor.g < 0.02f, "Fire projectile colors are baked as saturated red.");
            Check(blightColors.ProjectileCoreColor.maxColorComponent < 0.03f && blightColors.ProjectileHaloColor.g > blightColors.ProjectileHaloColor.r * 3f, "Blight projectile colors are baked as a dark core with a green halo.");
            fireColors.ProjectileCoreColor = Color.white;
            blightColors.ProjectileHaloColor = Color.white;
            bundle.LoadAsset<GameObject>("HexWandElementBlight").GetComponent<WandElementProfile>().TotalDamage = 999f;
            bundle.LoadAsset<GameObject>("HexBattleMage_Staff_BlackForest_01_Definition").GetComponent<HexBattleMageStaffProfile>().BluntDamage = 999f;
            mod.GetType("HexBattleMage.Core.Tuning.PrefabTuning").GetMethod("Apply", Fields).Invoke(null, new object[] { bundle });
            var elements = mod.GetType("HexBattleMage.Core.Elements.WandElementManager");

            Check(bundle.LoadAsset<GameObject>("HexWandElementBlight").GetComponent<WandElementProfile>().TotalDamage == 60f, "Blight has a working source override.");
            Check(bundle.LoadAsset<GameObject>("HexBattleMage_Staff_BlackForest_01_Definition").GetComponent<HexBattleMageStaffProfile>().BluntDamage == 30f, "Staff has a working source override.");
            elements.GetMethod("Configure", Fields).Invoke(null, new object[] { config });
            ValidateFlightColors(elements);
            harmony.CreateClassProcessor(mod.GetType("HexBattleMage.Patches.WandProjectileColorPatch")).Patch();
            harmony.CreateClassProcessor(mod.GetType("HexBattleMage.Patches.WandNativeElementPatch")).Patch();
            ValidateBlight(bundle, mod, harmony, container);
            ValidateStaff(bundle, mod, harmony, container, items);
            ValidateGear(bundle, mod, items);
            Check(bundle.LoadAllAssets<GameObject>().Count(asset => asset.GetComponent<HexBattleMageArmorProfile>()?.ItemPrefabName.Contains("_BlackForest_") == true) == 3, "Three Black Forest armor definitions are bundled.");
            var cloak = bundle.LoadAsset<GameObject>("HexBattleMage_Cloak_BlackForest_01");

            Check(cloak.GetComponent<MageEitrProfile>().MaximumEitr == 8f && cloak.GetComponent<HexBattleMageCloakProfile>().Armor == 4f, "Black Forest cloak bonuses are authored.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "blackforest-report.txt"), "PASS: actual SEMan Blight lifecycle, non-stacking refresh, complete damage budget, untyped damage, owner-only ticks, iterative multi-hop spread, single visit per chain, player/tame exclusions and line-of-sight; actual staff registration and blunt/pickaxe costs; Black Forest gear authoring. Native manager/world bootstrap, damage transport and health storage are isolated fixtures.");
        }

        finally
        {
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(_basePrefab);
            bundle.Unload(true);
            harmony.UnpatchSelf();
        }
    }

    private static void ValidateFlightColors(Type elements)
    {
        var flight = elements.GetMethod("TryGetFlightColors", Fields);
        object[] fire = { 1 << (int)WandElement.Fire, Color.clear, Color.clear };
        object[] blight = { 1 << (int)WandElement.Blight, Color.clear, Color.clear };
        object[] physical = { 0, Color.clear, Color.clear };

        Check((bool)flight.Invoke(null, fire) && ((Color)fire[1]).r > 0.9f && ((Color)fire[1]).g < 0.02f, "Runtime source tuning retains a red Fire core.");
        Check((bool)flight.Invoke(null, blight) && ((Color)blight[1]).maxColorComponent < 0.03f && ((Color)blight[2]).g > ((Color)blight[2]).r * 3f, "Runtime source tuning retains black-green Blight colors.");
        Check(!(bool)flight.Invoke(null, physical), "Unattuned physical projectiles use the existing fallback colors.");
        object[] mixed = { (1 << (int)WandElement.Fire) | (1 << (int)WandElement.Blight), Color.clear, Color.clear };

        Check((bool)flight.Invoke(null, mixed) && ((Color)mixed[1] - (((Color)fire[1] + (Color)blight[1]) / 2f)).maxColorComponent < 0.001f, "Multiple active effects blend their configured colors.");
        var mask = elements.GetMethod("GetElementMask", Fields);
        var effects = (System.Collections.IDictionary)elements.GetField("Effects", Fields).GetValue(null);

        foreach (System.Collections.DictionaryEntry entry in effects)
        {
            var effect = (StatusEffect)entry.Value;

            Check((int)mask.Invoke(null, new object[] { effect.NameHash() }) == (1 << (int)(WandElement)entry.Key), "Projectile status maps to the corresponding elemental color mask.");
        }

        Check((int)mask.Invoke(null, new object[] { 0 }) == 0, "Physical-only shots carry no elemental color mask.");
    }

    private static readonly Dictionary<Character, float> Damage = new Dictionary<Character, float>();
    private static readonly Dictionary<Character, SEMan> Managers = new Dictionary<Character, SEMan>();
    private static readonly HashSet<Character> Remote = new HashSet<Character>();
    private static readonly Dictionary<Character, int> Infections = new Dictionary<Character, int>();
    private static SE_HexBlight _template;
    private static HitData _staffImpact;
    private static Character _attacker;

    private static void ValidateBlight(AssetBundle bundle, Assembly mod, Harmony harmony, GameObject container)
    {
        Patch(harmony, typeof(Character), "IsOwner", "Owner");
        harmony.Patch(AccessTools.Method(typeof(Character), "IsTamed", Type.EmptyTypes), prefix: new HarmonyMethod(typeof(BlackForestValidation), "Tamed"));
        Patch(harmony, typeof(Character), "GetCenterPoint", "Center");
        Patch(harmony, typeof(Character), "GetZDOID", "Identity");
        Patch(harmony, typeof(Character), "ApplyDamage", "CaptureDamage");
        Patch(harmony, typeof(Character), "Damage", "Infect");
        Patch(harmony, typeof(BaseAI), "IsEnemy", "Hostile");
        var list = (List<Character>)AccessTools.Field(typeof(Character), "s_characters").GetValue(null);
        var previous = list.ToArray();

        container.transform.position = new Vector3(1000f, 100f, 1000f);

        list.Clear();

        try
        {
            var profile = bundle.LoadAsset<GameObject>("HexWandElementBlight").GetComponent<WandElementProfile>();

            _template = ScriptableObject.CreateInstance<SE_HexBlight>();
            _template.name = "HexWandStatusBlight";
            _template.m_effectsOnlyOnPlayer = false;
            _attacker = Enemy(container, "Attacker", new Vector3(-10f, 0f, 0f));
            var first = Enemy(container, "First", Vector3.zero);
            var second = Enemy(container, "Second", new Vector3(1.5f, 0f, 0f));
            var third = Enemy(container, "Third", new Vector3(3f, 0f, 0f));
            var player = Enemy(container, "Player excluded", new Vector3(1f, 0f, 0f));

            ((BlackForestEnemy)player).PlayerTarget = true;
            var tame = Enemy(container, "Tame excluded", new Vector3(1f, 0f, 0f));

            AccessTools.Field(typeof(Character), "m_tamed").SetValue(tame, true);
            var effect = Add(first, 1);

            effect.SetAttacker(_attacker);
            Check(Infections.Count == 0, "Base Blight has zero contagion radius.");
            effect.UpdateStatusEffect(3f);
            Check(Mathf.Abs(Damage[first] - 60f) < 0.001f, "A full three-second application delivers exactly sixty untyped damage.");
            Damage.Clear();
            effect = Add(first, 1);
            effect.UpdateStatusEffect(1f);
            Add(first, 1);
            Check(Managers[first].GetStatusEffects().Count == 1, "Refresh keeps one effect, without stacking.");
            Damage.Clear();
            effect.UpdateStatusEffect(3f);
            Check(Mathf.Abs(Damage[first] - 60f) < 0.001f, "Refreshing does not accumulate a larger damage budget.");
            Damage.Clear();
            Remote.Add(first);
            Add(first, 1).UpdateStatusEffect(3f);
            Check(Damage.Count == 0, "Remote peers cannot apply ticks.");
            Remote.Clear();
            var settings = mod.GetType("HexBattleMage.Core.Elements.WandElementManager").GetMethod("GetSettings", Fields).Invoke(null, new object[] { WandElement.Blight });
            var radius = (ConfigEntry<float>)settings.GetType().GetProperty("SpreadRadius", Fields).GetValue(settings);

            radius.Value = 2f;
            Add(first, 1).SetAttacker(_attacker);
            Check(Infections.Count == 2 && Infections[second] == 1 && Infections[third] == 1, "Contagion immediately travels two hops, visiting each enemy once.");
            Check(!Infections.ContainsKey(player) && !Infections.ContainsKey(tame), "Players and tame creatures are excluded.");
            Infections.Clear();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);

            wall.transform.SetParent(null, false);
            wall.transform.position = container.transform.TransformPoint(new Vector3(2.25f, 1f, 0f));
            wall.transform.localScale = new Vector3(0.2f, 4f, 4f);
            wall.SetActive(true);
            Physics.SyncTransforms();
            Add(first, 1).SetAttacker(_attacker);
            Check(Infections.ContainsKey(second) && !Infections.ContainsKey(third), "A solid wall blocks propagation.");
            Object.DestroyImmediate(wall);
            radius.Value = 0f;
            var duration = (ConfigEntry<float>)settings.GetType().GetProperty("Duration", Fields).GetValue(settings);

            duration.Value = 1.5f;
            Damage.Clear();
            Add(first, 1).UpdateStatusEffect(1.5f);
            Check(Mathf.Abs(Damage[first] - 60f) < 0.001f, "Shorter duration preserves the total damage budget.");
            duration.Value = 3f;
        }
        finally
        {
            list.Clear();
            list.AddRange(previous);
            Object.DestroyImmediate(_template);
        }
    }

    private static Character Enemy(GameObject container, string name, Vector3 position)
    {
        var root = new GameObject(name);

        root.SetActive(false);
        root.transform.SetParent(container.transform, false);
        root.transform.position = container.transform.TransformPoint(position);
        var enemy = root.AddComponent<BlackForestEnemy>();
        var manager = new SEMan(enemy, root.AddComponent<ZNetView>());

        AccessTools.Field(typeof(Character), "m_seman").SetValue(enemy, manager);
        AccessTools.Field(typeof(Character), "m_collider").SetValue(enemy, root.AddComponent<CapsuleCollider>());
        AccessTools.Field(typeof(Character), "m_nview").SetValue(enemy, root.GetComponent<ZNetView>());
        Managers[enemy] = manager;
        ((List<Character>)AccessTools.Field(typeof(Character), "s_characters").GetValue(null)).Add(enemy);
        return enemy;
    }

    private static SE_HexBlight Add(Character target, int level)
    {
        Managers[target].AddStatusEffect(_template, true, level, 0f);
        return (SE_HexBlight)Managers[target].GetStatusEffect(_template.NameHash());
    }

    private static bool Tamed(Character __instance, ref bool __result)
    {
        __result = (bool)AccessTools.Field(typeof(Character), "m_tamed").GetValue(__instance);
        return false;
    }

    private static bool Owner(Character __instance, ref bool __result)
    {
        __result = !Remote.Contains(__instance);
        return false;
    }

    private static bool Center(Character __instance, ref Vector3 __result)
    {
        __result = __instance.transform.position + Vector3.up;
        return false;
    }

    private static bool Identity(Character __instance, ref ZDOID __result)
    {
        __result = new ZDOID(1234L, unchecked((uint)__instance.GetInstanceID()));
        return false;
    }

    private static bool CaptureDamage(Character __instance, HitData hit)
    {
        Check(hit.m_damage.m_spirit == 0f && hit.m_damage.m_poison == 0f && hit.m_damage.m_blunt == 0f, "Blight never uses a native resisted channel.");
        Check(hit.m_attacker == _attacker.GetZDOID(), "Blight ticks retain attacker attribution.");
        Damage[__instance] = (Damage.TryGetValue(__instance, out float value) ? value : 0f) + hit.m_damage.m_damage;
        return false;
    }

    private static bool Infect(Character __instance, HitData hit)
    {
        if (hit.m_statusEffectHash == 0)
        {
            _staffImpact = hit;
            return false;
        }

        Infections[__instance] = (Infections.TryGetValue(__instance, out int value) ? value : 0) + 1;
        Add(__instance, hit.m_itemLevel).SetAttacker(_attacker);
        return false;
    }

    private static bool Hostile(Character __0, Character __1, ref bool __result)
    {
        __result = __1 != _attacker;
        return false;
    }

    private static void ValidateStaff(AssetBundle bundle, Assembly mod, Harmony harmony, GameObject container, ItemManager items)
    {
        Patch(harmony, typeof(Projectile), "Awake", "Skip");

        var source = bundle.LoadAsset<GameObject>("HexBattleMage_Staff_BlackForest_01_Definition");
        var profile = source.GetComponent<HexBattleMageStaffProfile>();

        _basePrefab = new GameObject("StaffFireball");
        _basePrefab.SetActive(false);
        _basePrefab.layer = 1;
        var drop = _basePrefab.AddComponent<ItemDrop>();
        var projectile = new GameObject("Native fireball fixture");

        projectile.SetActive(false);
        projectile.transform.SetParent(container.transform, false);
        var projectileComponent = projectile.AddComponent<Projectile>();
        var setup = mod.GetType("HexBattleMage.Patches.BlightProjectileSetupPatch").GetMethod("Postfix", Fields);
        var elementManager = mod.GetType("HexBattleMage.Core.Elements.WandElementManager");
        var blightHit = new HitData { m_statusEffectHash = (int)elementManager.GetMethod("GetHash", Fields).Invoke(null, new object[] { WandElement.Blight }) };
        var wandItem = new ItemDrop.ItemData { m_dropPrefab = _visual };

        projectileComponent.m_aoe = 3f;
        setup.Invoke(null, new object[] { projectileComponent, blightHit, wandItem });
        Check(projectileComponent.m_aoe == 0f, "Base Blight hits a single target without an inherited blast radius.");
        blightHit.m_statusEffectHash = (int)elementManager.GetMethod("GetHash", Fields).Invoke(null, new object[] { WandElement.Fire });
        projectileComponent.m_aoe = 3f;
        setup.Invoke(null, new object[] { projectileComponent, blightHit, wandItem });
        Check(projectileComponent.m_aoe == 3f, "Fire retains its existing impact area.");
        var inheritedSpawner = projectile.AddComponent<CinderSpawner>();

        inheritedSpawner.m_spawnOnProjectileHit = true;
        drop.m_itemData = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData { m_attack = new Attack { m_attackProjectile = projectile } } };
        mod.GetType("HexBattleMage.Core.Managers.StaffManager").GetMethod("RegisterStaff", Fields).Invoke(null, new object[] { source });
        var registered = ((Dictionary<string, CustomItem>)AccessTools.Field(typeof(ItemManager), "Items").GetValue(items))[profile.ItemPrefabName];
        var shared = registered.ItemDrop.m_itemData.m_shared;

        Check(profile.Icon != null && profile.Icon.texture.width <= 256 && profile.Icon.texture.isReadable && profile.Icon.texture.GetPixels32().Any(pixel => pixel.a == 0), "Staff has a readable transparent 256px icon.");
        Check(shared.m_damages.m_blunt == 30f && shared.m_damages.m_pickaxe == 20f && shared.m_damages.m_fire == 0f, "Earth staff registration has only blunt and pickaxe damage.");
        Check(shared.m_attack.m_attackStamina == 5f && shared.m_attack.m_attackEitr == 8f && shared.m_toolTier == 1, "Earth resource costs and mining tier are configured.");
        Check(shared.m_attack.m_attackProjectile.GetComponent<Projectile>().m_aoe == 0f, "Earth projectile does not inherit the fireball explosion.");
        Check(projectile.GetComponent<CinderSpawner>() != null && shared.m_attack.m_attackProjectile.GetComponent<CinderSpawner>() == null, "Earth clone removes native CinderSpawner without changing the source fireball.");
        Check(profile.ProjectileVisualScale == 2.75f && shared.m_attack.m_attackProjectile.GetComponent<Projectile>().m_rayRadius == 0.22f, "Boulder visual size and impact radius are authored and projected.");
        Check(registered.ItemPrefab.GetComponent<Collider>() != null && registered.ItemPrefab.transform.Find("attach") != null, "Earth staff has both native attachment and world collision.");
        harmony.CreateClassProcessor(mod.GetType("HexBattleMage.Patches.StaffProjectileHitPatch")).Patch();
        harmony.Patch(AccessTools.Method(typeof(Projectile), "IsValidTarget"), prefix: new HarmonyMethod(typeof(BlackForestValidation), "ValidImpactTarget"));
        var shot = Object.Instantiate(shared.m_attack.m_attackProjectile);

        shot.transform.SetParent(container.transform, false);
        var impact = shot.GetComponent<Projectile>();
        var view = shot.AddComponent<ZNetView>();
        var target = Enemy(container, "Earth impact enemy", new Vector3(6f, 0f, 0f));

        AccessTools.Field(typeof(Projectile), "m_nview").SetValue(impact, view);
        AccessTools.Field(typeof(Projectile), "m_owner").SetValue(impact, _attacker);
        AccessTools.Field(typeof(Projectile), "m_weapon").SetValue(impact, registered.ItemDrop.m_itemData);
        impact.m_hitType = HitData.HitType.PlayerHit;
        impact.m_damage = shared.m_damages;
        impact.m_stayAfterHitStatic = true;
        impact.m_hitNoise = 0f;
        impact.OnHit(target.GetComponent<Collider>(), target.transform.position, false, Vector3.back);
        Check(_staffImpact != null && _staffImpact.m_damage.m_blunt == 30f && _staffImpact.m_damage.m_pickaxe == 20f && _staffImpact.m_statusEffectHash == 0, "Actual native staff impact sends physical earth damage once without an elemental status.");
        Check((bool)AccessTools.Field(typeof(Projectile), "m_didHit").GetValue(impact), "Native boulder stops after a creature impact.");
    }

    private static void ValidateGear(AssetBundle bundle, Assembly mod, ItemManager items)
    {
        var registered = (Dictionary<string, CustomItem>)AccessTools.Field(typeof(ItemManager), "Items").GetValue(items);

        foreach (string category in new[] { "Helmet", "Chest", "Legs", "Cloak" })
        {
            string identity = "HexBattleMage_" + category + "_BlackForest_01";
            var source = bundle.LoadAsset<GameObject>(identity + (category == "Cloak" ? string.Empty : "_Definition"));
            var armor = source.GetComponent<HexBattleMageArmorProfile>();
            string baseName = armor != null ? armor.BaseItemPrefab : source.GetComponent<HexBattleMageCloakProfile>().BaseItemPrefab;

            Object.DestroyImmediate(_basePrefab);
            _basePrefab = new GameObject(baseName);
            _basePrefab.SetActive(false);
            _basePrefab.layer = 1;
            var native = _basePrefab.AddComponent<ItemDrop>();
            var type = category == "Helmet" ? ItemDrop.ItemData.ItemType.Helmet : category == "Chest" ? ItemDrop.ItemData.ItemType.Chest : category == "Legs" ? ItemDrop.ItemData.ItemType.Legs : ItemDrop.ItemData.ItemType.Shoulder;

            native.m_itemData = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData { m_itemType = type } };
            new GameObject("attach_skin").transform.SetParent(_basePrefab.transform, false);
            var nativeModel = new GameObject("model");

            nativeModel.transform.SetParent(_basePrefab.transform, false);
            nativeModel.AddComponent<BoxCollider>();

            if (category == "Cloak")
            {
                mod.GetType("HexBattleMage.Core.Managers.CloakManager").GetMethod("RegisterCloak", Fields).Invoke(null, new object[] { source });
            }
            else
            {
                mod.GetType("HexBattleMage.Core.Managers.ArmorManager").GetMethod("RegisterArmor", Fields).Invoke(null, new object[] { source });
            }

            var item = registered[identity];

            Check(item.ItemDrop.m_itemData.m_shared.m_armor == (category == "Cloak" ? 4f : 6f), "Black Forest gear armor survives actual registration.");
            Check(item.ItemPrefab.GetComponent<MageEitrProfile>().MaximumEitr == 8f, "Black Forest gear Eitr survives registration.");
            Check(item.Recipe.Recipe.m_resources.Length == 3, "Black Forest recipes have three native material requirements.");
            Check(item.ItemPrefab.GetComponent<MageEquipmentDefinition>().CatalogBossDefeatKey == "defeated_eikthyr", "Black Forest catalog is revealed by Eikthyr.");
        }
    }

    private static bool ValidImpactTarget(ref bool __result)
    {
        __result = true;
        return false;
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
        harmony.Patch(AccessTools.Method(type, method, type == typeof(BaseAI) ? new[] { typeof(Character), typeof(Character) } : null), prefix: new HarmonyMethod(typeof(BlackForestValidation), prefix));
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


public sealed class BlackForestEnemy : Humanoid
{
    public bool PlayerTarget;

    protected override void Awake() { }
    protected override void OnDestroy() { }
    public override bool IsPlayer() { return PlayerTarget; }
    public override bool IsDead() { return false; }
}

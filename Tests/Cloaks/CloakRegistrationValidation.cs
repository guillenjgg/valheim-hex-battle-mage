using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private static Game _gameFixture;
    private static bool _helmetNetworkOwner = true;
    private static int _materialResolutionCount;

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
        _materialResolutionCount = 0;
        harmony.Patch(AccessTools.Method(mockType, "FixReferences", new[] { typeof(object), typeof(int) }), prefix: new HarmonyMethod(typeof(CloakRegistrationValidation), "ValidateMockTarget"));
        harmony.Patch(AccessTools.Method(mockType, "TryFixMaterial"), prefix: new HarmonyMethod(typeof(CloakRegistrationValidation), "CountMaterialResolution"));

        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE"));
        var mod = typeof(HexBattleMageCloakProfile).Assembly;
        var manager = mod.GetType("HexBattleMage.Core.Managers.CloakManager");
        string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "TrinketRegistration");

        Directory.CreateDirectory(directory);
        typeof(Paths).GetProperty("BepInExConfigPath").SetValue(null, Path.Combine(directory, "core.cfg"));

        var config = new ConfigFile(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".cfg"), false) { SaveOnConfigSet = false };

        try
        {
            mod.GetType("HexBattleMage.Core.Tuning.PrefabTuning").GetMethod("Apply", Fields).Invoke(null, new object[] { bundle });
            _visual = bundle.LoadAsset<GameObject>("HexBattleMage_Cloak_Meadows_01");
            var profile = _visual.GetComponent<HexBattleMageCloakProfile>();
            var sourceStyles = _visual.GetComponent<MageArmorStyleProfile>();

            sourceStyles.Styles = new MageArmorStyle[0];
            sourceStyles.BakedStyles = null;

            _basePrefab = new GameObject(profile.BaseItemPrefab);
            _basePrefab.SetActive(false);
            _basePrefab.layer = 1;
            var baseDrop = _basePrefab.AddComponent<ItemDrop>();

            baseDrop.m_itemData = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData() };
            Check(prefabs.GetPrefab(profile.ItemPrefabName) == _visual, "Loaded visual occupies the authored name before registration.");
            mod.GetType("HexBattleMage.Assets.AssetBundleHelper").GetProperty("Bundle", Fields).SetValue(null, bundle);
            manager.GetMethod("Register", Fields).Invoke(null, null);

            var registered = (Dictionary<string, CustomItem>)typeof(ItemManager).GetField("Items", Fields).GetValue(items);

            Check(registered.ContainsKey(profile.ItemPrefabName), "Actual registration adds the cloak despite the loaded visual name.");
            var item = registered[profile.ItemPrefabName];

            Check(_materialResolutionCount > 0, "Actual Jotunn resolver processes cloak materials without scanning cloth internals.");

            Check(!item.FixReference, "Cloak registration excludes cloth internals from recursive mock resolution.");
            Check(item.Recipe.FixReference, "Recipe mock resolution remains enabled independently.");
            Check(item.ItemPrefab.GetComponent<HexBattleMageVitalityProfile>().MaximumStamina == 5f, "Cloak registration retains the authored flat stamina bonus.");
            Check(item.ItemPrefab != _visual, "Gameplay item is a separate native-base clone.");
            Check(item.ItemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder, "Native cape item type is applied.");
            Check(item.Recipe.Recipe.m_resources.Length == 3, "Mage Table recipe includes all authored costs.");
            Check(item.Recipe.Recipe.m_item.name.EndsWith(profile.ItemPrefabName, StringComparison.Ordinal), "Recipe targets the gameplay cloak identity for reference resolution.");
            Check(item.Recipe.Recipe.m_craftingStation.name.EndsWith("HexArcaneWorkbench", StringComparison.Ordinal), "Recipe targets the Mage Table.");
            Check(prefabs.GetPrefab(profile.ItemPrefabName) == item.ItemPrefab, "Registered gameplay item supersedes the visual lookup.");
            Check(profile.FrostResistance, "Production prefab authors frost resistance.");
            Check(item.ItemDrop.m_itemData.m_shared.m_damageModifiers.Exists(modifier => modifier.m_type == HitData.DamageType.Frost && modifier.m_modifier == HitData.DamageModifier.Resistant), "Registration applies native frost resistance.");
            Check(item.Recipe.Recipe.m_resources.Any(resource => resource.m_resItem.name.EndsWith("Resin", StringComparison.Ordinal) && resource.m_amount == 5), "Recipe requires exactly five Resin.");
            ValidateUpgrades(item, harmony);
            ValidateStyleVisibility(item, harmony);
            ValidateStyles(item);
            ValidateOwnedSelection(item);
            ValidateClonedStyles(item);
            ValidateBodyFits(manager, item, harmony);
            foreach (string suffix in new[] { "Test01", "Test02" })
            {
                string identity = profile.ItemPrefabName + "_" + suffix;

                Check(registered.ContainsKey(identity), "Copied texture item registered: " + identity);
                var test = registered[identity];

                Check(test.ItemDrop.m_itemData.m_shared.m_damageModifiers.Exists(modifier => modifier.m_type == HitData.DamageType.Frost && modifier.m_modifier == HitData.DamageModifier.Resistant), "Texture test applies native frost resistance.");
                Check(test.Recipe == null, "Texture test is spawn-only.");
                Check(test.ItemDrop.m_itemData.m_shared.m_armor == 2f, "Test retains authored armor.");
                Check(test.ItemPrefab.GetComponent<MageEitrProfile>().MaximumEitr == 5f, "Test retains authored Eitr.");
                ValidateBodyFits(manager, test, harmony);
            }

            Check(item.ItemDrop.m_itemData.m_shared.m_icons[0] == item.ItemPrefab.GetComponent<MageArmorStyleProfile>().Styles[0].Icon, "Authored default style icon is retained.");
            ValidateArmor(bundle, registered, mod, harmony);
            ValidateFixedRecipes(bundle, mod);
            ValidateWorldDrops(bundle, registered, mod);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "cloak-registration-report.txt"), "PASS: actual cloak registration and Jotunn AddItem/AddRecipe; three color styles across both body fits and world drop; perks before durability/weight; authored icons and three-resource Mage Table recipe (including 5 Resin); native cape category; male/female lookup, vanilla pass-through and native AttachArmor transpiler verified. Native base/manager bootstrap are fixtures. Full game cloth/movement remains an in-game check.");
        }
        finally
        {
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(_basePrefab);
            bundle.Unload(true);
            harmony.UnpatchSelf();
        }
    }

    private static void ValidateMockTarget(object __0)
    {
        Check(__0 == null || !__0.GetType().FullName.StartsWith("MagicaCloth2.", StringComparison.Ordinal), "Mock resolver must not traverse MagicaCloth runtime state.");
    }

    private static void CountMaterialResolution()
    {
        _materialResolutionCount++;
    }

    private static void ValidateFixedRecipes(AssetBundle bundle, Assembly mod)
    {
        var rules = mod.GetType("HexBattleMage.Core.GearUpgradeRules");

        foreach (string identity in new[] { "HexBattleMage_Wand_Meadows_01", "HexMagicBracerVisual" })
        {
            var source = bundle.LoadAsset<GameObject>(identity);
            var authoredRecipe = source.GetComponent<MageRecipeProfile>();
            var fixture = new GameObject("Upgrade fixture");
            var recipe = ScriptableObject.CreateInstance<Recipe>();

            try
            {
                rules.GetMethod("CopyProfile", Fields).Invoke(null, new object[] { source, fixture });
                recipe.m_item = fixture.AddComponent<ItemDrop>();
                recipe.m_minStationLevel = authoredRecipe.MinimumStationLevel;
                recipe.m_resources = authoredRecipe.GetResources().Select(resource => new Piece.Requirement { m_amount = resource.Amount, m_amountPerLevel = resource.AmountPerQuality }).ToArray();
                rules.GetMethod("Register", Fields).Invoke(null, new object[] { recipe, fixture.GetComponent<MageUpgradeProfile>() });
                int maximumQuality = identity == "HexMagicBracerVisual" ? 3 : 4;

                for (int quality = 2; quality <= maximumQuality; quality++)
                {
                    Check(recipe.GetRequiredStationLevel(quality) == authoredRecipe.MinimumStationLevel + quality - 1, "Fixed material costs retain native station progression.");

                    foreach (var resource in recipe.m_resources)
                    {
                        Check(resource.GetAmount(quality) == resource.m_amountPerLevel && resource.GetAmount(quality) < resource.GetAmount(1), "Every wand/bracer upgrade costs less than the initial craft.");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(recipe);
                Object.DestroyImmediate(fixture);
            }
        }

        var trinket = bundle.LoadAsset<GameObject>("HexBattleMage_Trinket_Meadows_01");

        Check(trinket.GetComponent<HexBattleMage.Core.Trinkets.HexBattleMageTrinketProfile>().MaximumQuality == 1, "Trinket cannot be upgraded.");
        Check(trinket.GetComponent<MageRecipeProfile>().GetResources().Any(resource => resource.ItemPrefab == "BoneFragments" && resource.Amount == 5), "Trinket retains its bone crafting cost.");
    }

    private static void ValidateArmor(AssetBundle bundle, Dictionary<string, CustomItem> registered, Assembly mod, Harmony harmony)
    {
        var manager = mod.GetType("HexBattleMage.Core.Managers.ArmorManager");

        harmony.CreateClassProcessor(mod.GetType("HexBattleMage.Patches.ArmorAttachmentPatch")).Patch();

        foreach (string category in new[] { "Helmet", "Chest", "Legs" })
        {
            string identity = "HexBattleMage_" + category + "_Meadows_01";
            var definition = bundle.LoadAsset<GameObject>(identity + "_Definition");
            var profile = definition.GetComponent<HexBattleMageArmorProfile>();
            var styleProfile = definition.GetComponent<MageArmorStyleProfile>();

            styleProfile.Styles = new MageArmorStyle[0];
            styleProfile.BakedStyles = null;
            Object.DestroyImmediate(_basePrefab);
            _basePrefab = new GameObject(profile.BaseItemPrefab);
            _basePrefab.SetActive(false);

            var baseline = _basePrefab.AddComponent<ItemDrop>();
            var nativeType = category == "Helmet" ? ItemDrop.ItemData.ItemType.Helmet : category == "Chest" ? ItemDrop.ItemData.ItemType.Chest : ItemDrop.ItemData.ItemType.Legs;

            baseline.m_itemData = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData { m_itemType = nativeType, m_armor = 10f, m_setName = "native_set", m_setSize = 3, m_movementModifier = 0.03f } };
            new GameObject("attach_skin").transform.SetParent(_basePrefab.transform, false);
            if (category == "Legs")
            {
                var nativeDrop = new GameObject("Native folded pants");

                nativeDrop.transform.SetParent(_basePrefab.transform, false);
                nativeDrop.AddComponent<BoxCollider>();
            }
            manager.GetMethod("RegisterArmor", Fields).Invoke(null, new object[] { definition });

            var item = registered[identity];
            var shared = item.ItemDrop.m_itemData.m_shared;

            Check(item.ItemPrefab != definition && item.ItemPrefab != _basePrefab, "Jotunn creates an independent gameplay clone.");
            if (category == "Legs")
            {
                Check(item.ItemPrefab.transform.Find("attach_skin") != null, "Native pants attachment survives cloning.");
            }
            else
            {
                ValidateCustomClothing(manager, item, category);
            }
            if (category == "Chest")
            {
                ValidateTunicFasteners(item, mod);
            }

            Check(item.ItemPrefab.GetComponent<HexBattleMageVitalityProfile>().MaximumHealth == 2f, "Armor registration retains the authored flat health bonus.");
            Check(shared.m_itemType == nativeType, "Native equipment slot survives cloning.");
            Check(shared.m_armor == 2f && shared.m_movementModifier == 0f && shared.m_setSize == 0 && shared.m_setName == string.Empty, "Starter stats replace inherited Fenris power.");
            Check(baseline.m_itemData.m_shared.m_armor == 10f && baseline.m_itemData.m_shared.m_setSize == 3, "Vanilla baseline stats remain unchanged.");
            Check(item.ItemPrefab.GetComponent<MageEitrProfile>().MaximumEitr == 5f, "Authored flat Eitr is installed.");
            Check(item.ItemPrefab.GetComponent<MageArmorStyleProfile>().Styles.Length == (category == "Helmet" ? 4 : 1), "Named baked style recovery works for cloned armor.");
            if (category == "Helmet")
            {
                ValidateHelmetStyles(item, mod, harmony);
            }
            Check(item.Recipe.Recipe.m_resources.Length == 2 && item.Recipe.Recipe.m_craftingStation.name.EndsWith("HexArcaneWorkbench", StringComparison.Ordinal), "Armor recipe targets the Mage Table.");
            Check(shared.m_maxQuality == 4, "Hood, tunic and trousers support four qualities.");

            for (int quality = 1; quality <= 4; quality++)
            {
                Check(item.ItemDrop.m_itemData.GetArmor(quality, 0f) == quality * 2f, "Native armor scales 2/4/6/8.");
                Check(item.Recipe.Recipe.GetRequiredStationLevel(quality) == quality, "Native Mage Table requirements scale 1/2/3/4.");
                Check(item.ItemPrefab.GetComponent<MageEitrProfile>().GetMaximumEitr(quality) == 5f, "Quality does not increase armor Eitr.");
            }

            var resources = item.Recipe.Recipe.m_resources;
            var hide = resources.Single(resource => resource.m_resItem.name.EndsWith("DeerHide", StringComparison.Ordinal));
            var resin = resources.Single(resource => resource.m_resItem.name.EndsWith("Resin", StringComparison.Ordinal));

            Check(hide.GetAmount(1) == 6 && resin.GetAmount(1) == 2, "Base armor recipe is preserved.");
            Check(!resources.Any(resource => resource.m_resItem.name.EndsWith("BoneFragments", StringComparison.Ordinal)), "Meadows armor has no bone requirement.");

            for (int quality = 2; quality <= 4; quality++)
            {
                Check(hide.GetAmount(quality) == 3 && resin.GetAmount(quality) == 1, "Every armor upgrade uses fixed cheaper costs.");
            }

            Check(profile.Icon != null && profile.Icon.texture.isReadable && profile.Icon.texture.width <= 256, "Authored icon follows texture policy.");
            Check(profile.DisplayName.StartsWith("$") && profile.Description.StartsWith("$"), "Player-facing identity is localized.");

            if (category == "Chest")
            {
                Check(profile.ClearBodyArmorMaterial && shared.m_armorMaterial == null, "Custom tunic clears native chest overlay.");
                Check(profile.BodyTexture == null && !profile.ReplaceMeshTexture, "Custom mesh uses its authored fabric instead of the old leather texture override.");
            }
        }

        File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "armor-registration-report.txt"), "PASS: all three authored armor definitions register through actual Jotunn CreateClonedPrefab/AddItem/AddRecipe; native slots retained; custom hood/tunic visuals selected for both body types; native chest/hood visuals removed; both native attachment transpilers installed; 53-bone bindings and full sleeve reach checked; vanilla fixture unchanged; Fenris bonuses removed; +5 Eitr; localized identity; recipes and baked style recovery validated. Native item bootstraps are fixtures; actual movement remains an in-game check.");
    }

    private static void ValidateTunicFasteners(CustomItem item, Assembly mod)
    {
        var profile = item.ItemPrefab.GetComponent<HexBattleMageArmorProfile>();

        foreach (var fit in new[] { profile.MaleVisual, profile.FemaleVisual })
        {
            var renderer = fit.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(value => value.name == "Buttons");
            var mesh = renderer.sharedMesh;
            var triangles = mesh.GetTriangles(0);
            var adjacency = new Dictionary<int, HashSet<int>>();

            for (int index = 0; index < triangles.Length; index += 3)
            {
                for (int edge = 0; edge < 3; edge++)
                {
                    int a = triangles[index + edge];
                    int b = triangles[index + (edge + 1) % 3];

                    if (!adjacency.ContainsKey(a))
                    {
                        adjacency[a] = new HashSet<int>();
                    }

                    if (!adjacency.ContainsKey(b))
                    {
                        adjacency[b] = new HashSet<int>();
                    }

                    adjacency[a].Add(b);
                    adjacency[b].Add(a);
                }
            }

            var visited = new HashSet<int>();
            int groups = 0;

            foreach (int vertex in adjacency.Keys)
            {
                if (!visited.Add(vertex))
                {
                    continue;
                }

                groups++;
                var pending = new Stack<int>();

                pending.Push(vertex);

                while (pending.Count > 0)
                {
                    int current = pending.Pop();

                    foreach (int neighbor in adjacency[current])
                    {
                        if (visited.Add(neighbor))
                        {
                            pending.Push(neighbor);
                        }
                    }
                }
            }

            Check(groups == 4, "Each tunic fit has four geometric wooden buttons.");
            Check(renderer.bones.Length == 53 && mesh.bindposes.Length == 53, "Buttons follow native clothing skinning.");
            Check(mesh.boneWeights.All(weight => Mathf.Abs(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3 - 1f) < 0.001f), "Button skin weights are normalized.");
        }

        var clone = Object.Instantiate(item.ItemPrefab);

        try
        {
            var model = clone.transform.Find("model");
            var buttons = model.Find("Buttons").GetComponent<Renderer>();
            var materials = buttons.sharedMaterials;
            var visualType = mod.GetType("HexBattleMage.Core.Visuals.MageArmorStyleVisual");

            visualType.GetMethod("Setup", Fields).Invoke(model.GetComponent(visualType), new object[] { 0 });
            Check(buttons.sharedMaterials.SequenceEqual(materials), "World-drop style changes preserve wooden fasteners and thread materials.");
        }
        finally
        {
            Object.DestroyImmediate(clone);
        }
    }
    private static void ValidateWorldDrops(AssetBundle bundle, Dictionary<string, CustomItem> registered, Assembly mod)
    {
        var previousMode = Physics.simulationMode;
        Vector3 previousGravity = Physics.gravity;
        var objects = new List<GameObject>();
        var drops = new List<GameObject>();
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);

        floor.name = "Solid ground drop fixture";
        floor.transform.position = Vector3.down * 0.125f;
        floor.transform.localScale = new Vector3(40f, 0.25f, 10f);

        int terrainLayer = LayerMask.NameToLayer("terrain");

        floor.layer = terrainLayer >= 0 ? terrainLayer : 0;

        try
        {
            Physics.simulationMode = SimulationMode.Script;
            Physics.gravity = Vector3.down * 9.81f;

            foreach (var item in registered.Values)
            {
                drops.Add(Object.Instantiate(item.ItemPrefab));
            }

            foreach (var prefab in bundle.LoadAllAssets<GameObject>())
            {
                var definition = prefab.GetComponent<MageEquipmentDefinition>();

                if (definition == null)
                {
                    continue;
                }

                if (definition.Category != MageEquipmentCategory.Wand && definition.Category != MageEquipmentCategory.Bracer && definition.Category != MageEquipmentCategory.Trinket)
                {
                    continue;
                }

                var clone = Object.Instantiate(prefab);

                clone.SetActive(false);

                if (definition.Category != MageEquipmentCategory.Wand)
                {
                    var collider = clone.AddComponent<BoxCollider>();
                    var profile = prefab.GetComponents<MonoBehaviour>().Single(component => component.GetType().GetField("DropColliderSize") != null);

                    collider.size = (Vector3)profile.GetType().GetField("DropColliderSize").GetValue(profile);

                    var center = profile.GetType().GetField("DropColliderCenter");

                    collider.center = center != null ? (Vector3)center.GetValue(profile) : Vector3.zero;
                }

                mod.GetType("HexBattleMage.Core.WorldDropPhysics").GetMethod("Configure", Fields).Invoke(null, new object[] { clone, definition.DropCollisionDetection });
                drops.Add(clone);
            }

            for (int index = 0; index < drops.Count; index++)
            {
                var drop = drops[index];

                objects.Add(drop);

                foreach (var behavior in drop.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    behavior.enabled = false;
                }

                drop.transform.position = new Vector3(-12f + index * 2f, 2f, 0f);
                drop.transform.rotation = Quaternion.Euler(20f, 15f, 5f);
                drop.SetActive(true);

                var body = drop.GetComponent<Rigidbody>();
                var colliders = drop.GetComponentsInChildren<Collider>().Where(collider => collider.enabled && !collider.isTrigger).ToArray();

                Check(body != null && !body.isKinematic && body.useGravity, "Every gear drop has a dynamic gravity-driven body.");
                Check(body.collisionDetectionMode == CollisionDetectionMode.ContinuousDynamic, "Gear drop uses authored continuous collision detection.");
                Check(colliders.Length > 0, "Every gear drop has an active non-trigger collider.");
                Check(colliders.Any(collider => !Physics.GetIgnoreLayerCollision(collider.gameObject.layer, floor.layer)), "Drop collision layer interacts with solid ground.");
                body.linearVelocity = Vector3.down * 12f;
            }

            Physics.SyncTransforms();

            for (int step = 0; step < 200; step++)
            {
                Physics.Simulate(0.02f);
            }

            foreach (var drop in drops)
            {
                float bottom = drop.GetComponentsInChildren<Collider>().Where(collider => collider.enabled && !collider.isTrigger).Min(collider => collider.bounds.min.y);

                Check(bottom > -0.04f, "Dropped gear settles on the floor instead of tunneling through: " + drop.name);
                Check(drop.GetComponent<Rigidbody>().linearVelocity.magnitude < 0.2f, "Gear drop comes to rest on the ground: " + drop.name);
            }

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "gear-drop-physics-report.txt"), "PASS: " + drops.Count + " gear drops have gravity-driven rigidbodies, solid colliders and continuous collision detection; all settle on solid ground after a 12 m/s downward drop. Armor/cloaks use actual registered prefabs; wand uses its authored prefab; bracer/trinket use their authored collider dimensions through the actual shared physics helper. Native world/network lifecycle is a fixture.");
        }
        finally
        {
            foreach (var item in objects)
            {
                Object.DestroyImmediate(item);
            }

            Object.DestroyImmediate(floor);
            Physics.gravity = previousGravity;
            Physics.simulationMode = previousMode;
        }
    }

    private static void ValidateHelmetStyles(CustomItem item, Assembly mod, Harmony harmony)
    {
        var styles = item.ItemPrefab.GetComponent<MageArmorStyleProfile>();
        var profile = item.ItemPrefab.GetComponent<HexBattleMageArmorProfile>();

        Check(item.ItemDrop.m_itemData.m_shared.m_variants == 4 && item.ItemDrop.m_itemData.m_shared.m_icons.Length == 4, "Native style dialog receives four icons and variants.");

        foreach (var source in new[] { profile.MaleVisual, profile.FemaleVisual, item.ItemPrefab.transform.Find("model").gameObject })
        {
            var clone = Object.Instantiate(source);
            var visual = clone.GetComponentsInChildren<MonoBehaviour>(true).Single(component => component.GetType().FullName == "HexBattleMage.Core.Visuals.MageArmorStyleVisual");

            for (int variant = 0; variant < styles.Styles.Length; variant++)
            {
                visual.GetType().GetMethod("Setup", Fields).Invoke(visual, new object[] { variant });

                foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
                {
                    Check(renderer.sharedMaterial == styles.Styles[variant].Material, "Selected color applies to each body fit and the world drop.");
                }
            }

            Object.DestroyImmediate(clone);
        }

        Patch(harmony, typeof(ZNetView), "Awake", "Skip");
        Patch(harmony, typeof(ZNetView), "OnDestroy", "Skip");
        Patch(harmony, typeof(ZDO), "IncreaseDataRevision", "Skip");
        Patch(harmony, typeof(ZNetView), "IsOwner", "HelmetOwner");

        foreach (string patch in new[] { "HelmetStylePublishPatch", "HelmetStyleRefreshPatch", "HelmetStyleAttachPatch" })
        {
            harmony.CreateClassProcessor(mod.GetType("HexBattleMage.Patches." + patch)).Patch();
        }

        var host = new GameObject("Helmet color network fixture");

        host.SetActive(false);

        var equipment = host.AddComponent<VisEquipment>();
        var humanoid = host.AddComponent<TrinketValidationPlayer>();
        var view = host.AddComponent<ZNetView>();
        var data = new ZDO();

        Set(typeof(ZDO), data, "m_uid", new ZDOID(987654321L, 1234U));
        Set(typeof(ZNetView), view, "m_zdo", data);
        Set(typeof(VisEquipment), equipment, "m_nview", view);
        Set(typeof(Humanoid), humanoid, "m_helmetItem", item.ItemDrop.m_itemData);

        var sync = mod.GetType("HexBattleMage.Patches.HelmetStyleSynchronization");
        var publish = sync.GetMethod("Publish", Fields);
        var read = sync.GetMethod("ReadVariant", Fields);
        var refresh = mod.GetType("HexBattleMage.Patches.HelmetStyleRefreshPatch").GetMethod("Prefix", Fields);
        var attach = mod.GetType("HexBattleMage.Patches.HelmetStyleAttachPatch").GetMethod("Prefix", Fields);
        int hash = item.GetHashCode();

        _helmetNetworkOwner = true;

        for (int variant = 0; variant < 4; variant++)
        {
            item.ItemDrop.m_itemData.m_variant = variant;
            publish.Invoke(null, new object[] { humanoid, equipment, false });
            Check(data.GetInt("HexBattleMage_HelmetVariant", -1) == variant, "Owner publishes the selected style into native ZDO data.");
            Check((int)read.Invoke(null, new object[] { equipment, hash }) == variant, "Native network data resolves the selected color.");
            Set(typeof(VisEquipment), equipment, "m_currentHelmetItemHash", hash);
            refresh.Invoke(null, new object[] { equipment, hash });
            Check((int)AccessTools.Field(typeof(VisEquipment), "m_currentHelmetItemHash").GetValue(equipment) == 0, "Color change requests native helmet reattachment.");

            var arguments = new object[] { equipment, hash, 0 };

            attach.Invoke(null, arguments);
            Check((int)arguments[2] == variant, "Native attachment receives the synchronized variant.");
        }

        _helmetNetworkOwner = false;
        item.ItemDrop.m_itemData.m_variant = 0;
        publish.Invoke(null, new object[] { humanoid, equipment, false });
        Check(data.GetInt("HexBattleMage_HelmetVariant", -1) == 3, "A non-owner cannot overwrite the owner's color.");
        Set(typeof(VisEquipment), equipment, "m_currentHelmetItemHash", hash + 1);
        refresh.Invoke(null, new object[] { equipment, hash + 1 });
        Check((int)AccessTools.Field(typeof(VisEquipment), "m_currentHelmetItemHash").GetValue(equipment) == hash + 1, "Unrelated helmet refresh is unchanged.");
        item.ItemDrop.m_itemData.m_variant = 0;
        _helmetNetworkOwner = true;
        Set(typeof(ZNetView), view, "m_zdo", null);
        Object.DestroyImmediate(host);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "helmet-style-report.txt"), "PASS: four native style entries; cloned male/female and drop visuals apply all colors; actual native ZDO stores and reads owner selection; non-owner writes are blocked; native helmet refresh and attachment receive variant; unrelated helmets pass through. Native world/ownership bootstrap is a fixture; live multiplayer remains an in-game check.");
    }

    private static bool HelmetOwner(ref bool __result)
    {
        __result = _helmetNetworkOwner;
        return false;
    }

    private static void ValidateCustomClothing(Type manager, CustomItem item, string category)
    {
        var profile = item.ItemPrefab.GetComponent<HexBattleMageArmorProfile>();
        var host = new GameObject("Custom clothing body selection");

        host.SetActive(false);

        var equipment = host.AddComponent<VisEquipment>();
        var resolve = manager.GetMethod("ResolveVisualPrefab", Fields);

        Check(profile.ReplaceNativeVisuals, "Custom visuals are authored.");
        Check(item.ItemPrefab.transform.Find("attach_skin") == null, "Native worn mesh is removed from the item.");
        Check(item.ItemPrefab.transform.Find("model").GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0, "Custom drop model is static.");

        for (int model = 0; model < 2; model++)
        {
            Set(typeof(VisEquipment), equipment, "m_currentModelIndex", model);

            var visual = (GameObject)resolve.Invoke(null, new object[] { item.ItemPrefab, equipment });
            var expected = model == 0 ? profile.MaleVisual : profile.FemaleVisual;
            var renderer = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);

            Check(visual == expected && visual.transform.Find("attach_skin") != null, "Native attachment lookup selects the correct body fit.");
            Check(renderer.bones.Length == 53 && renderer.sharedMesh.bindposes.Length == 53, "Custom clothing preserves native bone count and binding order.");
            Check(renderer.sharedMaterial.GetColor("_EmissionColor").maxColorComponent == 0f, "Plain cloth has no eye or trim emission.");
            Check(renderer.sharedMaterial.mainTexture.width <= 256 && ((Texture2D)renderer.sharedMaterial.mainTexture).isReadable, "Custom fabric follows texture policy.");

            foreach (var weight in renderer.sharedMesh.boneWeights)
            {
                Check(Mathf.Abs(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3 - 1f) < 0.001f, "Skin weights are normalized.");
            }

            if (category == "Chest")
            {
                foreach (string side in new[] { "LeftHand", "RightHand" })
                {
                    Vector3 wrist = renderer.bones.Single(bone => bone.name == side).position;

                    Check(renderer.sharedMesh.vertices.Any(vertex => Vector3.Distance(vertex, wrist) < 0.065f), "Tunic sleeve reaches the wrist.");
                }
            }
        }

        Check(resolve.Invoke(null, new object[] { _basePrefab, equipment }) == _basePrefab, "Unrelated native items pass through.");
        Object.DestroyImmediate(host);
    }

    private static void ValidateUpgrades(CustomItem item, Harmony harmony)
    {
        var mod = typeof(MageUpgradeProfile).Assembly;

        foreach (string name in new[] { "MageUpgradeStationPatch", "MageUpgradeCostPatch" })
        {
            harmony.CreateClassProcessor(mod.GetType("HexBattleMage.Patches." + name)).Patch();
        }

        var fixture = new GameObject("Native armor game fixture");

        fixture.SetActive(false);
        _gameFixture = fixture.AddComponent<Game>();
        harmony.Patch(AccessTools.PropertyGetter(typeof(Game), "instance"), prefix: new HarmonyMethod(typeof(CloakRegistrationValidation), "GameInstance"));
        var recipe = item.Recipe.Recipe;
        var mockItem = recipe.m_item;

        recipe.m_item = item.ItemDrop;
        var stationLevel = typeof(Recipe).GetMethod("GetRequiredStationLevel", Fields);
        Check((int)stationLevel.Invoke(recipe, new object[] { 1 }) == 1, "Initial craft still requires table level one.");
        Check((int)stationLevel.Invoke(recipe, new object[] { 2 }) == 3 && (int)stationLevel.Invoke(recipe, new object[] { 3 }) == 3, "Both upgrades require table level three; authored=" + item.ItemPrefab.GetComponent<MageUpgradeProfile>().RequiredStationLevel + "; native=" + stationLevel.Invoke(recipe, new object[] { 2 }) + "/" + stationLevel.Invoke(recipe, new object[] { 3 }));
        Check(item.ItemDrop.m_itemData.m_shared.m_maxQuality == 3, "Two upgrades cap quality at three.");

        for (int quality = 1; quality <= 3; quality++)
        {
            var data = item.ItemDrop.m_itemData;

            Check(data.GetArmor(quality, 0f) == quality + 1, "Native armor scales 2/3/4.");
            Check(item.ItemPrefab.GetComponent<MageEitrProfile>().GetMaximumEitr(quality) == quality + 4, "Authored Eitr scales 5/6/7.");

            foreach (var resource in recipe.m_resources)
            {
                int expected = resource.m_resItem.name.EndsWith("LeatherScraps", StringComparison.Ordinal) ? 3 : 2;

                Check((int)typeof(Piece.Requirement).GetMethod("GetAmount", Fields).Invoke(resource, new object[] { quality }) == (quality == 1 ? resource.m_amount : expected), "Each upgrade uses fixed half-cloth and two Resin costs.");
            }
        }

        var vanilla = ScriptableObject.CreateInstance<Recipe>();
        var vanillaRequirement = new Piece.Requirement { m_amount = 6, m_amountPerLevel = 3 };

        Check(vanilla.GetRequiredStationLevel(2) == 2 && vanillaRequirement.GetAmount(3) == 6, "Vanilla station and cost progression remain unchanged.");
        Object.DestroyImmediate(vanilla);
        recipe.m_item = mockItem;
        Object.DestroyImmediate(fixture);
    }

    private static void ValidateStyleVisibility(CustomItem item, Harmony harmony)
    {
        var mod = typeof(MageArmorStyleProfile).Assembly;
        var selection = mod.GetType("HexBattleMage.Patches.WandStyleSelection");
        var stationMethod = selection.GetMethod("IsAtMageTable", Fields);

        harmony.Patch(stationMethod, prefix: new HarmonyMethod(typeof(CloakRegistrationValidation), "AtMageTable"));
        Patch(harmony, typeof(InventoryGui), "Awake", "Skip");
        var root = new GameObject("Style visibility fixture");

        root.SetActive(false);
        var gui = root.AddComponent<InventoryGui>();
        var button = new GameObject("Style button", typeof(RectTransform), typeof(UnityEngine.UI.Button));

        button.transform.SetParent(root.transform, false);
        gui.m_variantButton = button.GetComponent<UnityEngine.UI.Button>();
        var recipe = ScriptableObject.CreateInstance<Recipe>();

        recipe.m_item = item.ItemDrop;
        var field = AccessTools.Field(typeof(InventoryGui), "m_selectedRecipe");
        var pair = System.Activator.CreateInstance(field.FieldType, Fields, null, new object[] { recipe, item.ItemDrop.m_itemData, null, true }, null);

        field.SetValue(gui, pair);
        button.SetActive(false);
        mod.GetType("HexBattleMage.Patches.ArmorStyleButtonPatch").GetMethod("Postfix", Fields).Invoke(null, new object[] { gui, null });
        Check(button.activeSelf, "The native Style button appears for the recovered three-style cloak recipe.");
        harmony.Unpatch(stationMethod, HarmonyPatchType.Prefix, harmony.Id);
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(recipe);
    }

    private static bool GameInstance(ref Game __result)
    {
        __result = _gameFixture;
        return false;
    }

    private static bool AtMageTable(ref bool __result)
    {
        __result = true;
        return false;
    }

    private static void ValidateOwnedSelection(CustomItem item)
    {
        var root = new GameObject("Owned appearance fixture");

        root.SetActive(false);
        var gui = root.AddComponent<InventoryGui>();
        var player = root.AddComponent<TrinketValidationPlayer>();
        var recipe = ScriptableObject.CreateInstance<Recipe>();

        recipe.m_item = item.ItemDrop;
        var field = AccessTools.Field(typeof(InventoryGui), "m_selectedRecipe");
        var pair = Activator.CreateInstance(field.FieldType, Fields, null, new object[] { recipe, null, null, true }, null);

        field.SetValue(gui, pair);
        var selection = typeof(MageArmorStyleProfile).Assembly.GetType("HexBattleMage.Patches.ArmorStyleSelection");
        var find = selection.GetMethod("FindOwnedItem", Fields);
        var owned = item.ItemDrop.m_itemData.Clone();

        owned.m_shared = new ItemDrop.ItemData.SharedData { m_name = "Copied instance stats" };
        owned.m_dropPrefab = item.ItemPrefab;
        owned.m_equipped = true;
        player.GetInventory().GetAllItems().Add(owned);
        object[] arguments = { gui, player, recipe, false };

        Check(find.Invoke(null, arguments) == owned && !(bool)arguments[3], "Owned cloak matches by prefab despite a separate shared-stats instance.");
        var second = owned.Clone();

        second.m_equipped = false;
        owned.m_equipped = false;
        player.GetInventory().GetAllItems().Add(second);
        arguments[3] = false;
        Check(find.Invoke(null, arguments) == null && (bool)arguments[3], "Multiple unequipped cloaks require an explicit target.");
        owned.m_equipped = true;
        arguments[3] = false;
        Check(find.Invoke(null, arguments) == owned && !(bool)arguments[3], "Equipped matching cloak takes priority.");
        pair = Activator.CreateInstance(field.FieldType, Fields, null, new object[] { recipe, second, null, true }, null);

        field.SetValue(gui, pair);
        arguments[3] = false;
        Check(find.Invoke(null, arguments) == second, "Explicit upgrade selection takes priority over the equipped cloak.");
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(recipe);
    }

    private static void ValidateClonedStyles(CustomItem item)
    {
        var root = new GameObject("Worn appearance clone fixture");

        root.SetActive(false);
        var cloak = item.ItemPrefab.GetComponent<HexBattleMageCloakProfile>();
        var profile = item.ItemPrefab.GetComponent<MageArmorStyleProfile>();
        var type = typeof(MageArmorStyleProfile).Assembly.GetType("HexBattleMage.Core.Visuals.MageArmorStyleVisual");

        foreach (var source in new[] { cloak.MaleVisual.transform.Find("attach_skin").gameObject, cloak.FemaleVisual.transform.Find("attach_skin").gameObject, item.ItemPrefab })
        {
            var clone = Object.Instantiate(source, root.transform, false);
            var visual = clone.GetComponentInChildren(type, true);
            var setup = type.GetMethod("Setup", Fields);
            var cloth = visual.GetComponentsInChildren<Renderer>(true).First(renderer => renderer.name == profile.RendererName);

            for (int index = 0; index < profile.Styles.Length; index++)
            {
                setup.Invoke(visual, new object[] { index });
                Check(cloth.sharedMaterial == profile.Styles[index].Material, "Instantiated worn/drop visual retains style profile and applies each chosen material.");
            }

            Object.DestroyImmediate(clone);
        }

        Object.DestroyImmediate(root);
    }

    private static void ValidateStyles(CustomItem item)
    {
        var profile = item.ItemPrefab.GetComponent<MageArmorStyleProfile>();
        var cloak = item.ItemPrefab.GetComponent<HexBattleMageCloakProfile>();

        Check(profile.Styles.Length == 3 && item.ItemDrop.m_itemData.m_shared.m_variants == 3, "Three authored styles are registered.");
        Check(item.ItemDrop.m_itemData.m_shared.m_icons.Distinct().Count() == 3, "Style picker uses three distinct authored icons.");

        foreach (var fit in new[] { cloak.MaleVisual, cloak.FemaleVisual, item.ItemPrefab.transform.Find("model").gameObject })
        {
            var visualType = typeof(MageArmorStyleProfile).Assembly.GetType("HexBattleMage.Core.Visuals.MageArmorStyleVisual");
            var visual = fit.GetComponentInChildren(visualType, true);
            var setup = visualType.GetMethod("Setup", Fields);
            var cloth = fit.GetComponentsInChildren<Renderer>(true).First(renderer => renderer.name == profile.RendererName);
            var clasp = fit.GetComponentsInChildren<Renderer>(true).First(renderer => renderer.name == "Clasp");
            var claspMaterial = clasp.sharedMaterial;

            for (int index = 0; index < profile.Styles.Length; index++)
            {
                setup.Invoke(visual, new object[] { index });
                Check(cloth.sharedMaterial == profile.Styles[index].Material, "Style applies to cloth on each fit and world model.");
                Check(clasp.sharedMaterial == claspMaterial, "Style leaves clasp unchanged.");
                Check(profile.Styles[index].Icon.texture.width <= 256 && profile.Styles[index].Icon.texture.isReadable, "Style icons preserve 256 maximum and Read/Write.");
            }

            setup.Invoke(visual, new object[] { 0 });
        }

        var helper = typeof(MageArmorStyleProfile).Assembly.GetType("HexBattleMage.Core.GearTooltip").GetMethod("InsertPerks", Fields);
        string tooltip = "$description\n$item_durability: 400\n$item_weight: 4\n$item_armor: 2\n$inventory_dmgmod: Resistant";
        string result = (string)helper.Invoke(null, new object[] { item.ItemDrop.m_itemData, tooltip, "Maximum Eitr: +5" });

        Check(result.IndexOf("Maximum Eitr", StringComparison.Ordinal) > result.IndexOf("$inventory_dmgmod", StringComparison.Ordinal), "Eitr follows resistance.");
        Check(result.IndexOf("Maximum Eitr", StringComparison.Ordinal) < result.IndexOf("$item_durability", StringComparison.Ordinal), "Eitr precedes durability and weight.");
        var trinket = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData { m_description = "$trinket_description" } };
        string trinketText = (string)helper.Invoke(null, new object[] { trinket, "$trinket_description\n$item_weight: 1", "Passive Effects\nMaximum Eitr: +25" });

        Check(trinketText.IndexOf("Passive Effects", StringComparison.Ordinal) < trinketText.IndexOf("$item_weight", StringComparison.Ordinal), "Trinket perks precede generic item stats.");
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

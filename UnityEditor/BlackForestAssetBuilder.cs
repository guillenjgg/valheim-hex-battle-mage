using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BlackForestAssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage";
    private const string Folder = Root + "/Progression/BlackForest";

    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        PrepareBlight();
        PrepareArmor("Helmet");
        PrepareArmor("Chest");
        PrepareArmor("Legs");
        PrepareCloak();
        PrepareStaff();
        ImproveStaffIcon();
        PrepareBoulder();
        AssetDatabase.SaveAssets();
        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }

    private static void PrepareBlight()
    {
        string path = Root + "/Elements/HexWandElementBlight.prefab";

        if (!File.Exists(path))
        {
            var root = new GameObject("HexWandElementBlight");
            var profile = root.AddComponent<WandElementProfile>();

            profile.Element = WandElement.Blight;
            profile.Duration = 3f;
            profile.TickInterval = 0.5f;
            profile.TickDamage = 10f;
            profile.TotalDamage = 60f;
            profile.SingleTargetProjectile = true;
            profile.DirectDamage = 0f;
            profile.DamagePerQuality = 0f;
            profile.Spreads = true;
            profile.SpreadRadius = 0f;
            profile.RequireSpreadLineOfSight = true;
            profile.MaximumChainTargets = 128;
            profile.StatusDisplayName = "$hexbattlemage_blight";
            profile.TipColor = new Color(0.55f, 0.12f, 0.8f);
            profile.ImpactColor = new Color(0.65f, 0.15f, 0.9f);
            profile.AuthoredVersion = 2;
            SaveNew(root, path);
        }

        var blightRoot = PrefabUtility.LoadPrefabContents(path);

        try
        {
            var profile = blightRoot.GetComponent<WandElementProfile>();

            if (profile.AuthoredVersion < 2)
            {
                profile.SingleTargetProjectile = true;
                profile.AuthoredVersion = 2;
                PrefabUtility.SaveAsPrefabAsset(blightRoot, path);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(blightRoot);
        }

        string attunementPath = Folder + "/HexBattleMage_Wand_Attunement_BlackForest_01.prefab";

        if (!File.Exists(attunementPath))
        {
            var root = new GameObject("HexBattleMage_Wand_Attunement_BlackForest_01");
            var definition = root.AddComponent<HexBattleMageWandAttunementProfile>();

            definition.Element = WandElement.Blight;
            definition.DisplayName = "$hexbattlemage_attunement_blight";
            definition.Description = "$hexbattlemage_attunement_blight_description";
            Recipe(root, attunementPath, HexBattleMageRuneBlockProfile.PrefabName, 2, new[] { Resource("TrophySkeleton", 1), Resource("SurtlingCore", 1), Resource("MushroomYellow", 5) });
            SaveNew(root, attunementPath);
        }

        string wandPath = Root + "/Wands/Meadows/Wand_Meadows_01/HexBattleMage_Wand_Meadows_01.prefab";
        var wand = PrefabUtility.LoadPrefabContents(wandPath);

        try
        {
            var profile = wand.GetComponent<HexBattleMageWandProfile>();

            if (!profile.Elements.Any(element => element.Element == WandElement.Blight))
            {
                profile.Elements = profile.Elements.Concat(new[] { AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<WandElementProfile>() }).ToArray();
                PrefabUtility.SaveAsPrefabAsset(wand, wandPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(wand);
        }

        string tablePath = Root + "/Crafting/RuneBlock/HexBattleMage_RuneBlock.prefab";
        var table = PrefabUtility.LoadPrefabContents(tablePath);

        try
        {
            var profile = table.GetComponent<HexBattleMageRuneBlockProfile>();
            var attunement = AssetDatabase.LoadAssetAtPath<GameObject>(attunementPath);

            if (profile.PreviewCatalogVersion < 1)
            {
                profile.AttunementPreviews = new[]
                {
                    new AttunementPreview { Id = "Spirit", DisplayName = "$hexbattlemage_element_spirit" },
                    new AttunementPreview { Id = "Frost", DisplayName = "$hexbattlemage_element_frost" },
                    new AttunementPreview { Id = "Earth", DisplayName = "$hexbattlemage_element_earth" }
                };
                profile.PreviewCatalogVersion = 1;
            }

            if (!profile.Attunements.Contains(attunement))
            {
                profile.Attunements = profile.Attunements.Concat(new[] { attunement }).ToArray();
            }

            PrefabUtility.SaveAsPrefabAsset(table, tablePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(table);
        }
    }

    private static void PrepareArmor(string category)
    {
        string identity = "HexBattleMage_" + category + "_BlackForest_01";
        string path = Folder + "/" + identity + "_Definition.prefab";

        if (File.Exists(path))
        {
            return;
        }

        string sourcePath = Root + "/Armor/Meadows/" + category + "_Meadows_01/HexBattleMage_" + category + "_Meadows_01_Definition.prefab";
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));
        var profile = root.GetComponent<HexBattleMageArmorProfile>();

        root.name = identity + "_Definition";
        profile.ItemPrefabName = identity;
        profile.DisplayName = "$item_hexbattlemage_" + category.ToLowerInvariant() + "_blackforest_01";
        profile.Description = "$item_hexbattlemage_blackforest_armor_description";
        profile.Armor = 6f;
        profile.ArmorPerQuality = 2f;
        profile.MaximumQuality = 4;
        root.GetComponent<MageEitrProfile>().MaximumEitr = 8f;
        root.GetComponent<MageEitrProfile>().MaximumEitrPerQuality = 0f;
        root.GetComponent<HexBattleMageVitalityProfile>().MaximumHealth = 4f;
        Catalog(root);
        root.GetComponent<MageUpgradeProfile>().UseFixedStationLevel = false;
        root.GetComponent<MageUpgradeProfile>().FixedResourceCosts = true;
        var styles = root.GetComponent<MageArmorStyleProfile>();

        styles.BakedStyles = null;
        Recipe(root, path, "HexArcaneWorkbench", 2, ArmorResources());
        ArmorStyleAssetPreparation.Prepare(root, path);
        SaveNew(root, path);
    }

    private static void PrepareCloak()
    {
        string identity = "HexBattleMage_Cloak_BlackForest_01";
        string path = Folder + "/" + identity + ".prefab";

        if (File.Exists(path))
        {
            return;
        }

        var source = AssetDatabase.FindAssets("t:Prefab", new[] { Root }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<GameObject>).First(asset => asset.GetComponent<HexBattleMageCloakProfile>()?.ItemPrefabName == "HexBattleMage_Cloak_Meadows_01");
        var root = Object.Instantiate(source);
        var profile = root.GetComponent<HexBattleMageCloakProfile>();

        root.name = identity;
        profile.ItemPrefabName = identity;
        profile.DisplayName = "$item_hexbattlemage_cloak_blackforest_01";
        profile.Description = "$item_hexbattlemage_blackforest_armor_description";
        profile.Armor = 4f;
        profile.MaximumQuality = 3;
        root.GetComponent<MageEitrProfile>().MaximumEitr = 8f;
        root.GetComponent<MageEitrProfile>().MaximumEitrPerQuality = 0f;
        root.GetComponent<HexBattleMageVitalityProfile>().MaximumStamina = 8f;
        Catalog(root);
        var upgrades = root.GetComponent<MageUpgradeProfile>();

        upgrades.UseFixedStationLevel = false;
        upgrades.FixedResourceCosts = true;
        root.GetComponent<MageArmorStyleProfile>().BakedStyles = null;
        Recipe(root, path, "HexArcaneWorkbench", 2, ArmorResources());
        ArmorStyleAssetPreparation.Prepare(root, path);
        SaveNew(root, path);
    }

    private static void PrepareStaff()
    {
        string path = Folder + "/HexBattleMage_Staff_BlackForest_01_Definition.prefab";

        if (File.Exists(path))
        {
            return;
        }

        var wood = Material("EarthStaffWood", new Color(0.28f, 0.17f, 0.08f));
        var stone = Material("EarthStaffStone", new Color(0.36f, 0.39f, 0.32f));
        var cord = Material("EarthStaffBinding", new Color(0.14f, 0.12f, 0.08f));
        var model = new GameObject("EarthStaffModel");

        Primitive(model, "CoreWoodShaft", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.3f), new Vector3(0.045f, 0.83f, 0.045f), new Vector3(90f, 0f, 0f), wood);
        Primitive(model, "FacetedStone", PrimitiveType.Sphere, new Vector3(0f, 0f, 1.1f), new Vector3(0.19f, 0.17f, 0.25f), Vector3.zero, stone);

        for (int index = 0; index < 4; index++)
        {
            Primitive(model, "LeatherBinding" + index, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.85f + index * 0.035f), new Vector3(0.065f, 0.012f, 0.065f), new Vector3(90f, 0f, 0f), cord);
        }

        var modelPrefab = PrefabUtility.SaveAsPrefabAsset(model, Folder + "/EarthStaffModel.prefab");

        Object.DestroyImmediate(model);
        var projectile = new GameObject("EarthStaffRock");

        Primitive(projectile, "Rock", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.24f, Vector3.zero, stone);
        var projectilePrefab = PrefabUtility.SaveAsPrefabAsset(projectile, Folder + "/EarthStaffRock.prefab");

        Object.DestroyImmediate(projectile);
        var root = new GameObject("HexBattleMage_Staff_BlackForest_01_Definition");
        var profile = root.AddComponent<HexBattleMageStaffProfile>();
        var definition = root.AddComponent<MageEquipmentDefinition>();

        definition.Category = MageEquipmentCategory.Staff;
        definition.Slot = MageEquipmentSlot.MainHand;
        Catalog(root);
        profile.WornModel = modelPrefab;
        profile.ProjectileModel = projectilePrefab;
        profile.Icon = (Sprite)typeof(GearIconAssetBuilder).GetMethod("Render", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { modelPrefab, Folder + "/EarthStaffIcon.png", null, string.Empty });
        var upgrades = root.AddComponent<MageUpgradeProfile>();

        upgrades.FixedResourceCosts = true;
        Recipe(root, path, HexBattleMageRuneBlockProfile.PrefabName, 2, new[] { Resource("RoundLog", 10, 5), Resource("Stone", 10, 5), Resource("Bronze", 4, 2) });
        SaveNew(root, path);
    }

    private static void PrepareBoulder()
    {
        string path = Folder + "/HexBattleMage_Staff_BlackForest_01_Definition.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            var profile = root.GetComponent<HexBattleMageStaffProfile>();

            if (profile.BoulderVersion >= 1)
            {
                return;
            }

            profile.ProjectileVisualScale = 2.75f;
            profile.ProjectileCollisionRadius = 0.22f;
            profile.SuppressNativeCinders = true;
            string modelPath = AssetDatabase.GetAssetPath(profile.ProjectileModel);
            var model = PrefabUtility.LoadPrefabContents(modelPath);

            try
            {
                float goldenRatio = (1f + Mathf.Sqrt(5f)) * 0.5f;
                var vertices = new[]
                {
                    new Vector3(-1f, goldenRatio, 0f), new Vector3(1f, goldenRatio, 0f), new Vector3(-1f, -goldenRatio, 0f), new Vector3(1f, -goldenRatio, 0f),
                    new Vector3(0f, -1f, goldenRatio), new Vector3(0f, 1f, goldenRatio), new Vector3(0f, -1f, -goldenRatio), new Vector3(0f, 1f, -goldenRatio),
                    new Vector3(goldenRatio, 0f, -1f), new Vector3(goldenRatio, 0f, 1f), new Vector3(-goldenRatio, 0f, -1f), new Vector3(-goldenRatio, 0f, 1f)
                };
                var uv = new Vector2[vertices.Length];

                for (int index = 0; index < vertices.Length; index++)
                {
                    var normal = vertices[index].normalized;

                    vertices[index] = normal * (0.5f + 0.035f * Mathf.Sin(index * 2.1f));
                    uv[index] = new Vector2(0.5f + Mathf.Atan2(normal.z, normal.x) / (2f * Mathf.PI), 0.5f - Mathf.Asin(normal.y) / Mathf.PI);
                }

                var mesh = new Mesh { name = "EarthBoulder", vertices = vertices, uv = uv, triangles = new[] { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8, 3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 } };

                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, Folder + "/EarthBoulder.asset");
                model.GetComponentInChildren<MeshFilter>().sharedMesh = mesh;
                PrefabUtility.SaveAsPrefabAsset(model, modelPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(model);
            }

            profile.BoulderVersion = 1;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ImproveStaffIcon()
    {
        string path = Folder + "/HexBattleMage_Staff_BlackForest_01_Definition.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            var profile = root.GetComponent<HexBattleMageStaffProfile>();

            if (profile.Icon != null && profile.Icon.name == "EarthStaffIcon04")
            {
                return;
            }

            var stone = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/EarthStaffStone.mat");
            string texture = AssetDatabase.FindAssets("CrackedFieldstone_BaseColor t:Texture2D", new[] { Root }).Select(AssetDatabase.GUIDToAssetPath).First();

            stone.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texture);
            stone.color = new Color(0.75f, 0.78f, 0.7f);
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/EarthStaffWood.mat");

            wood.color = new Color(0.75f, 0.6f, 0.45f);
            EditorUtility.SetDirty(wood);
            EditorUtility.SetDirty(stone);
            var preview = new GameObject("StaffIconPose");
            var pose = Object.Instantiate(profile.WornModel, preview.transform, false);

            try
            {
                pose.transform.localRotation = Quaternion.Euler(35f, 75f, 0f);
                profile.Icon = (Sprite)typeof(GearIconAssetBuilder).GetMethod("Render", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { preview, Folder + "/EarthStaffIcon04.png", null, string.Empty });
            }
            finally
            {
                Object.DestroyImmediate(preview);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void Primitive(GameObject root, string name, PrimitiveType type, Vector3 position, Vector3 scale, Vector3 rotation, Material material)
    {
        var part = GameObject.CreatePrimitive(type);

        part.name = name;
        part.transform.SetParent(root.transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.transform.localEulerAngles = rotation;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(part.GetComponent<Collider>());
    }

    private static Material Material(string name, Color color)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
        {
            return material;
        }

        var source = AssetDatabase.FindAssets("t:Material", new[] { Root + "/Crafting" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).First();

        material = new Material(source) { name = name, color = color };
        material.SetColor("_EmissionColor", Color.black);
        material.SetFloat("_Glossiness", 0f);
        material.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Recipe(GameObject root, string prefabPath, string station, int level, MageRecipeResource[] resources)
    {
        var recipe = root.GetComponent<MageRecipeProfile>() ?? root.AddComponent<MageRecipeProfile>();

        recipe.CraftingStation = station;
        recipe.RepairStation = station;
        recipe.MinimumStationLevel = level;
        recipe.Resources = resources;
        string path = Path.GetDirectoryName(prefabPath).Replace('\\', '/') + "/Recipe_" + root.name + ".json";

        File.WriteAllText(path, recipe.GetBakedData());
        AssetDatabase.ImportAsset(path);
        recipe.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
    }

    private static MageRecipeResource[] ArmorResources()
    {
        return new[] { Resource("TrollHide", 5, 2), Resource("DeerHide", 4, 2), Resource("Bronze", 2, 1) };
    }

    private static MageRecipeResource Resource(string name, int amount, int upgrade = 0)
    {
        return new MageRecipeResource { ItemPrefab = name, Amount = amount, AmountPerQuality = upgrade };
    }

    private static void Catalog(GameObject root)
    {
        var equipment = root.GetComponent<MageEquipmentDefinition>();

        equipment.ProgressionTier = "BlackForest";
        equipment.CatalogBiome = "BlackForest";
        equipment.CatalogRequiresBiomeDiscovery = false;
        equipment.CatalogBossDefeatKey = "defeated_eikthyr";
    }

    private static void SaveNew(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
    }
}

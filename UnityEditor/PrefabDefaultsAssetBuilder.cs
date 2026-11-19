using System;
using System.IO;
using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PrefabDefaultsAssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/";
    private const string WandPath = Root + "Wands/Meadows/Wand_Meadows_01/HexBattleMage_Wand_Meadows_01.prefab";
    private const string ProjectilePath = Root + "Projectiles/HexMagicBolt.prefab";
    private const string BracerPath = Root + "Shields/Apprentice/HexMagicBracerVisual.prefab";
    private const string TablePath = Root + "Crafting/HexArcaneWorkbench.prefab";
    private const string RequestPath = "Temp/HexPrefabDefaults.request";
    private const string ReportPath = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/prefab-defaults-result.txt";
    private const string BundlePath = "Assets/AssetBundles/hexbattlemage";
    private const string Destination = "C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/hexbattlemage";

    static PrefabDefaultsAssetBuilder()
    {
        if (File.Exists(RequestPath))
        {
            EditorApplication.delayCall += RunRequest;
        }
    }

    private static void RunRequest()
    {
        File.Delete(RequestPath);
        PrepareAndBuild();
    }

    [MenuItem("HexBattleMage/Prepare Authored Defaults and Build Bundle")]
    public static void PrepareAndBuild()
    {
        try
        {
            PrepareAuthoredDefaults();
            HexBattleMageBundlePolicy.ValidateDependencies();

            var manifest = BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

            if (manifest == null)
            {
                throw new InvalidOperationException("Authored defaults bundle build failed.");
            }

            File.Copy(BundlePath, Destination, true);
            File.WriteAllText(ReportPath, "PASS: authored wand, projectile, bracer, table and five elemental profiles saved and bundled. Existing profile values and visual geometry preserved.");
        }
        catch (Exception exception)
        {
            File.WriteAllText(ReportPath, exception.ToString());
            throw;
        }
    }

    public static void PrepareAuthoredDefaults()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();

        if (stage != null && stage.scene.isDirty)
        {
            throw new InvalidOperationException("Save the open prefab before preparing authored defaults.");
        }

        foreach (WandElement element in Enum.GetValues(typeof(WandElement)))
        {
            Edit(Root + "Elements/HexWandElement" + element + ".prefab", MigrateElement);
        }

        Edit(ProjectilePath, prefab =>
        {
            var profile = GetOrAdd<WandProjectileProfile>(prefab);
            var projectile = prefab.GetComponent<Projectile>();

            projectile.m_aoe = profile.AreaOfEffectRadius;
            projectile.m_rayRadius = profile.CollisionRadius;
            projectile.m_onlyStopOnTerrain = profile.OnlyStopOnTerrain;
        });

        Edit(WandPath, prefab =>
        {
            bool firstMigration = prefab.GetComponent<HexBattleMageWandProfile>() == null;
            var profile = GetOrAdd<HexBattleMageWandProfile>(prefab);

            if (firstMigration)
            {
                profile.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);
                profile.Elements = new WandElementProfile[Enum.GetValues(typeof(WandElement)).Length];

                foreach (WandElement element in Enum.GetValues(typeof(WandElement)))
                {
                    profile.Elements[(int)element] = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Elements/HexWandElement" + element + ".prefab").GetComponent<WandElementProfile>();
                }

                string materials = Root + "Transmogs/Wands/Apprentice/Materials/";

                profile.StyleMaterials = new[] { AssetDatabase.LoadAssetAtPath<Material>(materials + "ApprenticeBranch.mat"), AssetDatabase.LoadAssetAtPath<Material>(materials + "ApprenticeDarkBark.mat"), AssetDatabase.LoadAssetAtPath<Material>(materials + "ApprenticeBirch.mat") };
            }

            if (prefab.GetComponent<MageEquipmentDefinition>() == null)
            {
                var definition = prefab.AddComponent<MageEquipmentDefinition>();

                definition.Category = MageEquipmentCategory.Wand;
                definition.Slot = MageEquipmentSlot.MainHand;
            }

            AddRecipe(prefab, new[] { Resource("Wood", 20), Resource("Resin", 2) });

            var shared = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;

            if (firstMigration)
            {
                shared.m_name = "$item_hexbattlemage_wand_meadows_01";
                shared.m_description = "$item_hexbattlemage_wand_meadows_01_description";
                shared.m_animationState = ItemDrop.ItemData.AnimationState.OneHanded;
            }

            shared.m_damages.m_blunt = profile.WandBluntDamage;
            shared.m_damages.m_pierce = profile.WandPierceDamage;
            shared.m_variants = profile.StyleMaterials.Length;

            if (shared.m_icons.Length == 0)
            {
                throw new InvalidOperationException("The wand must have an authored inventory icon.");
            }

            if (shared.m_icons.Length != shared.m_variants)
            {
                var icons = new Sprite[shared.m_variants];

                for (int variant = 0; variant < icons.Length; variant++)
                {
                    icons[variant] = shared.m_icons[Math.Min(variant, shared.m_icons.Length - 1)];
                }

                shared.m_icons = icons;
            }

            shared.m_attack.m_attackAnimation = profile.AttackAnimation;
            shared.m_attack.m_attackChainLevels = profile.AttackChainLevels;
            shared.m_attack.m_attackRandomAnimations = profile.RandomAttackAnimations;
            shared.m_attack.m_attackProjectile = profile.ProjectilePrefab;
            shared.m_attack.m_projectileVel = profile.ProjectileVelocity;
            shared.m_attack.m_projectileAccuracy = profile.ProjectileAccuracy;
            shared.m_attack.m_projectileAccuracyMin = profile.ProjectileAccuracyMin;
            shared.m_attack.m_skillAccuracy = profile.SkillAccuracy;

            var light = prefab.transform.Find("attach/WandModel/WandTip").GetComponent<Light>();

            if (light != null)
            {
                if (firstMigration)
                {
                    light.type = LightType.Point;
                    light.shadows = LightShadows.None;
                    light.renderMode = LightRenderMode.ForcePixel;
                }

                light.range = profile.WandLightRange;
                light.intensity = profile.WandLightIntensity;
                light.color = profile.LightColor;
            }
        });

        Edit(BracerPath, prefab =>
        {
            GetOrAdd<MagicBracerProfile>(prefab);
            AddRecipe(prefab, new[] { Resource("Wood", 10, 5), Resource("Resin", 4, 2), Resource("LeatherScraps", 2, 2) });
        });

        Edit(TablePath, prefab =>
        {
            var profile = GetOrAdd<MageTableProfile>(prefab);

            AddRecipe(prefab, new[] { Resource("Wood", 10), Resource("Stone", 5) });
            var station = prefab.GetComponent<CraftingStation>();
            var piece = prefab.GetComponent<Piece>();

            station.m_showBasicRecipies = profile.ShowBasicRecipes;
            station.m_name = profile.DisplayName;
            piece.m_name = profile.DisplayName;
            piece.m_description = profile.Description;
        });

        MeadowsArmorAssetBuilder.Prepare();
        TrinketAssetPreparation.PrepareAll();
        WorldDropAssetPreparation.PrepareAll();
        ArmorStyleAssetPreparation.PrepareAll();
        AssetDatabase.SaveAssets();
    }

    private static void MigrateElement(GameObject prefab)
    {
        var profile = prefab.GetComponent<WandElementProfile>();

        if (profile.AuthoredVersion >= 1)
        {
            return;
        }

        profile.StatusDisplayName = "$se_hexbattlemage_wand_" + profile.Element.ToString().ToLowerInvariant();
        profile.Spreads = profile.Element == WandElement.Spirit || profile.Element == WandElement.Poison;
        profile.StackDamage = profile.Element == WandElement.Poison;
        profile.Stuns = profile.Element == WandElement.Lightning;

        switch (profile.Element)
        {
            case WandElement.Fire:
                profile.TipColor = Color.red;
                profile.TipEmissionStrength = 2f;
                profile.ImpactColor = new Color(1f, 0.3f, 0.04f);
                profile.NativeVisualStatus = "Burning";
                break;
            case WandElement.Frost:
                profile.TipColor = new Color(0.35f, 0.8f, 1f);
                profile.ImpactColor = new Color(0.3f, 0.8f, 1f);
                break;
            case WandElement.Spirit:
                profile.TipColor = new Color(0.7f, 0.3f, 1f);
                profile.ImpactColor = new Color(0.65f, 0.5f, 1f);
                break;
            case WandElement.Lightning:
                profile.TipColor = new Color(0.75f, 0.9f, 1f);
                profile.ImpactColor = new Color(0.5f, 0.6f, 1f);
                break;
            case WandElement.Poison:
                profile.TipColor = new Color(0.3f, 1f, 0.15f);
                profile.ImpactColor = new Color(0.4f, 1f, 0.12f);
                break;
        }

        profile.AuthoredVersion = 1;
    }

    private static MageRecipeResource Resource(string name, int amount, int upgradeAmount = 0)
    {
        return new MageRecipeResource { ItemPrefab = name, Amount = amount, AmountPerQuality = upgradeAmount };
    }

    private static void AddRecipe(GameObject prefab, MageRecipeResource[] resources)
    {
        var profile = prefab.GetComponent<MageRecipeProfile>();

        if (profile == null)
        {
            profile = prefab.AddComponent<MageRecipeProfile>();
            profile.Resources = resources;
        }

        const string recipeDirectory = Root + "Settings/Recipes";

        Directory.CreateDirectory(recipeDirectory);

        string path = recipeDirectory + "/Recipe_" + prefab.name + ".json";

        File.WriteAllText(path, profile.GetBakedData());
        AssetDatabase.ImportAsset(path);
        profile.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
    }

    private static T GetOrAdd<T>(GameObject prefab) where T : Component
    {
        var component = prefab.GetComponent<T>();

        return component != null ? component : prefab.AddComponent<T>();
    }

    private static void Edit(string path, Action<GameObject> edit)
    {
        var prefab = PrefabUtility.LoadPrefabContents(path);

        try
        {
            edit(prefab);
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }
}

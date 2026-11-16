using System;
using System.IO;
using HexBattleMage.Core.Elements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WandElementAssetBuilder
{
    private const string DirectoryPath = "Assets/_CustomItems/HexBattleMage/Elements";
    private const string RequestPath = "Temp/HexWandElements.request";
    private const string BundleDirectory = "Assets/AssetBundles";
    private const string BundleName = "hexbattlemage";
    private const string ReportPath = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/element-build-result.txt";
    private const string ModBundlePath = "C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/hexbattlemage";

    static WandElementAssetBuilder()
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

    [MenuItem("HexBattleMage/Prepare Wand Elements and Build Bundle")]
    public static void PrepareAndBuild()
    {
        try
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();

            if (stage != null && stage.scene.isDirty)
            {
                throw new InvalidOperationException("Save the open prefab before preparing wand elements.");
            }

            Directory.CreateDirectory(DirectoryPath);
            AssetDatabase.Refresh();

            foreach (WandElement element in Enum.GetValues(typeof(WandElement)))
            {
                string path = DirectoryPath + "/HexWandElement" + element + ".prefab";
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (existing != null)
                {
                    var existingProfile = existing.GetComponent<WandElementProfile>();

                    if (existingProfile == null || existingProfile.Element != element)
                    {
                        throw new InvalidOperationException("Invalid authored elemental profile: " + path);
                    }

                    AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant(BundleName, string.Empty);
                    continue;
                }

                var root = new GameObject("HexWandElement" + element);

                try
                {
                    var profile = root.AddComponent<WandElementProfile>();

                    profile.Element = element;
                    profile.Duration = 5f;

                    switch (element)
                    {
                        case WandElement.Fire:
                            profile.DirectDamage = 30f;
                            profile.TickDamage = 6f;
                            break;
                        case WandElement.Frost:
                            profile.DirectDamage = 20f;
                            profile.Duration = 4f;
                            profile.MovementSlow = 0.3f;
                            profile.AttackSlow = 0.3f;
                            break;
                        case WandElement.Spirit:
                            profile.DirectDamage = 10f;
                            profile.TickDamage = 8f;
                            profile.Duration = 8f;
                            profile.SpreadRadius = 8f;
                            break;
                        case WandElement.Lightning:
                            profile.DirectDamage = 25f;
                            profile.Duration = 1f;
                            break;
                        case WandElement.Poison:
                            profile.DirectDamage = 5f;
                            profile.TickDamage = 3f;
                            profile.Duration = 10f;
                            profile.SpreadRadius = 8f;
                            profile.MaximumStacks = 4;
                            profile.BossTickMultiplier = 0.25f;
                            break;
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant(BundleName, string.Empty);
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }

            AssetDatabase.SaveAssets();
            HexBattleMageBundlePolicy.ValidateDependencies();

            var manifest = BuildPipeline.BuildAssetBundles(BundleDirectory, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

            if (manifest == null)
            {
                throw new InvalidOperationException("Element bundle build failed.");
            }

            VerifyBundle();
            File.Copy(BundleDirectory + "/" + BundleName, ModBundlePath, true);
            File.WriteAllText(ReportPath, "PASS: five authored elemental profile prefabs and existing wand/bracer/table/projectile verified in production bundle. Bundle copied to mod source.");
        }
        catch (Exception exception)
        {
            File.WriteAllText(ReportPath, exception.ToString());
            throw;
        }
    }

    private static void VerifyBundle()
    {
        var bundle = AssetBundle.LoadFromFile(Path.GetFullPath(BundleDirectory + "/" + BundleName));

        if (bundle == null)
        {
            throw new InvalidOperationException("Cannot load elemental bundle.");
        }

        try
        {
            foreach (WandElement element in Enum.GetValues(typeof(WandElement)))
            {
                var prefab = bundle.LoadAsset<GameObject>("HexWandElement" + element);

                if (prefab == null)
                {
                    throw new InvalidOperationException("Missing elemental prefab: " + element);
                }

                var profile = prefab.GetComponent<WandElementProfile>();

                if (profile == null || profile.Element != element || profile.Duration <= 0f)
                {
                    throw new InvalidOperationException("Invalid elemental profile: " + element);
                }
            }

            foreach (string name in new[] { "HexBattleMageWand", "HexMagicBracerVisual", "HexMagicBolt", "HexArcaneWorkbench" })
            {
                if (bundle.LoadAsset<GameObject>(name) == null)
                {
                    throw new InvalidOperationException("Missing existing production prefab: " + name);
                }
            }
        }
        finally
        {
            bundle.Unload(true);
        }
    }
}

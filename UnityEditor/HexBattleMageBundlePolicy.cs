using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class HexBattleMageBundlePolicy
{
    private const string BundleName = "hexbattlemage";
    private const string ProjectilePath = "Assets/_CustomItems/HexBattleMage/Projectiles/HexMagicBolt.prefab";
    private const string MockDirectory = "Assets/_CustomItems/HexBattleMage/Mocks/Projectile";
    private const string RequestPath = "Temp/HexBattleMageShaderCleanup.request";
    private const string ReportDirectory = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/shader-cleanup";
    private const string BundlePath = "Assets/AssetBundles/hexbattlemage";
    private static readonly string[] TestPrefabPaths =
    {
        "Assets/_CustomItems/HexBattleMage/Wands/HexComponentTest.prefab",
        "Assets/_CustomItems/HexBattleMage/Wands/HexBundleTest.prefab"
    };

    static HexBattleMageBundlePolicy()
    {
        if (File.Exists(RequestPath))
        {
            EditorApplication.delayCall += RunRequestedCleanup;
        }
    }

    private static void RunRequestedCleanup()
    {
        if (!File.Exists(RequestPath))
        {
            return;
        }

        File.Delete(RequestPath);
        Directory.CreateDirectory(ReportDirectory);

        try
        {
            CleanAndBuild();
        }
        catch (Exception exception)
        {
            File.WriteAllText(ReportDirectory + "/result.txt", exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("HexBattleMage/Clean Shader Dependencies and Build Bundle")]
    public static void CleanAndBuild()
    {
        PrefabDefaultsAssetBuilder.PrepareAuthoredDefaults();
        Directory.CreateDirectory(ReportDirectory);
        WriteAudit("before");
        PrepareForBuild();
        ValidateDependencies();
        MageTableAssetBuilder.PrepareAndBuild();
        WriteAudit("after");
        VerifyBuiltBundle();
    }

    public static void PrepareForBuild()
    {
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

        if (stage != null && stage.scene.isDirty)
        {
            throw new InvalidOperationException("Save the open prefab before shader cleanup.");
        }

        Directory.CreateDirectory(ReportDirectory);
        Directory.CreateDirectory(MockDirectory);
        AssetDatabase.Refresh();

        foreach (string path in TestPrefabPaths)
        {
            var importer = AssetImporter.GetAtPath(path);

            if (importer == null)
            {
                continue;
            }

            if (importer.assetBundleName != BundleName)
            {
                continue;
            }

            BackupFile(path + ".meta");
            importer.SetAssetBundleNameAndVariant(string.Empty, string.Empty);
        }

        BackupFile(ProjectilePath);
        var prefab = PrefabUtility.LoadPrefabContents(ProjectilePath);

        try
        {
            var spark = prefab.transform.Find("SparkEffect");

            if (spark != null)
            {
                Object.DestroyImmediate(spark.gameObject);
            }

            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    throw new InvalidOperationException("Projectile has a missing component script.");
                }

                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();

                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    var reference = property.objectReferenceValue as GameObject;

                    if (reference == null)
                    {
                        continue;
                    }

                    string path = AssetDatabase.GetAssetPath(reference);

                    if (!path.StartsWith("Assets/GameObject/", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    property.objectReferenceValue = GetEffectMock(reference.name);
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, ProjectilePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        FixWandProjectileReference();
        AssetDatabase.SaveAssets();
    }

    private static void FixWandProjectileReference()
    {
        const string wandPath = "Assets/_CustomItems/HexBattleMage/Wands/Meadows/Wand_Meadows_01/HexBattleMage_Wand_Meadows_01.prefab";
        var projectile = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);

        BackupFile(wandPath);

        var wand = PrefabUtility.LoadPrefabContents(wandPath);

        try
        {
            foreach (var component in wand.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null)
                {
                    throw new InvalidOperationException("Wand has a missing component script.");
                }

                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();

                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    if (property.name != "m_attackProjectile")
                    {
                        continue;
                    }

                    if (property.objectReferenceValue == null)
                    {
                        continue;
                    }

                    property.objectReferenceValue = projectile;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(wand, wandPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(wand);
        }
    }

    public static void ValidateDependencies()
    {
        string[] roots = AssetDatabase.GetAssetPathsFromAssetBundle(BundleName);
        string[] dependencies = AssetDatabase.GetDependencies(roots, true);

        HexBattleMageTexturePolicy.Validate(dependencies);
        var forbidden = dependencies.Where(IsCopiedGameAsset).ToArray();

        if (forbidden.Length > 0)
        {
            throw new InvalidOperationException("Copied game assets must use Jotunn mocks before building:\n" + string.Join("\n", forbidden));
        }
    }

    private static bool IsCopiedGameAsset(string path)
    {
        if (path.StartsWith("Assets/", StringComparison.Ordinal) && AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Shader))
        {
            if (!path.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return path.StartsWith("Assets/Shader/", StringComparison.Ordinal)
            || path.StartsWith("Assets/GameObject/", StringComparison.Ordinal)
            || path.StartsWith("Assets/Material/", StringComparison.Ordinal)
            || path.StartsWith("Assets/Texture2D/", StringComparison.Ordinal)
            || path.StartsWith("Assets/Mesh/", StringComparison.Ordinal);
    }

    private static GameObject GetEffectMock(string effectName)
    {
        string name = "JVLmock_" + effectName;
        string path = MockDirectory + "/" + name + ".prefab";
        var mock = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (mock != null)
        {
            return mock;
        }

        var placeholder = new GameObject(name);

        try
        {
            return PrefabUtility.SaveAsPrefabAsset(placeholder, path);
        }
        finally
        {
            Object.DestroyImmediate(placeholder);
        }
    }

    private static void BackupFile(string path)
    {
        string destination = ReportDirectory + "/" + Path.GetFileName(path) + ".before";

        if (!File.Exists(destination))
        {
            File.Copy(path, destination);
        }
    }

    private static void WriteAudit(string phase)
    {
        var report = new StringBuilder();
        string[] roots = AssetDatabase.GetAssetPathsFromAssetBundle(BundleName);

        foreach (string root in roots)
        {
            report.AppendLine("ROOT " + root);

            foreach (string dependency in AssetDatabase.GetDependencies(root, true))
            {
                if (IsCopiedGameAsset(dependency))
                {
                    report.AppendLine("  COPIED " + dependency);
                }
            }
        }

        File.WriteAllText(ReportDirectory + "/" + phase + "-dependencies.txt", report.ToString());
    }

    private static void VerifyBuiltBundle()
    {
        int shaderWarnings = 0;
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (message.Contains("Failed to find expected binary shader data"))
            {
                shaderWarnings++;
            }
        };

        Application.logMessageReceived += capture;
        AssetBundle bundle = null;

        try
        {
            bundle = AssetBundle.LoadFromFile(Path.GetFullPath(BundlePath));

            if (bundle == null)
            {
                throw new InvalidOperationException("Clean bundle failed to load.");
            }

            var shaderNames = new HashSet<string>();

            bundle.LoadAllAssets<Shader>();
            string[] required = { "HexArcaneWorkbench", "HexBattleMage_Wand_Meadows_01", "HexMagicBolt" };

            foreach (string name in required)
            {
                var prefab = bundle.LoadAsset<GameObject>(name);

                if (prefab == null)
                {
                    throw new InvalidOperationException("Missing production prefab: " + name);
                }

                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null)
                        {
                            throw new InvalidOperationException("Missing material on " + name);
                        }

                        if (material.shader == null)
                        {
                            throw new InvalidOperationException("Missing shader on " + material.name);
                        }

                        shaderNames.Add(material.shader.name);
                    }
                }
            }

            var projectile = bundle.LoadAsset<GameObject>("HexMagicBolt");

            if (projectile.GetComponentsInChildren<ParticleSystem>(true).Length != 0)
            {
                throw new InvalidOperationException("Obsolete flight particles remain in the bundle.");
            }

            if (shaderWarnings != 0)
            {
                throw new InvalidOperationException("Clean bundle produced " + shaderWarnings + " broken shader warnings.");
            }

            string report = "SUCCESS\nProduction prefabs verified: " + string.Join(", ", required)
                + "\nCopied game shader/material/prefab dependencies: 0"
                + "\nBroken shader-data warnings during bundle verification: " + shaderWarnings
                + "\nBundle shaders: " + string.Join(", ", shaderNames.OrderBy(name => name))
                + "\nBundle size: " + new FileInfo(BundlePath).Length + " bytes"
                + "\nClean bundle copied to mod source. In-game verification remains required.";

            File.WriteAllText(ReportDirectory + "/result.txt", report);
            Debug.Log("HexBattleMage shader cleanup verified and bundle copied to mod source.");
        }
        finally
        {
            if (bundle != null)
            {
                bundle.Unload(true);
            }

            Application.logMessageReceived -= capture;
        }
    }
}

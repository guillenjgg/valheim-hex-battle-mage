using System;
using System.IO;
using System.Linq;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PrefabDefaultsValidation
{
    private const string RequestPath = "Temp/HexPrefabDefaultsValidation.request";
    private const string WandPath = "Assets/_CustomItems/HexBattleMage/Wands/HexBattleMageWand/HexBattleMageWand.prefab";
    private const string ReportPath = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/prefab-preservation-tests.txt";

    static PrefabDefaultsValidation()
    {
        if (File.Exists(RequestPath))
        {
            EditorApplication.delayCall += Run;
        }
    }

    [MenuItem("HexBattleMage/Validate Authored Defaults Preservation")]
    public static void Run()
    {
        if (File.Exists(RequestPath))
        {
            File.Delete(RequestPath);
        }

        PrefabDefaultsAssetBuilder.PrepareAuthoredDefaults();

        var paths = AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/_CustomItems/HexBattleMage/", StringComparison.Ordinal) && (path.EndsWith(".prefab", StringComparison.Ordinal) || path.EndsWith(".json", StringComparison.Ordinal))).ToArray();
        var originals = paths.ToDictionary(path => path, File.ReadAllBytes);

        try
        {
            PrefabDefaultsAssetBuilder.PrepareAuthoredDefaults();

            foreach (string path in paths)
            {
                if (!File.ReadAllBytes(path).SequenceEqual(originals[path]))
                {
                    throw new InvalidOperationException("Repeated authoring pass changed prefab: " + path);
                }
            }

            var prefab = PrefabUtility.LoadPrefabContents(WandPath);
            float editedRange;
            int editedCost;

            try
            {
                var settings = prefab.GetComponent<WandProfile>();
                var recipe = prefab.GetComponent<MageRecipeProfile>();

                settings.WandLightRange += 0.75f;
                recipe.Resources[0].Amount += 1;
                editedRange = settings.WandLightRange;
                editedCost = recipe.Resources[0].Amount;
                PrefabUtility.SaveAsPrefabAsset(prefab, WandPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            MagicShieldAssetBuilder.Prepare();
            WandRuneAssetBuilder.Prepare();

            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(WandPath);

            if (saved.GetComponent<WandProfile>().WandLightRange != editedRange)
            {
                throw new InvalidOperationException("Builder reset the authored wand light range.");
            }

            if (saved.GetComponent<MageRecipeProfile>().Resources[0].Amount != editedCost)
            {
                throw new InvalidOperationException("Builder reset the authored recipe.");
            }
        }
        catch (Exception exception)
        {
            File.WriteAllText(ReportPath, exception.ToString());
            throw;
        }
        finally
        {
            foreach (string path in paths)
            {
                File.WriteAllBytes(path, originals[path]);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        PrefabDefaultsAssetBuilder.PrepareAndBuild();
        File.WriteAllText(ReportPath, "PASS: repeated authoring is byte-stable for all custom prefabs; edited light range and recipe cost survive legacy bracer/rune preparation; original values restored and production bundle rebuilt.");
    }
}

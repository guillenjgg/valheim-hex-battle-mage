using System;
using System.IO;
using System.Linq;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;

public static class ArmorStyleAssetPreparation
{
    private const string AssetRoot = "Assets/_CustomItems/HexBattleMage";
    private const string BundleName = "hexbattlemage";

    public static void PrepareAll()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { AssetRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (source.GetComponent<MageArmorStyleProfile>() == null)
            {
                continue;
            }

            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                Prepare(root, path);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    public static void Prepare(GameObject root, string prefabPath)
    {
        var profile = root.GetComponent<MageArmorStyleProfile>();

        if (profile.Styles.Length == 0)
        {
            throw new InvalidOperationException("Armor style profile has no authored styles: " + prefabPath);
        }

        var data = new MageArmorStyleData
        {
            RendererName = profile.RendererName,
            Styles = profile.Styles.Select(style => new MageArmorStyleAsset { DisplayName = style.DisplayName, MaterialPath = AssetDatabase.GetAssetPath(style.Material), IconPath = AssetDatabase.GetAssetPath(style.Icon) }).ToArray()
        };
        string path = profile.BakedStyles != null ? AssetDatabase.GetAssetPath(profile.BakedStyles) : Path.GetDirectoryName(prefabPath).Replace('\\', '/') + "/Styles_" + root.name + ".json";

        File.WriteAllText(path, MageArmorStyleProfile.Encode(data));
        AssetDatabase.ImportAsset(path);
        profile.BakedStyles = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant(BundleName, string.Empty);

        foreach (var style in data.Styles)
        {
            if (string.IsNullOrEmpty(style.MaterialPath) || string.IsNullOrEmpty(style.IconPath) || string.IsNullOrEmpty(style.DisplayName))
            {
                throw new InvalidOperationException("Armor style needs a material, icon and localized name: " + prefabPath);
            }

            AssetImporter.GetAtPath(style.MaterialPath).SetAssetBundleNameAndVariant(BundleName, string.Empty);
            AssetImporter.GetAtPath(style.IconPath).SetAssetBundleNameAndVariant(BundleName, string.Empty);
        }
    }
}

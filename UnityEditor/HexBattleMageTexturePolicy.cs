using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class HexBattleMageTexturePolicy : AssetPostprocessor
{
    private const string AssetRoot = "Assets/_CustomItems/HexBattleMage/";
    private const int CompressionQuality = 100;
    private const int MaximumTextureSize = 256;
    private const string DesktopPlatform = "Standalone";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(AssetRoot, StringComparison.Ordinal))
        {
            return;
        }

        Configure((TextureImporter)assetImporter);
    }

    [MenuItem("HexBattleMage/Compress Authored Textures")]
    public static void PrepareAll()
    {
        int count = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { AssetRoot.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                continue;
            }

            if (!Configure(importer))
            {
                continue;
            }

            importer.SaveAndReimport();
            count++;
        }

        Debug.Log("HexBattleMage texture compression updated: " + count + " imports; maximum size 256, Read/Write enabled; texture types and alpha settings preserved.");
    }

    private static bool Configure(TextureImporter importer)
    {
        var defaults = importer.GetDefaultPlatformTextureSettings();
        var desktop = importer.GetPlatformTextureSettings(DesktopPlatform);
        bool changed = !importer.isReadable || NeedsCompression(defaults);

        if (desktop.overridden && NeedsCompression(desktop))
        {
            changed = true;
        }

        if (!changed)
        {
            return false;
        }

        importer.isReadable = true;
        ConfigureCompression(defaults);
        importer.SetPlatformTextureSettings(defaults);

        if (desktop.overridden)
        {
            ConfigureCompression(desktop);
            importer.SetPlatformTextureSettings(desktop);
        }

        return true;
    }

    private static bool NeedsCompression(TextureImporterPlatformSettings settings)
    {
        return settings.maxTextureSize != MaximumTextureSize || settings.textureCompression != TextureImporterCompression.Compressed || !settings.crunchedCompression || settings.compressionQuality != CompressionQuality || settings.format != TextureImporterFormat.Automatic;
    }

    private static void ConfigureCompression(TextureImporterPlatformSettings settings)
    {
        settings.maxTextureSize = MaximumTextureSize;
        settings.textureCompression = TextureImporterCompression.Compressed;
        settings.crunchedCompression = true;
        settings.compressionQuality = CompressionQuality;
        settings.format = TextureImporterFormat.Automatic;
    }

    public static void Validate(string[] dependencies)
    {
        foreach (string path in dependencies.Where(path => path.StartsWith(AssetRoot, StringComparison.Ordinal)))
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                continue;
            }

            if (!importer.isReadable || NeedsCompression(importer.GetDefaultPlatformTextureSettings()))
            {
                throw new InvalidOperationException("Compress this authored texture before building: " + path);
            }

            var desktop = importer.GetPlatformTextureSettings(DesktopPlatform);

            if (desktop.overridden && NeedsCompression(desktop))
            {
                throw new InvalidOperationException("Compress this texture's desktop override before building: " + path);
            }
        }
    }
}

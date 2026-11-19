using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class GearIconAssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage";
    private const int IconSize = 256;
    private const float FrameFill = 0.86f;

    [MenuItem("HexBattleMage/Prepare World Drop Icons and Build Bundle")]
    public static void Build()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var equipment = source.GetComponent<MageEquipmentDefinition>();

            if (equipment == null)
            {
                continue;
            }

            if (equipment.Category == MageEquipmentCategory.Wand || equipment.Category == MageEquipmentCategory.Legs || equipment.Category == MageEquipmentCategory.None)
            {
                continue;
            }

            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var armor = root.GetComponent<HexBattleMageArmorProfile>();
                var cloak = root.GetComponent<HexBattleMageCloakProfile>();
                var model = armor != null ? armor.WorldModel : cloak != null ? cloak.WorldModel : root;

                if (model == null)
                {
                    continue;
                }

                var styles = root.GetComponent<MageArmorStyleProfile>();
                int count = styles != null ? styles.Styles.Length : 1;
                Sprite first = null;
                string directory = Path.GetDirectoryName(path).Replace('\\', '/') + "/Icons";

                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();

                for (int index = 0; index < count; index++)
                {
                    string iconPath = directory + "/WorldDrop_v2_" + index + ".png";
                    var icon = Render(model, iconPath, styles != null ? styles.Styles[index].Material : null, styles != null ? styles.RendererName : string.Empty);

                    if (index == 0)
                    {
                        first = icon;
                    }

                    if (styles != null)
                    {
                        styles.Styles[index].Icon = icon;
                    }
                }

                foreach (var component in root.GetComponents<MonoBehaviour>())
                {
                    var field = component.GetType().GetField("Icon", BindingFlags.Public | BindingFlags.Instance);

                    if (field != null && field.FieldType == typeof(Sprite))
                    {
                        field.SetValue(component, first);
                    }
                }

                if (styles != null)
                {
                    ArmorStyleAssetPreparation.Prepare(root, path);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }

    private static Sprite Render(GameObject model, string path, Material style, string rendererName)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (existing != null)
        {
            return existing;
        }

        var instance = Object.Instantiate(model);
        var preview = new PreviewRenderUtility();
        var materials = new List<Material>();

        try
        {
            instance.SetActive(true);

            var renderers = instance.GetComponentsInChildren<Renderer>().Where(renderer => renderer.enabled).ToArray();

            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Cannot render an empty dropped item: " + model.name);
            }

            var bounds = renderers[0].bounds;

            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);

                var originals = renderer.sharedMaterials;
                var replacements = new Material[originals.Length];

                for (int index = 0; index < originals.Length; index++)
                {
                    var source = style != null && (renderer.name == rendererName || renderers.Length == 1) ? style : originals[index];
                    var material = new Material(Shader.Find("Hidden/HexBattleMage/GearIconPreview"));

                    material.mainTexture = source.mainTexture;
                    material.mainTextureScale = source.mainTextureScale;
                    material.mainTextureOffset = source.mainTextureOffset;
                    material.color = source.HasProperty("_Color") ? source.color : Color.white;
                    material.SetColor("_EmissionColor", source.HasProperty("_EmissionColor") ? source.GetColor("_EmissionColor") : Color.black);
                    replacements[index] = material;
                    materials.Add(material);
                }

                renderer.sharedMaterials = replacements;
            }

            preview.AddSingleGO(instance);
            preview.camera.transform.position = bounds.center + new Vector3(0.25f, 0.04f, model.GetComponent<MagicBracerProfile>() != null || model.GetComponents<MonoBehaviour>().Any(component => component.GetType().Name == "HexBattleMageTrinketProfile") ? -1f : 1f).normalized * Mathf.Max(2f, bounds.size.magnitude * 2f);
            preview.camera.transform.LookAt(bounds.center);
            preview.camera.orthographic = true;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 20f;

            float extent = 0f;

            for (int corner = 0; corner < 8; corner++)
            {
                var direction = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                Vector3 point = preview.camera.transform.InverseTransformPoint(bounds.center + Vector3.Scale(bounds.extents, direction));

                extent = Mathf.Max(extent, Mathf.Abs(point.x), Mathf.Abs(point.y));
            }

            preview.camera.orthographicSize = Mathf.Max(0.01f, extent / FrameFill);
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = Color.clear;
            preview.BeginPreview(new Rect(0f, 0f, IconSize, IconSize), GUIStyle.none);
            preview.Render();

            var result = preview.EndPreview();
            var previous = RenderTexture.active;

            RenderTexture.active = (RenderTexture)result;

            var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);

            texture.ReadPixels(new Rect(0f, 0f, IconSize, IconSize), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        finally
        {
            preview.Cleanup();

            foreach (var material in materials)
            {
                Object.DestroyImmediate(material);
            }
        }

        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = IconSize;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.crunchedCompression = true;
        importer.compressionQuality = 100;
        importer.SaveAndReimport();
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}



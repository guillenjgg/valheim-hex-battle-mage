using System;
using System.IO;
using System.Linq;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class CloakStyleAssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01";
    private const string Identity = "HexBattleMage_Cloak_Meadows_01";
    private const string StyleFolder = Root + "/Styles";

    [MenuItem("HexBattleMage/Cloak/Prepare Color Styles and Build Bundle")]
    public static void Build()
    {
        Directory.CreateDirectory(StyleFolder);
        AssetDatabase.Refresh();
        string path = Root + "/" + Identity + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            var profile = root.GetComponent<MageArmorStyleProfile>();

            if (profile == null)
            {
                profile = root.AddComponent<MageArmorStyleProfile>();
            }

            if (profile.Styles.Length == 0)
            {
                var names = new[] { "Brown", "White", "Black" };
                var colors = new[] { new Color(0.3f, 0.19f, 0.105f), new Color(0.88f, 0.87f, 0.82f), new Color(0.025f, 0.027f, 0.03f) };
                var source = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/MeadowsCloth.mat");
                var cloak = root.GetComponent<HexBattleMageCloakProfile>();

                profile.Styles = new MageArmorStyle[names.Length];

                for (int index = 0; index < names.Length; index++)
                {
                    string materialPath = StyleFolder + "/" + names[index] + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                    if (material == null)
                    {
                        material = new Material(source);
                        material.mainTexture = null;
                        material.color = colors[index];
                        AssetDatabase.CreateAsset(material, materialPath);
                    }

                    string iconPath = StyleFolder + "/" + names[index] + ".png";

                    if (!File.Exists(iconPath))
                    {
                        RenderIcon(cloak.WorldModel, profile.RendererName, material, iconPath);
                    }

                    AssetDatabase.ImportAsset(iconPath);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                    profile.Styles[index] = new MageArmorStyle { DisplayName = "$hexbattlemage_armor_style_" + names[index].ToLowerInvariant(), Material = material, Icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath) };
                }
            }

            ArmorStyleAssetPreparation.Prepare(root, path);

            var recipe = root.GetComponent<MageRecipeProfile>();

            if (!recipe.Resources.Any(resource => resource.ItemPrefab == "Resin"))
            {
                recipe.Resources = recipe.Resources.Concat(new[] { new MageRecipeResource { ItemPrefab = "Resin", Amount = 5 } }).ToArray();
            }

            string recipePath = Root + "/Recipe_" + Identity + ".json";

            File.WriteAllText(recipePath, recipe.GetBakedData());
            AssetDatabase.ImportAsset(recipePath);
            recipe.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(recipePath);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        HexBattleMageTexturePolicy.PrepareAll();
        AssetDatabase.SaveAssets();
        HexBattleMageBundlePolicy.ValidateDependencies();
        var manifest = BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

        if (manifest == null)
        {
            throw new InvalidOperationException("Armor style bundle build failed.");
        }

        File.Copy("Assets/AssetBundles/hexbattlemage", "C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/hexbattlemage", true);
    }

    private static void RenderIcon(GameObject model, string rendererName, Material style, string path)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var instance = Object.Instantiate(model);
        var cameraObject = new GameObject("Style icon camera");
        var lightObject = new GameObject("Style icon light");
        var target = new RenderTexture(256, 256, 24);
        var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;

        SceneManager.MoveGameObjectToScene(instance, scene);
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        SceneManager.MoveGameObjectToScene(lightObject, scene);

        try
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.name == rendererName ? style : renderer.sharedMaterial;
                var preview = new Material(Shader.Find("Standard"));

                preview.color = source.color;
                preview.mainTexture = source.mainTexture;
                preview.SetFloat("_Glossiness", 0f);
                renderer.sharedMaterial = preview;
            }

            var light = lightObject.AddComponent<Light>();

            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            var camera = cameraObject.AddComponent<Camera>();

            camera.scene = scene;
            camera.transform.position = new Vector3(0.25f, 0.12f, 1f);
            camera.transform.LookAt(Vector3.zero);
            camera.orthographic = true;
            camera.orthographicSize = 0.24f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}

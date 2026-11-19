using System;
using System.IO;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MeadowsCloak01GameplayBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01";
    private const string Identity = "HexBattleMage_Cloak_Meadows_01";
    private const string BundleName = "hexbattlemage";

    [MenuItem("HexBattleMage/Cloak/Prepare Meadows Cloak 01 Gameplay and Build Bundle")]
    public static void Build()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();

        if (stage != null && stage.scene.isDirty)
        {
            throw new InvalidOperationException("Save the open prefab before preparing the cloak.");
        }

        Prepare();
        HexBattleMageBundlePolicy.ValidateDependencies();
        Directory.CreateDirectory("Assets/AssetBundles");

        var manifest = BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

        if (manifest == null)
        {
            throw new InvalidOperationException("Cloak bundle build failed.");
        }

        File.Copy("Assets/AssetBundles/" + BundleName, "C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/" + BundleName, true);
        Debug.Log("Cloak gameplay defaults, icon and both body fits included in hexbattlemage.");
    }

    public static void Prepare()
    {
        string path = Root + "/" + Identity + ".prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);

        try
        {
            var profile = prefab.GetComponent<HexBattleMageCloakProfile>();

            if (profile == null)
            {
                profile = prefab.AddComponent<HexBattleMageCloakProfile>();
                profile.FrostResistance = true;
            }

            if (prefab.GetComponent<MageEitrProfile>() == null)
            {
                var eitr = prefab.AddComponent<MageEitrProfile>();

                eitr.MaximumEitr = 5f;
                eitr.MaximumEitrPerQuality = 1f;
                eitr.RegenerationModifier = 0f;
            }

            if (profile.MaleVisual == null)
            {
                profile.MaleVisual = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Visuals/" + Identity + "_Male.prefab");
            }

            if (profile.FemaleVisual == null)
            {
                profile.FemaleVisual = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Visuals/" + Identity + "_Female.prefab");
            }

            if (profile.WorldModel == null)
            {
                profile.WorldModel = PrepareWorldModel(profile.MaleVisual);
            }

            if (profile.Icon == null)
            {
                profile.Icon = PrepareIcon(profile.WorldModel);
            }

            if (prefab.GetComponent<MageUpgradeProfile>() == null)
            {
                var upgrades = prefab.AddComponent<MageUpgradeProfile>();

                upgrades.RequiredStationLevel = 3;
                upgrades.FixedResourceCosts = true;
            }

            if (prefab.GetComponent<MageEquipmentDefinition>() == null)
            {
                var definition = prefab.AddComponent<MageEquipmentDefinition>();

                definition.Category = MageEquipmentCategory.Cloak;
                definition.Slot = MageEquipmentSlot.Cloak;
            }

            var recipe = prefab.GetComponent<MageRecipeProfile>();

            if (recipe == null)
            {
                recipe = prefab.AddComponent<MageRecipeProfile>();
                recipe.Resources = new[]
                {
                    new MageRecipeResource { ItemPrefab = "LeatherScraps", Amount = 6, AmountPerQuality = 3 },
                    new MageRecipeResource { ItemPrefab = "DeerHide", Amount = 4, AmountPerQuality = 2 },
                    new MageRecipeResource { ItemPrefab = "Resin", Amount = 5, AmountPerQuality = 2 }
                };
            }

            string recipePath = Root + "/Recipe_" + Identity + ".json";

            File.WriteAllText(recipePath, recipe.GetBakedData());
            AssetDatabase.ImportAsset(recipePath);
            recipe.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(recipePath);
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        CloakCollisionAssetPreparation.PrepareAll();
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant(BundleName, string.Empty);
        AssetDatabase.SaveAssets();
    }

    private static GameObject PrepareWorldModel(GameObject visual)
    {
        string path = Root + "/" + Identity + "_Drop.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (existing != null)
        {
            return existing;
        }

        var source = Object.Instantiate(visual);
        var model = new GameObject(Identity + "_Drop");

        try
        {
            foreach (var renderer in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = new Mesh { name = Identity + "_Drop_" + renderer.name };

                renderer.BakeMesh(mesh);

                var positions = mesh.vertices;

                for (int index = 0; index < positions.Length; index++)
                {
                    positions[index] = (positions[index] - Vector3.up * 1.05f) * 0.3f;
                }

                mesh.vertices = positions;
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, Root + "/Meshes/" + mesh.name + ".asset");

                var part = new GameObject(renderer.name);

                part.transform.SetParent(model.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                part.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            }

            PrefabUtility.SaveAsPrefabAsset(model, path);
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(model);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static Sprite PrepareIcon(GameObject model)
    {
        string path = Root + "/" + Identity + "_Icon.png";

        if (!File.Exists(path))
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var instance = Object.Instantiate(model);
            var cameraObject = new GameObject("Icon camera");
            var lightObject = new GameObject("Icon light");
            var target = new RenderTexture(256, 256, 24);
            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, scene);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, scene);

            try
            {
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

        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}

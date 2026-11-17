using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WandAssetTools
{
    private const string PrefabPath = "Assets/_CustomItems/HexBattleMage/Wands/HexBattleMageWand/HexBattleMageWand.prefab";
    private const string MaterialDirectory = "Assets/_CustomItems/HexBattleMage/Transmogs/Wands/Apprentice/Materials";
    private const string SharedTextureDirectory = "Assets/_CustomItems/HexBattleMage/Transmogs/Wands/Shared/Textures";
    private const string RequestPath = "Temp/HexBattleMageWandValidate.request";
    private const string ResultPath = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/wand-validation-result.txt";

    static WandAssetTools()
    {
        if (File.Exists(RequestPath))
        {
            EditorApplication.delayCall += RunRequestedValidation;
        }
    }

    private static void RunRequestedValidation()
    {
        if (!File.Exists(RequestPath))
        {
            return;
        }

        File.Delete(RequestPath);

        try
        {
            PrepareAndValidate();
        }
        catch (Exception exception)
        {
            File.WriteAllText(ResultPath, exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("HexBattleMage/Prepare and Validate Wand")]
    public static void PrepareAndValidate()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();

        if (stage != null && stage.scene.isDirty)
        {
            throw new InvalidOperationException("Save the open prefab before preparing the wand.");
        }

        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);

        try
        {
            var tip = prefab.transform.Find("attach/WandModel/WandTip");

            if (tip == null)
            {
                throw new InvalidOperationException("The wand tip anchor is missing.");
            }

            var glow = tip.Find("Glow");

            if (glow != null)
            {
                Object.DestroyImmediate(glow.gameObject);
            }

            var emission = PrepareTipEmissionMask();

            foreach (string materialName in new[] { "ApprenticeBranch", "ApprenticeDarkBark", "ApprenticeBirch" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialDirectory + "/" + materialName + ".mat");

                if (material.GetTexture("_EmissionMap") == null)
                {
                    material.SetTexture("_EmissionMap", emission);
                }
                material.EnableKeyword("_EMISSION");
                EditorUtility.SetDirty(material);
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        AssetDatabase.SaveAssets();
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        string report = "Prepared wood-tip emission fading over the final 3 cm; removed the separate glow sphere.\n";

        foreach (string layer in new[] { "terrain", "piece" })
        {
            report += ValidateDrop(source, layer, Quaternion.identity);
            report += ValidateDrop(source, layer, Quaternion.Euler(75f, 30f, 15f));
        }

        var natural = AssetDatabase.LoadAssetAtPath<Material>(MaterialDirectory + "/ApprenticeBranch.mat");
        var dark = AssetDatabase.LoadAssetAtPath<Material>(MaterialDirectory + "/ApprenticeDarkBark.mat");

        var birch = AssetDatabase.LoadAssetAtPath<Material>(MaterialDirectory + "/ApprenticeBirch.mat");

        if (natural.mainTexture == null || dark.mainTexture == null || birch.mainTexture == null || natural.mainTexture == dark.mainTexture || birch.mainTexture == natural.mainTexture || birch.mainTexture == dark.mainTexture)
        {
            throw new InvalidOperationException("Wand styles must reference three distinct textures.");
        }

        File.WriteAllText(ResultPath, "SUCCESS\n" + report + "Three distinct style textures verified. Bundle was not rebuilt.");
        Debug.Log("HexBattleMage wand preparation and isolated physics tests passed.");
    }

    private static Texture2D PrepareTipEmissionMask()
    {
        const int textureSize = 256;
        const float branchUvLimit = 0.62f;
        const float emissionStart = 0.96f;
        string texturePath = SharedTextureDirectory + "/WandTipEmission.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        if (existing != null)
        {
            return existing;
        }

        var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false, true);
        var pixels = new Color[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            float alongBranch = y / (float)(textureSize - 1);
            float strength = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(emissionStart, 1f, alongBranch));

            for (int x = 0; x < textureSize; x++)
            {
                float acrossTexture = x / (float)(textureSize - 1);
                float emission = acrossTexture <= branchUvLimit ? strength : 0f;

                pixels[y * textureSize + x] = new Color(emission, emission, emission, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(texturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(texturePath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);

        importer.sRGBTexture = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
    }

    private static string ValidateDrop(GameObject source, string layer, Quaternion rotation)
    {
        var scene = EditorSceneManager.NewPreviewScene();

        try
        {
            var ground = new GameObject("ValidationGround");

            SceneManager.MoveGameObjectToScene(ground, scene);
            ground.layer = LayerMask.NameToLayer(layer);
            ground.transform.position = new Vector3(0f, -0.1f, 0f);
            ground.AddComponent<BoxCollider>().size = new Vector3(20f, 0.2f, 20f);

            var instance = Object.Instantiate(source);

            SceneManager.MoveGameObjectToScene(instance, scene);

            foreach (var script in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                Object.DestroyImmediate(script);
            }

            instance.SetActive(true);
            instance.transform.SetPositionAndRotation(new Vector3(0f, 2f, 0f), rotation);

            var body = instance.GetComponent<Rigidbody>();
            var colliders = instance.GetComponentsInChildren<Collider>();

            if (colliders.Length == 0)
            {
                throw new InvalidOperationException("The wand has no imported collision shape.");
            }

            body.isKinematic = false;
            body.WakeUp();

            var physics = scene.GetPhysicsScene();
            if (physics == Physics.defaultPhysicsScene)
            {
                throw new InvalidOperationException("Refusing to simulate the user scene.");
            }

            for (int step = 0; step < 250; step++)
            {
                physics.Simulate(0.02f);
            }

            if (body.position.y < -0.1f || body.position.y > 1f)
            {
                throw new InvalidOperationException($"Wand drop failed on {layer}: y={body.position.y}.");
            }

            return $"PASS: drop onto {layer}; settled y={body.position.y:0.###}; colliders={colliders.Length}.\n";
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}



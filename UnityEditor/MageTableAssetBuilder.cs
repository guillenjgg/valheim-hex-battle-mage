using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class MageTableAssetBuilder
{
    private const string RequestPath = "Temp/HexBattleMagePrepare.request";
    private const string ResultPath = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/mage-table-prepare-result.txt";
    private const string CraftingPath = "Assets/_CustomItems/HexBattleMage/Crafting";
    private const string PrefabPath = CraftingPath + "/HexArcaneWorkbench.prefab";
    private const string ModelPath = CraftingPath + "/mage-table.fbx";
    private const string MaterialPath = "Assets/_CustomItems/HexBattleMage/Materials/MageTable";
    private const string MockPath = "Assets/_CustomItems/HexBattleMage/Mocks/MageTable";
    private const string ShaderPath = "Assets/JVLmock/Shader/JVLmock_Custom_Piece.shader";
    private const string BundleName = "hexbattlemage";
    private const string BundleDirectory = "Assets/AssetBundles";
    private const string ModBundlePath = "C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/hexbattlemage";
    private static readonly Dictionary<Object, Object> Mocks = new Dictionary<Object, Object>();

    static MageTableAssetBuilder()
    {
        if (File.Exists(RequestPath))
        {
            EditorApplication.delayCall += RunRequestedBuild;
        }
    }

    private static void RunRequestedBuild()
    {
        if (!File.Exists(RequestPath))
        {
            return;
        }

        File.Delete(RequestPath);

        try
        {
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

            if (stage != null && stage.scene.isDirty)
            {
                throw new InvalidOperationException("Save the open prefab before preparing the Mage Table.");
            }

            PrepareAndBuild();
            File.WriteAllText(ResultPath, "SUCCESS: Mage Table materials and bundle prepared and verified.");
        }
        catch (Exception exception)
        {
            File.WriteAllText(ResultPath, exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("HexBattleMage/Prepare Mage Table and Build Bundle")]
    public static void PrepareAndBuild()
    {
        Directory.CreateDirectory(MaterialPath);
        Directory.CreateDirectory(MockPath);
        AssetDatabase.Refresh();

        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

        if (shader == null)
        {
            throw new InvalidOperationException("The JVLmock_Custom/Piece shader is missing.");
        }

        var materials = new Dictionary<string, Material>();

        foreach (string name in new[] { "DarkOak", "CarvedStone", "RuneInlay" })
        {
            materials.Add(name, PrepareMaterial(name, shader));
        }

        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;

        if (importer == null)
        {
            throw new InvalidOperationException("Mage Table FBX is missing.");
        }

        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;

        foreach (var material in materials)
        {
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.Key), material.Value);
        }

        importer.SaveAndReimport();

        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);

        try
        {
            prefab.transform.localPosition = Vector3.zero;

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;

                for (int index = 0; index < slots.Length; index++)
                {
                    if (slots[index] == null)
                    {
                        continue;
                    }

                    string materialName = slots[index].name.Replace(" (Instance)", string.Empty);

                    if (materials.TryGetValue(materialName, out Material replacement))
                    {
                        slots[index] = replacement;
                    }
                }

                renderer.sharedMaterials = slots;
            }

            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    throw new InvalidOperationException("Mage Table has a missing component script.");
                }

                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();

                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    Object reference = property.objectReferenceValue;

                    if (reference == null)
                    {
                        continue;
                    }

                    string path = AssetDatabase.GetAssetPath(reference);

                    if (string.IsNullOrEmpty(path) || path.StartsWith("Assets/_CustomItems/", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (reference is GameObject || reference is Sprite || reference is Material)
                    {
                        property.objectReferenceValue = GetMock(reference, shader);
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        AssetImporter.GetAtPath(PrefabPath).SetAssetBundleNameAndVariant(BundleName, string.Empty);
        AssetDatabase.SaveAssets();
        ValidateDependencies();
        Directory.CreateDirectory(BundleDirectory);

        var build = new AssetBundleBuild
        {
            assetBundleName = BundleName,
            assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(BundleName)
        };
        var manifest = BuildPipeline.BuildAssetBundles(BundleDirectory, new[] { build }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);

        if (manifest == null)
        {
            throw new InvalidOperationException("Mage Table asset bundle build failed.");
        }

        var bundle = AssetBundle.LoadFromFile(Path.GetFullPath(BundleDirectory + "/" + BundleName));

        if (bundle == null)
        {
            throw new InvalidOperationException("The built bundle could not be opened.");
        }

        try
        {
            foreach (string name in new[] { "HexArcaneWorkbench", "HexBeechWand", "HexMagicBolt" })
            {
                if (bundle.LoadAsset<GameObject>(name) == null)
                {
                    throw new InvalidOperationException("Built bundle is missing " + name);
                }
            }
        }
        finally
        {
            bundle.Unload(true);
        }

        File.Copy(BundleDirectory + "/" + BundleName, ModBundlePath, true);
        Debug.Log("HexBattleMage: materials prepared; Mage Table, wand and projectile verified in bundle; bundle copied to mod source.");
    }

    private static Material PrepareMaterial(string name, Shader shader)
    {
        string path = MaterialPath + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetTexture("_MainTex", LoadTexture(name, "BaseColor", false));
        material.SetTexture("_BumpMap", LoadTexture(name, "Normal", true));
        material.SetTexture("_EmissionMap", LoadTexture(name, "Emission", false));
        material.SetColor("_Color", Color.white);
        material.SetColor("_EmissionColor", name == "RuneInlay" ? Color.white * 1.5f : Color.black);
        material.SetFloat("_Glossiness", name == "CarvedStone" ? 0.12f : 0.22f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_MetallicAlphaGloss", 0f);
        material.SetFloat("_ValueNoise", 0f);
        material.SetFloat("_ValueNoiseVertex", 0f);
        material.SetFloat("_RippleDistance", 0f);
        material.SetFloat("_Cull", name == "RuneInlay" ? 0f : 2f);
        material.EnableKeyword("_NORMALMAP");

        if (name == "RuneInlay")
        {
            material.EnableKeyword("_EMISSION");
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D LoadTexture(string name, string suffix, bool normalMap)
    {
        string path = CraftingPath + "/textures/" + name + "_" + suffix + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            throw new InvalidOperationException("Missing texture: " + path);
        }

        importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normalMap;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Object GetMock(Object source, Shader shader)
    {
        if (Mocks.TryGetValue(source, out Object cached))
        {
            return cached;
        }

        string name = "JVLmock_" + source.name;
        string fileName = name.Replace('/', '_').Replace('\\', '_');
        Object mock;

        if (source is GameObject)
        {
            string path = MockPath + "/" + fileName + ".prefab";
            mock = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (mock == null)
            {
                var placeholder = new GameObject(name);

                try
                {
                    mock = PrefabUtility.SaveAsPrefabAsset(placeholder, path);
                }
                finally
                {
                    Object.DestroyImmediate(placeholder);
                }
            }
        }
        else if (source is Sprite)
        {
            string path = MockPath + "/" + fileName + ".asset";
            mock = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (mock == null)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CraftingPath + "/textures/RuneInlay_BaseColor.png");

                mock = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
                mock.name = name;
                AssetDatabase.CreateAsset(mock, path);
            }
        }
        else
        {
            string path = MockPath + "/" + fileName + ".mat";
            mock = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mock == null)
            {
                mock = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mock, path);
            }
        }

        Mocks.Add(source, mock);
        return mock;
    }

    private static void ValidateDependencies()
    {
        foreach (string path in AssetDatabase.GetDependencies(PrefabPath, true))
        {
            if (path.StartsWith("Assets/GameObject/", StringComparison.Ordinal) || path.StartsWith("Assets/Material/", StringComparison.Ordinal) || path.StartsWith("Assets/Texture2D/", StringComparison.Ordinal) || path.StartsWith("Assets/Mesh/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Unexpected vanilla asset dependency: " + path);
            }
        }
    }
}

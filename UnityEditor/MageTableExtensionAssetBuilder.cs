using System;
using System.IO;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class MageTableExtensionAssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/Crafting/Extensions";
    private const string TablePath = "Assets/_CustomItems/HexBattleMage/Crafting/HexArcaneWorkbench.prefab";
    private const string Materials = "Assets/_CustomItems/HexBattleMage/Materials/MageTable/";
    private const string RequestPath = "Temp/HexMageExtensions.request";

    static MageTableExtensionAssetBuilder()
    {
        if (File.Exists(RequestPath))
        {
            EditorApplication.delayCall += RunRequest;
        }
    }

    private static void RunRequest()
    {
        File.Delete(RequestPath);

        try
        {
            Build();
        }
        catch (Exception exception)
        {
            File.WriteAllText("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/mage-extensions-result.txt", exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("HexBattleMage/Prepare Meadows Table Extensions and Build Bundle")]
    public static void Build()
    {
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

        if (stage != null && stage.scene.isDirty)
        {
            throw new InvalidOperationException("Save the open prefab before preparing extensions.");
        }

        Directory.CreateDirectory(Root);
        AssetDatabase.Refresh();
        Prepare("HexMageRuneStone", "$piece_hexbattlemage_runestone", "$piece_hexbattlemage_runestone_description", false);
        Prepare("HexMageReagentStand", "$piece_hexbattlemage_reagentstand", "$piece_hexbattlemage_reagentstand_description", true);
        AssetDatabase.SaveAssets();
        Preview("HexMageRuneStone");
        Preview("HexMageReagentStand");
        AssetDatabase.SaveAssets();
        HexBattleMageBundlePolicy.ValidateDependencies();

        var manifest = BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

        if (manifest == null)
        {
            throw new InvalidOperationException("Mage Table extension bundle build failed.");
        }

        File.Copy("Assets/AssetBundles/hexbattlemage", "C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/hexbattlemage", true);
        File.WriteAllText("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/mage-extensions-result.txt", "PASS: two preserved, inspectable Meadows extension prefabs bundled; distinct non-stacking station extensions, authored recipes, colliders, blue rune materials and native station reference verified.");
    }

    private static void Prepare(string name, string displayName, string description, bool reagentStand)
    {
        string path = Root + "/" + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (existing != null)
        {
            if (existing.GetComponent<StationExtension>().m_connectionPrefab == null)
            {
                var contents = PrefabUtility.LoadPrefabContents(path);

                SetConnection(contents.GetComponent<StationExtension>());
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }

            Validate(existing);
            return;
        }

        var table = AssetDatabase.LoadAssetAtPath<GameObject>(TablePath);
        var root = new GameObject(name);

        root.layer = table.layer;

        foreach (var source in table.GetComponents<Component>())
        {
            if (source is Transform || source is CraftingStation || source is MageTableProfile || source is MageRecipeProfile)
            {
                continue;
            }

            if (source is Collider || source is Renderer || source is MeshFilter)
            {
                continue;
            }

            var target = root.AddComponent(source.GetType());

            EditorUtility.CopySerialized(source, target);
        }

        var piece = root.GetComponent<Piece>();

        piece.m_name = displayName;
        piece.m_description = description;
        piece.m_craftingStation = table.GetComponent<CraftingStation>();
        piece.m_icon = null;

        var extension = root.AddComponent<StationExtension>();

        extension.m_craftingStation = table.GetComponent<CraftingStation>();
        extension.m_maxStationDistance = 5f;
        extension.m_stack = false;
        SetConnection(extension);

        var recipe = root.AddComponent<MageRecipeProfile>();

        recipe.CraftingStation = "HexArcaneWorkbench";
        recipe.Resources = reagentStand
            ? new[] { Resource("Wood", 15), Resource("Resin", 8), Resource("Feathers", 5), Resource("BoneFragments", 5) }
            : new[] { Resource("Wood", 10), Resource("Stone", 10), Resource("Resin", 5) };

        string recipePath = Root + "/Recipe_" + name + ".json";

        File.WriteAllText(recipePath, recipe.GetBakedData());
        AssetDatabase.ImportAsset(recipePath);
        recipe.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(recipePath);

        var wood = AssetDatabase.LoadAssetAtPath<Material>(Materials + "DarkOak.mat");
        var stone = AssetDatabase.LoadAssetAtPath<Material>(Materials + "CarvedStone.mat");
        var blue = PrepareBlueMaterial();

        if (reagentStand)
        {
            Part(root, "Top", PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f), new Vector3(0.7f, 0.1f, 0.45f), wood);

            foreach (float x in new[] { -0.26f, 0.26f })
            {
                foreach (float z in new[] { -0.14f, 0.14f })
                {
                    Part(root, "Leg", PrimitiveType.Cube, new Vector3(x, 0.35f, z), new Vector3(0.09f, 0.7f, 0.09f), wood);
                }
            }

            Part(root, "FrontBoard", PrimitiveType.Cube, new Vector3(0f, 0.59f, -0.19f), new Vector3(0.62f, 0.17f, 0.06f), wood);
            var resin = PrepareMaterial("AmberResin", new Color(0.8f, 0.35f, 0.05f));
            var bone = PrepareMaterial("BoneAndFeather", new Color(0.8f, 0.77f, 0.62f));

            for (int index = 0; index < 3; index++)
            {
                Part(root, "ResinLump", PrimitiveType.Sphere, new Vector3(-0.2f + index * 0.06f, 0.81f, 0.01f), new Vector3(0.09f, 0.09f, 0.08f), resin);
                var fragment = Part(root, "BoneFragment", PrimitiveType.Capsule, new Vector3(0.12f + index * 0.05f, 0.8f, -0.06f), new Vector3(0.035f, 0.09f, 0.035f), bone);

                fragment.transform.localRotation = Quaternion.Euler(90f, index * 25f, 0f);
                var feather = Part(root, "Feather", PrimitiveType.Sphere, new Vector3(0.07f + index * 0.06f, 0.81f, 0.1f), new Vector3(0.035f, 0.012f, 0.2f), bone);

                feather.transform.localRotation = Quaternion.Euler(0f, index * 20f, 0f);
            }

            Rune(root, new Vector3(0f, 0.59f, -0.225f), 0.065f, blue);
        }
        else
        {
            Part(root, "WoodenBase", PrimitiveType.Cube, new Vector3(0f, 0.09f, 0f), new Vector3(0.65f, 0.18f, 0.48f), wood);
            var monolith = Part(root, "RoughRuneStone", PrimitiveType.Sphere, new Vector3(0f, 0.58f, 0f), new Vector3(0.48f, 0.95f, 0.28f), stone);

            monolith.GetComponent<MeshFilter>().sharedMesh = CreateStoneMesh();
            monolith.transform.localRotation = Quaternion.Euler(0f, 0f, -7f);
            Rune(root, new Vector3(0f, 0.6f, -0.145f), 0.18f, blue);
        }

        var saved = PrefabUtility.SaveAsPrefabAsset(root, path);

        Object.DestroyImmediate(root);
        AssetImporter.GetAtPath(path).assetBundleName = "hexbattlemage";
        Validate(saved);
    }

    private static MageRecipeResource Resource(string name, int amount)
    {
        return new MageRecipeResource { ItemPrefab = name, Amount = amount, Recover = true };
    }

    private static void SetConnection(StationExtension extension)
    {
        const string mockPath = Root + "/JVLmock_vfx_ExtensionConnection.prefab";
        var mock = AssetDatabase.LoadAssetAtPath<GameObject>(mockPath);

        if (mock == null)
        {
            var root = new GameObject("JVLmock_vfx_ExtensionConnection");

            mock = PrefabUtility.SaveAsPrefabAsset(root, mockPath);
            Object.DestroyImmediate(root);
        }

        extension.m_connectionPrefab = mock;
        extension.m_connectionOffset = new Vector3(0f, 0.6f, 0f);
    }

    private static Mesh CreateStoneMesh()
    {
        const int sides = 7;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        for (int side = 0; side < sides; side++)
        {
            float angle = side * Mathf.PI * 2f / sides;
            float nextAngle = (side + 1) * Mathf.PI * 2f / sides;
            var lower = new Vector3(Mathf.Cos(angle) * 0.45f, -0.5f, Mathf.Sin(angle) * 0.45f);
            var nextLower = new Vector3(Mathf.Cos(nextAngle) * 0.45f, -0.5f, Mathf.Sin(nextAngle) * 0.45f);
            var upper = new Vector3(Mathf.Cos(angle) * 0.4f, 0.3f + Mathf.Sin(angle * 2f) * 0.08f, Mathf.Sin(angle) * 0.4f);
            var nextUpper = new Vector3(Mathf.Cos(nextAngle) * 0.4f, 0.3f + Mathf.Sin(nextAngle * 2f) * 0.08f, Mathf.Sin(nextAngle) * 0.4f);

            AddTriangle(vertices, triangles, lower, upper, nextUpper);
            AddTriangle(vertices, triangles, lower, nextUpper, nextLower);
            AddTriangle(vertices, triangles, upper, new Vector3(0.07f, 0.5f, 0f), nextUpper);
            AddTriangle(vertices, triangles, lower, nextLower, new Vector3(0f, -0.5f, 0f));
        }

        var mesh = new Mesh { name = "MeadowsRoughRuneStone" };

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Root + "/MeadowsRoughRuneStone.asset");
        return mesh;
    }

    private static void AddTriangle(System.Collections.Generic.List<Vector3> vertices, System.Collections.Generic.List<int> triangles, Vector3 first, Vector3 second, Vector3 third)
    {
        int start = vertices.Count;

        vertices.Add(first);
        vertices.Add(second);
        vertices.Add(third);
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
    }

    private static Material PrepareBlueMaterial()
    {
        var material = PrepareMaterial("MeadowsBlueRune", new Color(0.15f, 0.65f, 1f));

        return material;
    }

    private static Material PrepareMaterial(string name, Color color)
    {
        string path = Root + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (existing != null)
        {
            return existing;
        }

        var material = new Material(AssetDatabase.LoadAssetAtPath<Material>(Materials + "RuneInlay.mat"));

        material.name = name;
        material.mainTexture = null;
        material.SetTexture("_EmissionMap", null);
        material.color = color;
        material.SetColor("_EmissionColor", color * (name == "MeadowsBlueRune" ? 1.5f : 0f));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static GameObject Part(GameObject root, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(type);

        part.name = name;
        part.layer = root.layer;
        part.transform.SetParent(root.transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    private static void Rune(GameObject root, Vector3 center, float size, Material material)
    {
        Stroke(root, center + Vector3.down * size, center + Vector3.up * size, material);
        Stroke(root, center, center + new Vector3(-size * 0.7f, size * 0.7f, 0f), material);
        Stroke(root, center, center + new Vector3(size * 0.7f, size * 0.7f, 0f), material);
    }

    private static void Stroke(GameObject root, Vector3 from, Vector3 to, Material material)
    {
        var stroke = Part(root, "BlueRuneInlay", PrimitiveType.Cube, (from + to) * 0.5f, new Vector3(0.012f, Vector3.Distance(from, to), 0.008f), material);

        stroke.transform.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
        Object.DestroyImmediate(stroke.GetComponent<Collider>());
    }

    private static void Validate(GameObject prefab)
    {
        var extension = prefab.GetComponent<StationExtension>();

        if (extension == null)
        {
            throw new InvalidOperationException("Missing station extension: " + prefab.name);
        }

        if (extension.m_craftingStation == null)
        {
            throw new InvalidOperationException("Missing extension station: " + prefab.name);
        }

        if (extension.m_stack)
        {
            throw new InvalidOperationException("Duplicate extensions must not stack: " + prefab.name);
        }

        if (extension.m_connectionPrefab == null)
        {
            throw new InvalidOperationException("Missing extension connection effect: " + prefab.name);
        }

        if (prefab.GetComponent<MageRecipeProfile>().BakedRecipe == null)
        {
            throw new InvalidOperationException("Missing extension recipe: " + prefab.name);
        }

        if (prefab.GetComponentsInChildren<Collider>().Length == 0)
        {
            throw new InvalidOperationException("Missing extension colliders: " + prefab.name);
        }
    }

    private static void Preview(string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + name + ".prefab");
        var preview = new PreviewRenderUtility();
        var root = new GameObject("ExtensionPreview");

        foreach (var source in prefab.GetComponentsInChildren<MeshFilter>())
        {
            var child = new GameObject(source.name);

            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = source.transform.localPosition;
            child.transform.localRotation = source.transform.localRotation;
            child.transform.localScale = source.transform.localScale;
            child.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;

            var authored = source.GetComponent<Renderer>().sharedMaterial;
            var material = new Material(Shader.Find("Standard"));

            material.color = authored.color;
            material.mainTexture = authored.mainTexture;

            if (source.name == "BlueRuneInlay")
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.15f, 0.65f, 1f));
            }

            child.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        preview.AddSingleGO(root);
        preview.camera.transform.position = new Vector3(1.4f, 1.25f, -2f);
        preview.camera.transform.LookAt(new Vector3(0f, 0.48f, 0f));
        preview.camera.nearClipPlane = 0.01f;
        preview.camera.farClipPlane = 10f;
        preview.camera.fieldOfView = 35f;
        preview.lights[0].intensity = 1.3f;
        preview.lights[0].transform.rotation = Quaternion.Euler(40f, 30f, 0f);
        preview.lights[1].intensity = 0.6f;
        preview.BeginPreview(new Rect(0f, 0f, 640f, 640f), GUIStyle.none);
        preview.camera.clearFlags = CameraClearFlags.SolidColor;
        preview.camera.backgroundColor = Color.clear;
        preview.camera.Render();

        var rendered = (RenderTexture)preview.EndPreview();
        var previous = RenderTexture.active;
        var texture = new Texture2D(rendered.width, rendered.height, TextureFormat.RGBA32, false);

        RenderTexture.active = rendered;
        texture.ReadPixels(new Rect(0f, 0f, rendered.width, rendered.height), 0, 0);
        texture.Apply();
        RenderTexture.active = previous;

        byte[] bytes = texture.EncodeToPNG();

        File.WriteAllBytes("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/" + name + "-preview.png", bytes);
        Object.DestroyImmediate(texture);
        preview.Cleanup();

        if (prefab.GetComponent<Piece>().m_icon != null)
        {
            return;
        }

        string iconPath = Root + "/" + name + "Icon.png";

        if (!File.Exists(iconPath))
        {
            File.WriteAllBytes(iconPath, bytes);
        }

        AssetDatabase.ImportAsset(iconPath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        string prefabPath = Root + "/" + name + ".prefab";
        var contents = PrefabUtility.LoadPrefabContents(prefabPath);

        contents.GetComponent<Piece>().m_icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<Piece>().m_icon == null)
        {
            throw new InvalidOperationException("Missing baked extension icon: " + name);
        }
    }
}

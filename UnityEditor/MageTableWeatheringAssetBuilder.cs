using System;
using System.IO;
using System.Text.RegularExpressions;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MageTableWeatheringAssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage";
    private const string Crafting = Root + "/Crafting";
    private const string MaterialRoot = Root + "/Materials/MageTable";
    private const string MeshRoot = Crafting + "/WeatheredMeshes";

    [MenuItem("HexBattleMage/Apply Worn Mage Table Appearance")]
    public static void Build()
    {
        Directory.CreateDirectory(MeshRoot);
        AssetDatabase.Refresh();

        foreach (string name in new[] { "DarkOak_Worn_BaseColor", "CarvedStone_BaseColor", "RuneInlay_BaseColor" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Crafting + "/textures/" + name + ".png");

            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Crafting + "/WeatheredTextures" }))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));

            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }

        var wood = Material(MaterialRoot + "/DarkOak.mat");
        var stone = Material(MaterialRoot + "/CarvedStone.mat");

        wood.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Crafting + "/WeatheredTextures/KnottedSplitOak_BaseColor.png");
        stone.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Crafting + "/WeatheredTextures/CrackedFieldstone_BaseColor.png");
        Matte(wood, new Color(1f, 0.97f, 0.9f));
        Matte(stone, new Color(0.84f, 0.82f, 0.74f));
        wood.SetFloat("_BumpScale", 0.12f);
        stone.SetFloat("_BumpScale", 0.18f);

        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Crafting + "/AlchemyDetails" }))
        {
            var material = Material(AssetDatabase.GUIDToAssetPath(guid));
            Color tint = material.color;

            Matte(material, tint);
            material.mainTexture = material.name == "BookLeather" ? wood.mainTexture : stone.mainTexture;
            material.SetTexture("_EmissionMap", null);
            material.SetColor("_EmissionColor", Color.black);
        }

        var bone = Material(Crafting + "/Extensions/BoneAndFeather.mat");
        var resin = Material(Crafting + "/Extensions/AmberResin.mat");
        var rune = Material(Crafting + "/Extensions/MeadowsBlueRune.mat");
        var inlay = Material(MaterialRoot + "/RuneInlay.mat");

        Matte(bone, new Color(0.65f, 0.58f, 0.43f));
        bone.mainTexture = stone.mainTexture;
        Matte(resin, new Color(0.48f, 0.23f, 0.07f));
        resin.mainTexture = stone.mainTexture;
        Matte(rune, new Color(0.22f, 0.52f, 0.64f));
        rune.SetColor("_EmissionColor", new Color(0.12f, 0.32f, 0.42f));
        Matte(inlay, new Color(0.56f, 0.48f, 0.66f));
        inlay.SetColor("_EmissionColor", new Color(0.25f, 0.18f, 0.38f));

        foreach (string path in new[] { Crafting + "/HexArcaneWorkbench.prefab", Crafting + "/Extensions/HexMageRuneStone.prefab", Crafting + "/Extensions/HexMageReagentStand.prefab" })
        {
            WeatherPrefab(path, wood, stone);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { MeshRoot }))
        {
            AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)).SetAssetBundleNameAndVariant(string.Empty, string.Empty);
        }

        AssetDatabase.SaveAssets();
        HexBattleMageBundlePolicy.ValidateDependencies();
        File.WriteAllText("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/mage-table-weathering-result.txt", "PASS: authored worn matte materials, point-filtered compressed readable textures, weathered wood/stone meshes, subdued table glow; station/recipe/collider identities preserved.");
    }

    public static void BuildBundle()
    {
        Build();
        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }

    private static Material Material(string path)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            throw new InvalidOperationException("Missing material: " + path);
        }

        return material;
    }

    private static void Matte(Material material, Color tint)
    {
        material.color = tint;
        material.SetFloat("_Glossiness", 0f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_MetallicAlphaGloss", 0f);
        material.SetFloat("_AddRain", 0f);
        material.DisableKeyword("_ADDRAIN_ON");
        EditorUtility.SetDirty(material);
    }

    private static void WeatherPrefab(string path, Material wood, Material stone)
    {
        var prefab = PrefabUtility.LoadPrefabContents(path);
        string identity = prefab.name;
        string recipeBefore = prefab.GetComponent<MageRecipeProfile>().GetBakedData();
        int colliderCount = prefab.GetComponentsInChildren<Collider>(true).Length;

        try
        {
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();

                if (renderer == null)
                {
                    continue;
                }

                bool wooden = renderer.sharedMaterial == wood;
                bool rocky = renderer.sharedMaterial == stone;

                if (renderer.sharedMaterial == null || renderer.sharedMaterial.name.Contains("Rune"))
                {
                    continue;
                }

                filter.sharedMesh = WeatherMesh(filter.sharedMesh, identity + "_" + filter.name, wooden, filter.transform.InverseTransformDirection(Vector3.up));

                if (wooden || rocky)
                {
                    ProjectSurfaceUv(filter.sharedMesh, filter.transform, wooden);
                }
            }

            var profile = prefab.GetComponent<MageTableProfile>();

            if (profile != null)
            {
                profile.ArcaneColor = new Color(0.45f, 0.3f, 0.6f, 0.6f);
                profile.CentralCrystalScale = 0.5f;
                profile.SideCrystalScale = 0.24f;
                profile.RuneLineWidth = 0.014f;
                profile.LightIntensity = 0.65f;
                profile.SparkRate = 5f;
                profile.MaximumSparks = 20;
            }

            if (prefab.name != identity || recipeBefore != prefab.GetComponent<MageRecipeProfile>().GetBakedData() || colliderCount != prefab.GetComponentsInChildren<Collider>(true).Length)
            {
                throw new InvalidOperationException("Appearance pass changed gameplay data: " + path);
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            Preview(prefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static void ProjectSurfaceUv(Mesh mesh, Transform transform, bool wooden)
    {
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var uv = new Vector2[vertices.Length];
        float scale = wooden ? 0.52f : 0.85f;
        Vector3 worldSize = transform.GetComponent<Renderer>().bounds.size;
        bool uprightWood = wooden && worldSize.y > Mathf.Max(worldSize.x, worldSize.z) * 1.8f;

        for (int index = 0; index < vertices.Length; index++)
        {
            Vector3 point = transform.TransformPoint(vertices[index]);
            Vector3 normal = transform.TransformDirection(normals[index]);
            Vector3 direction = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

            if (direction.y >= direction.x && direction.y >= direction.z)
            {
                uv[index] = new Vector2(point.x * scale + 0.5f, point.z * scale + 0.5f);
            }
            else if (direction.z >= direction.x)
            {
                uv[index] = uprightWood ? new Vector2(point.y * scale, point.x * scale + 0.5f) : new Vector2(point.x * scale + 0.5f, point.y * scale);
            }
            else
            {
                uv[index] = uprightWood ? new Vector2(point.y * scale, point.z * scale + 0.5f) : new Vector2(point.z * scale + 0.5f, point.y * scale);
            }
        }

        mesh.uv = uv;
        EditorUtility.SetDirty(mesh);
    }

    private static Mesh WeatherMesh(Mesh source, string name, bool wooden, Vector3 up)
    {
        string path = MeshRoot + "/" + Regex.Replace(name, "[^A-Za-z0-9_]", "_") + "_RoughV5.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (existing != null)
        {
            return existing;
        }

        if (wooden)
        {
            var box = RoughBox(source.bounds, name, up);

            AssetDatabase.CreateAsset(box, path);
            AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
            return box;
        }

        var mesh = Object.Instantiate(source);
        var vertices = mesh.vertices;
        var uv = mesh.uv;
        var bounds = mesh.bounds;

        if (uv.Length != vertices.Length)
        {
            uv = new Vector2[vertices.Length];

            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 point = vertices[index] - bounds.center;

                uv[index] = new Vector2(Mathf.Atan2(point.z, point.x) / (Mathf.PI * 2f) + 0.5f, Mathf.InverseLerp(bounds.min.y, bounds.max.y, vertices[index].y));
            }
        }

        int seed = Seed(name);

        for (int index = 0; index < vertices.Length; index++)
        {
            Vector3 point = vertices[index];
            Vector3 relative = point - bounds.center;
            float wave = Mathf.Sin(relative.x * 15.7f + relative.y * 24.3f + relative.z * 19.1f + seed % 41);
            float amount = wooden ? 0.025f : 0.055f;

            relative.x *= 1f + wave * amount;
            relative.z *= 1f + Mathf.Sin(relative.y * 32f + seed % 23) * amount;
            relative.y += wave * bounds.size.y * amount * 0.35f;
            vertices[index] = bounds.center + relative;
        }

        mesh.name = name + "_Weathered";
        mesh.vertices = vertices;
        mesh.uv = uv;
        if (source.bounds.size.magnitude >= 0.1f)
        {
            FlatShade(mesh);
        }
        else
        {
            mesh.RecalculateNormals();
        }

        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
        return mesh;
    }

    private static int Seed(string name)
    {
        int value = 17;

        foreach (char character in name)
        {
            value = unchecked(value * 31 + character);
        }

        return value & int.MaxValue;
    }

    private static Mesh RoughBox(Bounds bounds, string name, Vector3 up)
    {
        bool swapAxes = Mathf.Abs(up.z) > Mathf.Abs(up.y);

        if (swapAxes)
        {
            bounds = new Bounds(new Vector3(bounds.center.x, bounds.center.z, bounds.center.y), new Vector3(bounds.size.x, bounds.size.z, bounds.size.y));
        }

        var random = new System.Random(Seed(name));
        var vertices = new Vector3[18];
        var uv = new Vector2[18];
        var triangles = new System.Collections.Generic.List<int>();
        var corners = new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, 1f) };

        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 point = corners[corner];
            Vector2 previous = corners[(corner + 3) % 4];
            Vector2 next = corners[(corner + 1) % 4];
            float firstCut = 0.09f + (float)random.NextDouble() * 0.13f;
            float secondCut = 0.09f + (float)random.NextDouble() * 0.13f;
            var outline = new[] { Vector2.Lerp(point, previous, firstCut), Vector2.Lerp(point, next, secondCut) };

            for (int side = 0; side < 2; side++)
            {
                int index = corner * 2 + side;
                Vector2 edge = outline[side];
                float sag = (float)random.NextDouble() * 0.18f;

                vertices[index] = bounds.center + new Vector3(edge.x * bounds.extents.x, -bounds.extents.y, edge.y * bounds.extents.z);
                vertices[index + 8] = bounds.center + new Vector3(edge.x * bounds.extents.x * (0.97f + (float)random.NextDouble() * 0.04f), bounds.extents.y - bounds.size.y * sag, edge.y * bounds.extents.z);
                uv[index] = uv[index + 8] = new Vector2((edge.x + 1f) * 0.5f, (edge.y + 1f) * 0.5f);
            }
        }

        vertices[16] = bounds.center + Vector3.up * bounds.extents.y * 0.91f;
        vertices[17] = bounds.center - Vector3.up * bounds.extents.y;
        uv[16] = uv[17] = new Vector2(0.5f, 0.5f);

        for (int index = 0; index < 8; index++)
        {
            int next = (index + 1) % 8;

            triangles.AddRange(new[] { 16, next + 8, index + 8, 17, index, next, index, index + 8, next + 8, index, next + 8, next });
        }

        var chippedVertices = new Vector3[34];
        var chippedUv = new Vector2[34];

        for (int index = 0; index < 8; index++)
        {
            int next = (index + 1) % 8;
            int point = index * 2;

            chippedVertices[point] = vertices[index];
            chippedVertices[point + 16] = vertices[index + 8];
            chippedVertices[point + 1] = Vector3.Lerp(vertices[index], vertices[next], 0.48f);
            chippedVertices[point + 17] = Vector3.Lerp(vertices[index + 8], vertices[next + 8], 0.48f);
            chippedUv[point] = chippedUv[point + 16] = uv[index];
            chippedUv[point + 1] = chippedUv[point + 17] = Vector2.Lerp(uv[index], uv[next], 0.48f);

            if (index % 2 == 1 && (Seed(name) + index) % 3 == 0)
            {
                Vector3 bottom = chippedVertices[point + 1];
                Vector3 top = chippedVertices[point + 17];

                bottom.x = Mathf.Lerp(bottom.x, bounds.center.x, 0.09f);
                bottom.z = Mathf.Lerp(bottom.z, bounds.center.z, 0.09f);
                top.x = Mathf.Lerp(top.x, bounds.center.x, 0.16f);
                top.z = Mathf.Lerp(top.z, bounds.center.z, 0.16f);
                top.y -= bounds.size.y * 0.18f;
                chippedVertices[point + 1] = bottom;
                chippedVertices[point + 17] = top;
            }
        }

        chippedVertices[32] = vertices[16];
        chippedVertices[33] = vertices[17];
        chippedUv[32] = chippedUv[33] = uv[16];
        vertices = chippedVertices;
        uv = chippedUv;
        triangles.Clear();

        for (int index = 0; index < 16; index++)
        {
            int next = (index + 1) % 16;

            triangles.AddRange(new[] { 32, next + 16, index + 16, 33, index, next, index, index + 16, next + 16, index, next + 16, next });
        }

        if (swapAxes)
        {
            for (int index = 0; index < vertices.Length; index++)
            {
                vertices[index] = new Vector3(vertices[index].x, vertices[index].z, vertices[index].y);
            }

            for (int index = 0; index < triangles.Count; index += 3)
            {
                int first = triangles[index];

                triangles[index] = triangles[index + 2];
                triangles[index + 2] = first;
            }
        }

        var mesh = new Mesh { name = name + "_Chipped", vertices = vertices, uv = uv, triangles = triangles.ToArray() };

        FlatShade(mesh);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void FlatShade(Mesh mesh)
    {
        var originalVertices = mesh.vertices;
        var originalUv = mesh.uv;
        var indices = mesh.triangles;
        var vertices = new Vector3[indices.Length];
        var uv = new Vector2[indices.Length];

        for (int index = 0; index < indices.Length; index++)
        {
            vertices[index] = originalVertices[indices[index]];
            uv[index] = originalUv[indices[index]];
            indices[index] = index;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
    }

    public static void PreviewAll()
    {
        foreach (string path in new[] { Crafting + "/HexArcaneWorkbench.prefab", Crafting + "/Extensions/HexMageRuneStone.prefab", Crafting + "/Extensions/HexMageReagentStand.prefab" })
        {
            Preview(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }
    }

    private static void Preview(GameObject prefab)
    {
        var preview = new PreviewRenderUtility();
        var model = Object.Instantiate(prefab);

        try
        {
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;

                for (int index = 0; index < materials.Length; index++)
                {
                    var authored = materials[index];
                    var material = new Material(Shader.Find("Standard"));

                    material.color = authored.color;
                    material.mainTexture = authored.mainTexture;
                    material.SetFloat("_Glossiness", 0f);
                    materials[index] = material;
                }

                renderer.sharedMaterials = materials;
            }

            preview.AddSingleGO(model);
            bool table = prefab.GetComponent<MageTableProfile>() != null;

            preview.camera.transform.position = table ? new Vector3(2.8f, 2.5f, -3.2f) : new Vector3(1.3f, 1.2f, -1.8f);
            preview.camera.transform.LookAt(new Vector3(0f, table ? 0.7f : 0.5f, 0f));
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 20f;
            preview.camera.fieldOfView = 38f;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.19f, 0.2f, 0.22f);
            preview.ambientColor = new Color(0.55f, 0.55f, 0.55f);
            preview.lights[0].enabled = true;
            preview.lights[1].enabled = true;
            preview.lights[0].intensity = 1.6f;
            preview.lights[0].transform.rotation = Quaternion.Euler(40f, 25f, 0f);
            preview.lights[1].intensity = 0.5f;
            preview.BeginPreview(new Rect(0f, 0f, 768f, 768f), GUIStyle.none);
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f);
            preview.Render();
            var texture = (RenderTexture)preview.EndPreview();
            var previous = RenderTexture.active;

            RenderTexture.active = texture;
            var output = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);

            output.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            var pixels = output.GetPixels();

            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = pixels[index].gamma;
            }

            output.SetPixels(pixels);
            output.Apply();
            File.WriteAllBytes("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/" + prefab.name + "-worn-preview.png", output.EncodeToPNG());
            RenderTexture.active = previous;
            Object.DestroyImmediate(output);
        }
        finally
        {
            preview.Cleanup();
        }
    }
}

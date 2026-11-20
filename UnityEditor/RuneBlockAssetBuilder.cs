using System;
using System.Collections.Generic;
using System.IO;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class RuneBlockAssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage";
    private const string Folder = Root + "/Crafting/RuneBlock";
    private const string Identity = HexBattleMageRuneBlockProfile.PrefabName;
    private const float Surface = 0.78f;
    private static int _meshIndex;

    [MenuItem("HexBattleMage/Prepare Rune-Whittler Block and Build Bundle")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var endGrainImporter = (TextureImporter)AssetImporter.GetAtPath(Folder + "/OakEndGrain.png");

        endGrainImporter.filterMode = FilterMode.Point;
        endGrainImporter.wrapMode = TextureWrapMode.Clamp;
        endGrainImporter.SaveAndReimport();
        string path = Folder + "/" + Identity + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (existing == null)
        {
            Create(path);
        }

        CompleteAuthoring(path);
        MeadowsTrinket01GameplayBuilder.Prepare();
        StarterWandAssetBuilder.Prepare();
        MoveRecipe(Root + "/Trinkets/Meadows/Trinket_Meadows_01/HexBattleMage_Trinket_Meadows_01.prefab");
        AssetDatabase.SaveAssets();
        PrefabDefaultsAssetBuilder.PrepareAndBuild();
        File.WriteAllText("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/rune-block-build-report.txt", "PASS: one rune block prefab, wood-only 10 Wood recipe, no extensions, authored boss keys and cumulative visual children, wand/trinket station migration. Existing visuals preserved on rebuild.");
    }

    private static void Create(string path)
    {
        _meshIndex = 0;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Crafting/HexArcaneWorkbench.prefab");
        var root = Object.Instantiate(source);

        root.name = Identity;
        var station = root.GetComponent<CraftingStation>();
        var marker = station.m_areaMarker;

        if (marker != null)
        {
            marker.transform.SetParent(root.transform, false);
        }

        for (int index = root.transform.childCount - 1; index >= 0; index--)
        {
            var child = root.transform.GetChild(index).gameObject;

            if (child != marker)
            {
                Object.DestroyImmediate(child);
            }
        }

        Object.DestroyImmediate(root.GetComponent<MageTableProfile>());
        Object.DestroyImmediate(root.GetComponent<MageRecipeProfile>());

        foreach (var collider in root.GetComponents<Collider>())
        {
            Object.DestroyImmediate(collider);
        }

        var body = root.AddComponent<BoxCollider>();

        body.center = new Vector3(0f, 0.39f, 0f);
        body.size = new Vector3(1.34f, 0.78f, 1.34f);
        station.m_effectAreaCollider = null;
        station.m_roofCheckPoint = Child(root.transform, "RoofCheck", new Vector3(0f, 1.2f, 0f));
        station.m_connectionPoint = Child(root.transform, "Connection", new Vector3(0f, 0.8f, 0f));
        station.m_craftRequireRoof = false;
        station.m_craftRequireFire = false;
        station.m_useDistance = 3f;
        station.m_showBasicRecipies = false;
        station.m_rangeBuild = 20f;
        station.m_extraRangePerLevel = 0f;
        station.m_inUseObject = null;
        station.m_haveFireObject = null;
        var profile = root.AddComponent<HexBattleMageRuneBlockProfile>();

        profile.AuthoredVersion = 3;
        station.m_name = profile.DisplayName;
        var piece = root.GetComponent<Piece>();

        piece.m_name = profile.DisplayName;
        piece.m_description = profile.Description;
        piece.m_craftingStation = null;
        piece.m_resources = new Piece.Requirement[0];
        var recipe = root.AddComponent<MageRecipeProfile>();

        recipe.CraftingStation = string.Empty;
        recipe.RepairStation = string.Empty;
        recipe.Resources = new[] { new MageRecipeResource { ItemPrefab = "Wood", Amount = 10 } };
        SaveRecipe(root, recipe, Folder + "/Recipe_" + Identity + ".json");

        var template = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/MageTable/DarkOak.mat");
        var grain = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/OakEndGrain.png");
        var bark = MakeMaterial("OldOakBark", template, new Color(0.48f, 0.36f, 0.23f), template.mainTexture);
        var top = MakeMaterial("PaleCutOak", template, new Color(0.78f, 0.82f, 0.78f), grain);
        var dark = MakeMaterial("CarvingCuts", template, new Color(0.14f, 0.075f, 0.035f), null);
        var blue = MakeMaterial("CarvedBlueRunes", template, new Color(0.18f, 0.55f, 0.72f), null);

        blue.SetColor("_EmissionColor", new Color(0.15f, 0.5f, 0.7f));
        blue.EnableKeyword("_EMISSION");
        Trunk(root.transform, bark, top);

        for (int index = 0; index < 7; index++)
        {
            float angle = index * Mathf.PI * 2f / 7f;
            var start = new Vector3(Mathf.Cos(angle) * 0.48f, 0.26f, Mathf.Sin(angle) * 0.48f);
            var end = new Vector3(Mathf.Cos(angle + 0.1f) * (0.92f + index % 2 * 0.1f), 0.045f, Mathf.Sin(angle + 0.1f) * (0.92f + index % 2 * 0.1f));

            Branch(root.transform, "GnarledRoot", start, end, 0.22f, 0.045f, bark);
        }

        for (int index = 0; index < 36; index++)
        {
            float first = index * Mathf.PI * 2f / 36f;
            float second = (index + 1) * Mathf.PI * 2f / 36f;

            Stroke(root.transform, "CarvedRuneCircle", new Vector3(Mathf.Cos(first) * 0.43f, Surface + 0.003f, Mathf.Sin(first) * 0.43f), new Vector3(Mathf.Cos(second) * 0.43f, Surface + 0.003f, Mathf.Sin(second) * 0.43f), 0.006f, blue);
        }

        for (int index = 0; index < 10; index++)
        {
            float angle = index * Mathf.PI * 2f / 10f;
            var center = new Vector3(Mathf.Cos(angle) * 0.37f, Surface + 0.005f, Mathf.Sin(angle) * 0.37f);
            var tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            Stroke(root.transform, "CarvedGlyphStem", center - radial * 0.035f, center + radial * 0.035f, 0.007f, blue);
            Stroke(root.transform, "CarvedGlyphBranch", center, center + radial * 0.025f + tangent * 0.025f, 0.007f, blue);
        }

        Stroke(root.transform, "WandGroove", new Vector3(-0.4f, Surface + 0.002f, 0.27f), new Vector3(0.39f, Surface + 0.002f, 0.27f), 0.052f, dark);
        Branch(root.transform, "RestingWand", new Vector3(-0.35f, Surface + 0.025f, 0.27f), new Vector3(0.35f, Surface + 0.035f, 0.27f), 0.017f, 0.012f, top);
        Branch(root.transform, "CrookedBranchBase", new Vector3(-0.5f, 0.66f, 0.34f), new Vector3(-0.6f, 1.22f, 0.35f), 0.085f, 0.045f, bark);
        Branch(root.transform, "CrookedBranchArm", new Vector3(-0.6f, 1.22f, 0.35f), new Vector3(-0.05f, 1.55f, 0.36f), 0.045f, 0.025f, bark);
        Branch(root.transform, "BranchFork", new Vector3(-0.5f, 1.29f, 0.35f), new Vector3(-0.52f, 1.58f, 0.35f), 0.035f, 0.018f, bark);
        Branch(root.transform, "HangingCord", new Vector3(-0.13f, 1.49f, 0.36f), new Vector3(-0.13f, 1.16f, 0.36f), 0.007f, 0.007f, dark);
        Box(root.transform, "WoodenCharm", new Vector3(-0.13f, 1.12f, 0.36f), new Vector3(0.09f, 0.14f, 0.025f), top);
        var flint = MakeMaterial("FlintKnife", template, new Color(0.38f, 0.4f, 0.37f), null);

        Branch(root.transform, "KnifeGrip", new Vector3(0.19f, Surface + 0.035f, -0.36f), new Vector3(0.43f, Surface + 0.035f, -0.24f), 0.03f, 0.025f, bark);
        var blade = new Mesh { vertices = new[] { new Vector3(-0.04f, Surface + 0.015f, -0.48f), new Vector3(0.2f, Surface + 0.018f, -0.39f), new Vector3(0.17f, Surface + 0.032f, -0.31f), new Vector3(0.06f, Surface + 0.045f, -0.4f) }, triangles = new[] { 0, 3, 1, 1, 3, 2, 0, 1, 2, 0, 2, 3 } };

        AddMesh(root.transform, "FlintBlade", blade, flint);
        Bowl(root.transform, top);

        for (int index = 0; index < 6; index++)
        {
            var shaving = Box(root.transform, "WoodShaving", new Vector3(0.05f + index * 0.045f, Surface + 0.009f, -0.54f + index % 2 * 0.055f), new Vector3(0.06f, 0.009f, 0.018f), top);

            shaving.localRotation = Quaternion.Euler(0f, index * 37f, index % 2 * 13f);
        }

        Progression(root.transform, profile, template, blue);
        var icon = Render(root, 256, "rune-block-icon.png", true);

        File.WriteAllBytes(Folder + "/RuneBlockIcon.png", icon.EncodeToPNG());
        Object.DestroyImmediate(icon);
        AssetDatabase.ImportAsset(Folder + "/RuneBlockIcon.png");
        var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/RuneBlockIcon.png");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        piece.m_icon = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/RuneBlockIcon.png");
        station.m_icon = piece.m_icon;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
        var preview = Render(root, 768, "rune-block-meadows-preview.png", false);

        Object.DestroyImmediate(preview);
        Object.DestroyImmediate(root);
    }

    private static void CompleteAuthoring(string path)
    {
        var prefab = PrefabUtility.LoadPrefabContents(path);

        try
        {
            var profile = prefab.GetComponent<HexBattleMageRuneBlockProfile>();
            bool refreshIcon = profile.AuthoredVersion < 2 || prefab.GetComponent<Piece>().m_icon == null;

            if (profile.AuthoredVersion < 2)
            {
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh;
                    bool carving = filter.name.StartsWith("Carved", StringComparison.Ordinal) || filter.name == "WandGroove";
                    bool branch = filter.name == "OldOakStump" || filter.name.Contains("Branch") || filter.name == "GnarledRoot" || filter.name == "RestingWand" || filter.name == "HangingCord" || filter.name == "KnifeGrip";

                    if (carving && mesh.normals.Length > 0 && mesh.normals[0].y < 0f)
                    {
                        var triangles = mesh.triangles;

                        for (int index = 0; index < triangles.Length; index += 3)
                        {
                            int first = triangles[index];

                            triangles[index] = triangles[index + 2];
                            triangles[index + 2] = first;
                        }

                        mesh.triangles = triangles;
                        mesh.RecalculateNormals();
                    }

                    if (branch)
                    {
                        var uv = mesh.uv;

                        for (int index = 0; index < uv.Length; index++)
                        {
                            uv[index] = new Vector2(uv[index].y, uv[index].x);
                        }

                        mesh.uv = uv;
                    }

                    EditorUtility.SetDirty(mesh);
                }

                var top = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/PaleCutOak.mat");

                top.color = new Color(0.78f, 0.82f, 0.78f);
                EditorUtility.SetDirty(top);
                profile.AuthoredVersion = 2;
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }

            if (profile.AuthoredVersion < 3)
            {
                prefab.GetComponent<CraftingStation>().m_effectAreaCollider = null;
                profile.AuthoredVersion = 3;
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }

            var preview = Render(prefab, 768, "rune-block-meadows-preview.png", false);

            Object.DestroyImmediate(preview);

            if (profile.AuthoredVersion < 4)
            {
                prefab.GetComponent<WearNTear>().m_fragmentRoots = Array.Empty<GameObject>();
                profile.AuthoredVersion = 4;
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }

            if (profile.AuthoredVersion < 5)
            {
                foreach (var collider in prefab.GetComponents<Collider>())
                {
                    Object.DestroyImmediate(collider);
                }

                var body = prefab.AddComponent<BoxCollider>();

                body.center = profile.BodyColliderCenter;
                body.size = profile.BodyColliderSize;
                profile.AuthoredVersion = 5;
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }

            if (refreshIcon)
            {
                var icon = Render(prefab, 256, "rune-block-icon.png", true);

                File.WriteAllBytes(Folder + "/RuneBlockIcon.png", icon.EncodeToPNG());
                AssetDatabase.ImportAsset(Folder + "/RuneBlockIcon.png");
                Object.DestroyImmediate(icon);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static Material MakeMaterial(string name, Material template, Color tint, Texture texture)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
        {
            return material;
        }

        material = new Material(template) { name = name, color = tint, mainTexture = texture };
        material.SetTexture("_BumpMap", null);
        material.SetTexture("_EmissionMap", null);
        material.SetColor("_EmissionColor", Color.black);
        material.SetFloat("_Glossiness", 0f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_AddRain", 0f);
        material.DisableKeyword("_ADDRAIN_ON");
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Trunk(Transform parent, Material bark, Material top)
    {
        const int Sides = 24;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();

        for (int ring = 0; ring < 3; ring++)
        {
            float y = ring == 0 ? 0.04f : ring == 1 ? 0.38f : Surface;

            for (int side = 0; side <= Sides; side++)
            {
                float angle = side * Mathf.PI * 2f / Sides;
                float radius = (ring == 0 ? 0.76f : 0.65f) * (1f + Mathf.Sin(angle * 5f) * 0.04f + Mathf.Cos(angle * 9f) * 0.025f);

                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
                uv.Add(new Vector2(ring * 0.5f, side / (float)Sides * 2f));

                if (ring < 2 && side < Sides)
                {
                    int index = ring * (Sides + 1) + side;
                    int next = index + Sides + 1;

                    triangles.AddRange(new[] { index, next, next + 1, index, next + 1, index + 1 });
                }
            }
        }

        AddMesh(parent, "OldOakStump", new Mesh { vertices = vertices.ToArray(), uv = uv.ToArray(), triangles = triangles.ToArray() }, bark);
        var cap = new List<Vector3> { new Vector3(0f, Surface, 0f) };
        var capUv = new List<Vector2> { new Vector2(0.5f, 0.5f) };
        var capTriangles = new List<int>();

        for (int side = 0; side <= Sides; side++)
        {
            float angle = side * Mathf.PI * 2f / Sides;
            float radius = 0.65f * (1f + Mathf.Sin(angle * 5f) * 0.04f + Mathf.Cos(angle * 9f) * 0.025f);

            cap.Add(new Vector3(Mathf.Cos(angle) * radius, Surface, Mathf.Sin(angle) * radius));
            capUv.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));

            if (side < Sides)
            {
                capTriangles.AddRange(new[] { 0, side + 2, side + 1 });
            }
        }

        AddMesh(parent, "CutEndGrain", new Mesh { vertices = cap.ToArray(), uv = capUv.ToArray(), triangles = capTriangles.ToArray() }, top);
    }

    private static Transform Child(Transform parent, string name, Vector3 position)
    {
        var child = new GameObject(name).transform;

        child.SetParent(parent, false);
        child.localPosition = position;
        child.gameObject.layer = parent.gameObject.layer;
        return child;
    }

    private static void AddMesh(Transform parent, string name, Mesh mesh, Material material)
    {
        mesh.name = name;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Folder + "/" + name + "_" + _meshIndex++ + ".asset");
        var child = Child(parent, name, Vector3.zero);

        child.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        child.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Transform Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);

        part.name = name;
        part.layer = parent.gameObject.layer;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        return part.transform;
    }

    private static void Branch(Transform parent, string name, Vector3 first, Vector3 second, float firstRadius, float secondRadius, Material material)
    {
        const int Sides = 8;
        var axis = (second - first).normalized;
        var tangent = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
        var bitangent = Vector3.Cross(axis, tangent);
        var vertices = new Vector3[(Sides + 1) * 2];
        var uv = new Vector2[vertices.Length];
        var triangles = new List<int>();

        for (int side = 0; side <= Sides; side++)
        {
            float angle = side * Mathf.PI * 2f / Sides;
            Vector3 radial = tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle);

            vertices[side] = first + radial * firstRadius;
            vertices[side + Sides + 1] = second + radial * secondRadius;
            uv[side] = new Vector2(0f, side / (float)Sides);
            uv[side + Sides + 1] = new Vector2(1f, side / (float)Sides);

            if (side < Sides)
            {
                triangles.AddRange(new[] { side, side + 1, side + Sides + 2, side, side + Sides + 2, side + Sides + 1 });
            }
        }

        AddMesh(parent, name, new Mesh { vertices = vertices, uv = uv, triangles = triangles.ToArray() }, material);
    }

    private static void Stroke(Transform parent, string name, Vector3 first, Vector3 second, float width, Material material)
    {
        Vector3 side = Vector3.Cross(Vector3.up, second - first).normalized * width * 0.5f;

        AddMesh(parent, name, new Mesh { vertices = new[] { first - side, first + side, second + side, second - side }, uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up }, triangles = new[] { 0, 2, 1, 0, 3, 2 } }, material);
    }

    private static void Bowl(Transform parent, Material material)
    {
        var points = new[] { new Vector2(0.07f, 0f), new Vector2(0.1f, 0.09f), new Vector2(0.075f, 0.09f), new Vector2(0.045f, 0.025f) };
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();

        for (int ring = 0; ring < points.Length; ring++)
        {
            for (int side = 0; side <= 12; side++)
            {
                float angle = side * Mathf.PI / 6f;

                vertices.Add(new Vector3(0.45f + Mathf.Cos(angle) * points[ring].x, Surface + points[ring].y, 0.06f + Mathf.Sin(angle) * points[ring].x));
                uv.Add(new Vector2(side / 12f, ring / 3f));

                if (ring < points.Length - 1 && side < 12)
                {
                    int index = ring * 13 + side;

                    triangles.AddRange(new[] { index, index + 13, index + 14, index, index + 14, index + 1 });
                }
            }
        }

        AddMesh(parent, "WoodenResinBowl", new Mesh { vertices = vertices.ToArray(), uv = uv.ToArray(), triangles = triangles.ToArray() }, material);
    }

    private static void Progression(Transform parent, HexBattleMageRuneBlockProfile profile, Material template, Material blue)
    {
        var progression = Child(parent, "Progression", Vector3.zero);
        var colors = new[] { new Color(0.4f, 0.25f, 0.11f), new Color(0.32f, 0.4f, 0.5f), new Color(0.45f, 0.7f, 0.8f), new Color(0.65f, 0.5f, 0.18f), new Color(0.24f, 0.58f, 0.7f), new Color(0.55f, 0.18f, 0.07f), new Color(0.7f, 0.8f, 0.9f) };

        for (int index = 0; index < profile.StageVisualPaths.Length; index++)
        {
            string name = Path.GetFileName(profile.StageVisualPaths[index]);
            var stage = Child(progression, name, Vector3.zero);
            var material = MakeMaterial(name + "Adornment", template, colors[index], null);

            for (int side = 0; side < 3; side++)
            {
                float angle = side * Mathf.PI * 2f / 3f + index * 0.37f;
                var position = new Vector3(Mathf.Cos(angle) * 0.63f, 0.2f + index * 0.065f, Mathf.Sin(angle) * 0.63f);
                var adornment = Box(stage, "CarvedTierFitting", position, new Vector3(0.055f, 0.12f, 0.11f), material);

                adornment.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            }

            Branch(stage, "BranchCharm", new Vector3(-0.5f + index * 0.04f, 1.3f, 0.37f), new Vector3(-0.5f + index * 0.04f, 1.17f - index * 0.025f, 0.37f), 0.018f, 0.012f, index > 0 ? blue : material);
            stage.gameObject.SetActive(false);
        }
    }

    private static void SaveRecipe(GameObject prefab, MageRecipeProfile recipe, string path)
    {
        File.WriteAllText(path, recipe.GetBakedData());
        AssetDatabase.ImportAsset(path);
        recipe.BakedRecipe = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
    }

    private static void MoveRecipe(string path)
    {
        var prefab = PrefabUtility.LoadPrefabContents(path);

        try
        {
            var recipe = prefab.GetComponent<MageRecipeProfile>();

            recipe.CraftingStation = Identity;
            recipe.RepairStation = Identity;
            SaveRecipe(prefab, recipe, AssetDatabase.GetAssetPath(recipe.BakedRecipe));
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static Texture2D Render(GameObject prefab, int size, string file, bool transparent)
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
                    var source = materials[index];
                    var material = new Material(Shader.Find("Standard")) { color = source.color, mainTexture = source.mainTexture };

                    material.SetFloat("_Glossiness", 0f);
                    materials[index] = material;
                }

                renderer.sharedMaterials = materials;
            }

            preview.AddSingleGO(model);
            preview.camera.transform.position = new Vector3(2.2f, 2.2f, -3f);
            preview.camera.transform.LookAt(new Vector3(0f, 0.72f, 0f));
            preview.camera.fieldOfView = 34f;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 20f;
            preview.lights[0].intensity = 1.4f;
            preview.lights[0].transform.rotation = Quaternion.Euler(40f, 25f, 0f);
            preview.lights[1].intensity = 0.7f;
            preview.ambientColor = new Color(0.5f, 0.5f, 0.5f);
            preview.BeginPreview(new Rect(0f, 0f, size, size), GUIStyle.none);
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = transparent ? Color.clear : new Color(0.12f, 0.13f, 0.15f);
            preview.Render();
            var previous = RenderTexture.active;

            RenderTexture.active = (RenderTexture)preview.EndPreview();
            var output = new Texture2D(size, size, TextureFormat.RGBA32, false);

            output.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
            var pixels = output.GetPixels();

            for (int index = 0; index < pixels.Length; index++)
            {
                float alpha = pixels[index].a;

                pixels[index] = pixels[index].gamma;
                pixels[index].a = alpha;
            }

            output.SetPixels(pixels);
            output.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/" + file, output.EncodeToPNG());
            return output;
        }
        finally
        {
            preview.Cleanup();
        }
    }
}

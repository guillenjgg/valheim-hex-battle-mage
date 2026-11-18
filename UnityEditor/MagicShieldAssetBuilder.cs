using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HexBattleMage.Core.Managers;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class MagicShieldAssetBuilder
{
    private const string AssetDirectory = "Assets/_CustomItems/HexBattleMage/Shields/Apprentice";
    private const string PrefabPath = AssetDirectory + "/HexMagicBracerVisual.prefab";
    private const string BundleName = "hexbattlemage";
    private const string BundleDirectory = "Assets/AssetBundles";
    private const string Workspace = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage";
    private const string ModBundlePath = "C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/hexbattlemage";
    private const int CircleSegments = 96;
    private const int CuffSegments = 24;
    private const float FieldRadius = 0.48f;
    private const string RequestPath = "Temp/HexMagicBracer.request";
    private static readonly Color RuneColor = new Color(0.25f, 0.8f, 1f, 0.85f);

    static MagicShieldAssetBuilder()
    {
        if (File.Exists(RequestPath))
        {
            EditorApplication.delayCall += RunRequest;
        }
    }

    private static void RunRequest()
    {
        File.Delete(RequestPath);
        PrepareAndBuild();
    }

    [MenuItem("HexBattleMage/Prepare Magic Bracer and Build Bundle")]
    public static void PrepareAndBuild()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            PrefabDefaultsAssetBuilder.PrepareAndBuild();
            File.WriteAllText(Workspace + "/magic-shield-result.txt", "SUCCESS: authored bracer settings and existing visuals preserved; bundle rebuilt and copied.");
            return;
        }

        try
        {
            Prepare();
            PrefabDefaultsAssetBuilder.PrepareAuthoredDefaults();
            HexBattleMageBundlePolicy.ValidateDependencies();
            Directory.CreateDirectory(BundleDirectory);

            var manifest = BuildPipeline.BuildAssetBundles(BundleDirectory, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

            if (manifest == null)
            {
                throw new InvalidOperationException("Magic bracer bundle build failed.");
            }

            VerifyBundle();
            File.Copy(BundleDirectory + "/" + BundleName, ModBundlePath, true);
            File.WriteAllText(Workspace + "/magic-shield-result.txt", "SUCCESS: bracer, field geometry, icon and production bundle verified and copied. Existing wand assets were not regenerated. In-game verification remains required.");
        }
        catch (Exception exception)
        {
            File.WriteAllText(Workspace + "/magic-shield-result.txt", exception.ToString());
            throw;
        }
    }

    public static void Prepare()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            PrefabDefaultsAssetBuilder.PrepareAuthoredDefaults();
            return;
        }

        var stage = PrefabStageUtility.GetCurrentPrefabStage();

        if (stage != null && stage.scene.isDirty)
        {
            throw new InvalidOperationException("Save the open prefab before preparing the magic bracer.");
        }

        Directory.CreateDirectory(AssetDirectory);
        AssetDatabase.Refresh();

        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/JVLmock/Shader/JVLmock_Custom_Piece.shader");

        if (shader == null)
        {
            throw new InvalidOperationException("The Jotunn Piece shader mock is missing.");
        }

        var leather = Material("WardLeather", shader, new Color(0.16f, 0.09f, 0.05f), Color.black);
        var bronze = Material("WardBronze", shader, new Color(0.55f, 0.32f, 0.09f), Color.black);
        var glow = Material("WardRune", shader, new Color(0.15f, 0.65f, 0.9f), new Color(0.3f, 1.5f, 2f));
        var prefab = new GameObject("HexMagicBracerVisual");
        var existingProfile = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)?.GetComponent<MagicBracerProfile>();
        var profile = prefab.AddComponent<MagicBracerProfile>();
        var existingDefinition = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)?.GetComponent<MageEquipmentDefinition>();
        var definition = prefab.AddComponent<MageEquipmentDefinition>();

        definition.Category = MageEquipmentCategory.Bracer;
        definition.Slot = MageEquipmentSlot.Bracer;
        definition.SupportsBlocking = true;

        if (existingDefinition != null)
        {
            EditorUtility.CopySerialized(existingDefinition, definition);
        }

        if (existingProfile != null)
        {
            EditorUtility.CopySerialized(existingProfile, profile);
        }

        try
        {
            var bracer = Child(prefab.transform, "BracerModel");

            AddMesh(bracer, "LeatherCuff", Cuff(-0.09f, 0.09f, 0.058f, 0.073f), leather);
            AddMesh(bracer, "WristBand", Cuff(-0.085f, -0.065f, 0.061f, 0.063f), bronze);
            AddMesh(bracer, "ForearmBand", Cuff(0.065f, 0.085f, 0.074f, 0.076f), bronze);

            var inlay = new Geometry();

            inlay.Line(new Vector3(0f, -0.045f, 0.071f), new Vector3(0f, 0.045f, 0.071f), 0.004f, RuneColor);
            inlay.Line(new Vector3(-0.028f, 0.045f, 0.071f), new Vector3(0f, 0.012f, 0.071f), 0.004f, RuneColor);
            inlay.Line(new Vector3(0.028f, 0.045f, 0.071f), new Vector3(0f, 0.012f, 0.071f), 0.004f, RuneColor);

            var runeMesh = inlay.Mesh();
            var runeVertices = runeMesh.vertices;

            for (int index = 0; index < runeVertices.Length; index++)
            {
                var vertex = runeVertices[index];
                float radius = Mathf.Lerp(0.058f, 0.073f, Mathf.InverseLerp(-0.09f, 0.09f, vertex.y));

                vertex.z = Mathf.Sqrt(radius * radius - vertex.x * vertex.x) + 0.0015f;
                runeVertices[index] = vertex;
            }

            runeMesh.vertices = runeVertices;
            runeMesh.RecalculateNormals();
            runeMesh.RecalculateBounds();
            AddMesh(bracer, "BracerRune", runeMesh, glow);
            bracer.Find("BracerRune").localRotation = Quaternion.Euler(0f, 180f, 0f);

            var field = Child(prefab.transform, "MagicField");
            var boundary = new Geometry();

            boundary.Ring(FieldRadius, 0.008f, RuneColor);
            boundary.Ring(0.425f, 0.003f, RuneColor);
            boundary.Ring(0.285f, 0.003f, new Color(0.2f, 0.65f, 1f, 0.45f));
            boundary.Ring(0.085f, 0.005f, RuneColor);

            for (int index = 0; index < 8; index++)
            {
                float angle = index * Mathf.PI / 4f;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

                boundary.Line(direction * 0.11f, direction * 0.265f, 0.003f, RuneColor);
            }

            AddMesh(field, "WardBoundary", boundary.Mesh(), glow);

            var veil = new Geometry();

            for (int index = 0; index < CircleSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / CircleSegments;
                float nextAngle = (index + 1) * Mathf.PI * 2f / CircleSegments;
                var first = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * FieldRadius;
                var second = new Vector3(Mathf.Cos(nextAngle), Mathf.Sin(nextAngle), 0f) * FieldRadius;

                veil.Triangle(Vector3.zero, first, second, new Color(0.1f, 0.5f, 1f, 0.045f));
            }

            AddMesh(field, "WardVeil", veil.Mesh(), glow);

            var runes = new Geometry();

            for (int index = 0; index < 12; index++)
            {
                var rotation = Quaternion.Euler(0f, 0f, index * 30f);
                var center = rotation * new Vector3(0f, 0.355f, -0.001f);
                var stemBottom = center + rotation * new Vector3(0f, -0.035f, 0f);
                var stemTop = center + rotation * new Vector3(0f, 0.035f, 0f);

                runes.Line(stemBottom, stemTop, 0.005f, RuneColor);

                if (index % 3 == 0)
                {
                    runes.Line(center, center + rotation * new Vector3(-0.023f, 0.022f, 0f), 0.005f, RuneColor);
                    runes.Line(center, center + rotation * new Vector3(0.023f, 0.022f, 0f), 0.005f, RuneColor);
                }
                else if (index % 3 == 1)
                {
                    var corner = center + rotation * new Vector3(0.025f, 0f, 0f);

                    runes.Line(stemTop, corner, 0.005f, RuneColor);
                    runes.Line(corner, stemBottom, 0.005f, RuneColor);
                }
                else
                {
                    runes.Line(center + rotation * new Vector3(-0.022f, 0.015f, 0f), center + rotation * new Vector3(0.022f, -0.015f, 0f), 0.005f, RuneColor);
                }
            }

            AddMesh(field, "RuneRing", runes.Mesh(), glow);
            SavePreview(prefab, false, Workspace + "/magic-bracer-preview.png", 768);
            SavePreview(prefab, false, AssetDirectory + "/MagicBracerIcon.png", 128);
            SavePreview(prefab, true, Workspace + "/magic-shield-preview.png", 768);
            field.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            AssetImporter.GetAtPath(PrefabPath).SetAssetBundleNameAndVariant(BundleName, string.Empty);
            AssetDatabase.ImportAsset(AssetDirectory + "/MagicBracerIcon.png");

            var iconImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDirectory + "/MagicBracerIcon.png");

            iconImporter.textureType = TextureImporterType.Sprite;
            iconImporter.spriteImportMode = SpriteImportMode.Single;
            iconImporter.alphaIsTransparency = true;
            iconImporter.mipmapEnabled = false;
            iconImporter.SetAssetBundleNameAndVariant(BundleName, string.Empty);
            iconImporter.SaveAndReimport();
            AssetDatabase.SaveAssets();
        }
        finally
        {
            Object.DestroyImmediate(prefab);
        }
    }

    private static Material Material(string name, Shader shader, Color color, Color emission)
    {
        string path = AssetDirectory + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_Color", color);
        material.SetColor("_EmissionColor", emission);
        material.SetTexture("_EmissionMap", Texture2D.whiteTexture);
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Transform Child(Transform parent, string name)
    {
        var child = new GameObject(name).transform;

        child.SetParent(parent, false);
        return child;
    }

    private static void AddMesh(Transform parent, string name, Mesh mesh, Material material)
    {
        string path = AssetDirectory + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        mesh.name = name;

        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            existing = mesh;
        }
        else
        {
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
        }

        var child = Child(parent, name);

        child.gameObject.AddComponent<MeshFilter>().sharedMesh = existing;
        child.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Mesh Cuff(float bottom, float top, float bottomRadius, float topRadius)
    {
        const float thickness = 0.005f;
        var geometry = new Geometry();

        for (int index = 0; index < CuffSegments; index++)
        {
            float angle = index * Mathf.PI * 2f / CuffSegments;
            float nextAngle = (index + 1) * Mathf.PI * 2f / CuffSegments;
            var first = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var second = new Vector3(Mathf.Cos(nextAngle), 0f, Mathf.Sin(nextAngle));
            var a = first * bottomRadius + Vector3.up * bottom;
            var b = second * bottomRadius + Vector3.up * bottom;
            var c = second * topRadius + Vector3.up * top;
            var d = first * topRadius + Vector3.up * top;
            var innerA = a - first * thickness;
            var innerB = b - second * thickness;
            var innerC = c - second * thickness;
            var innerD = d - first * thickness;

            geometry.Quad(a, d, c, b, Color.white);
            geometry.Quad(innerA, innerB, innerC, innerD, Color.white);
            geometry.Quad(a, b, innerB, innerA, Color.white);
            geometry.Quad(d, innerD, innerC, c, Color.white);
        }

        return geometry.Mesh();
    }

    private static void SavePreview(GameObject prefab, bool showField, string path, int size)
    {
        var preview = new PreviewRenderUtility();
        var fieldMaterial = new Material(Shader.Find("Sprites/Default"));

        try
        {
            preview.BeginPreview(new Rect(0f, 0f, size, size), GUIStyle.none);
            preview.camera.orthographic = true;
            preview.camera.orthographicSize = showField ? 0.57f : 0.145f;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 5f;
            preview.camera.transform.position = showField ? new Vector3(0f, 0f, -2f) : new Vector3(0.3f, 0.2f, -0.4f);
            preview.camera.transform.LookAt(Vector3.zero);
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f, 0f);
            preview.lights[0].intensity = 1.4f;
            preview.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);

            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                bool isField = filter.transform.parent.name == "MagicField";

                if (isField != showField)
                {
                    continue;
                }

                var material = isField ? fieldMaterial : filter.GetComponent<MeshRenderer>().sharedMaterial;

                preview.DrawMesh(filter.sharedMesh, filter.transform.localToWorldMatrix, material, 0);
            }

            preview.Render();

            var renderTexture = (RenderTexture)preview.EndPreview();
            var previousTarget = RenderTexture.active;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            try
            {
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousTarget;
                Object.DestroyImmediate(texture);
            }
        }
        finally
        {
            preview.Cleanup();
            Object.DestroyImmediate(fieldMaterial);
        }
    }

    private static void VerifyBundle()
    {
        var bundle = AssetBundle.LoadFromFile(Path.GetFullPath(BundleDirectory + "/" + BundleName));

        if (bundle == null)
        {
            throw new InvalidOperationException("Built bundle cannot be loaded.");
        }

        try
        {
            foreach (string name in new[] { "HexMagicBracerVisual", "HexBattleMage_Wand_Meadows_01", "HexArcaneWorkbench", "HexMagicBolt" })
            {
                var prefab = bundle.LoadAsset<GameObject>(name);

                if (prefab == null)
                {
                    throw new InvalidOperationException("Missing production prefab: " + name);
                }

                foreach (var component in prefab.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                    {
                        throw new InvalidOperationException("Missing component on " + name);
                    }
                }
            }

            var bracer = bundle.LoadAsset<GameObject>("HexMagicBracerVisual");

            if (bracer.transform.Find("MagicField").gameObject.activeSelf)
            {
                throw new InvalidOperationException("The magic field must be hidden by default.");
            }

            if (bracer.GetComponentsInChildren<Collider>(true).Length != 0)
            {
                throw new InvalidOperationException("The visual prefab must not contain combat colliders.");
            }

            if (bundle.LoadAsset<Sprite>("MagicBracerIcon") == null)
            {
                throw new InvalidOperationException("Missing magic bracer inventory icon.");
            }
        }
        finally
        {
            bundle.Unload(true);
        }
    }

    private sealed class Geometry
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<Color> _colors = new List<Color>();

        internal void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            int start = _vertices.Count;

            _vertices.AddRange(new[] { a, b, c });
            _colors.AddRange(new[] { color, color, color });
            _triangles.AddRange(new[] { start, start + 1, start + 2 });
        }

        internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            Triangle(a, b, c, color);
            Triangle(a, c, d, color);
        }

        internal void Line(Vector3 start, Vector3 end, float width, Color color)
        {
            var side = Vector3.Cross((end - start).normalized, Vector3.forward) * width * 0.5f;

            Quad(start - side, start + side, end + side, end - side, color);
        }

        internal void Ring(float radius, float width, Color color)
        {
            for (int index = 0; index < CircleSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / CircleSegments;
                float nextAngle = (index + 1) * Mathf.PI * 2f / CircleSegments;
                var first = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var second = new Vector3(Mathf.Cos(nextAngle), Mathf.Sin(nextAngle), 0f);

                Quad(first * (radius - width), first * radius, second * radius, second * (radius - width), color);
            }
        }

        internal Mesh Mesh()
        {
            var mesh = new Mesh();

            mesh.SetVertices(_vertices);
            mesh.SetTriangles(_triangles, 0);
            mesh.SetColors(_colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

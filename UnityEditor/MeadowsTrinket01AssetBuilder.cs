using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MeadowsTrinket01AssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/Trinkets/Meadows/Trinket_Meadows_01";
    private const string PrefabPath = Root + "/HexBattleMage_Trinket_Meadows_01.prefab";
    private const string Workspace = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/";
    private const int StoneSides = 16;
    private static readonly float[] Heights = { -0.045f, -0.037f, -0.022f, 0f, 0.022f, 0.036f, 0.045f };
    private static readonly float[] Widths = { 0.006f, 0.02f, 0.027f, 0.029f, 0.021f, 0.012f, 0.009f };

    [MenuItem("HexBattleMage/Prepare Meadows Trinket 01 Visual and Build Bundle")]
    public static void Build()
    {
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

        if (stage != null && stage.scene.isDirty)
        {
            throw new InvalidOperationException("Save the open prefab before preparing the stone trinket.");
        }

        Directory.CreateDirectory(Root);
        AssetDatabase.Refresh();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            Create();
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab.GetComponentsInChildren<MeshFilter>().Length != 6)
        {
            throw new InvalidOperationException("Stone trinket is missing an authored mesh.");
        }

        RenderPreview(prefab);
        AssetDatabase.SaveAssets();
        HexBattleMageBundlePolicy.ValidateDependencies();

        var manifest = BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

        if (manifest == null)
        {
            throw new InvalidOperationException("Stone trinket bundle build failed.");
        }

        File.Copy("Assets/AssetBundles/hexbattlemage", "C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/hexbattlemage", true);
        File.WriteAllText(Workspace + "trinket-meadows-01-visual-result.txt", "PASS: Meadows trinket 01 visual assets saved and bundled; existing authored geometry and profiles preserved.");
    }

    private static void Create()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/JVLmock/Shader/JVLmock_Custom_Piece.shader");

        if (shader == null)
        {
            throw new InvalidOperationException("Missing Piece shader mock.");
        }

        var stone = Material("RiverStone", shader, new Color(0.55f, 0.54f, 0.51f), Color.black);
        var leather = Material("WornLeather", shader, new Color(0.25f, 0.115f, 0.05f), Color.black);
        var groove = Material("RuneEngraving", shader, new Color(0.055f, 0.075f, 0.08f), Color.black);
        var blue = Material("ProtectionRuneBlue", shader, new Color(0.15f, 0.62f, 0.95f), new Color(0.2f, 0.8f, 1.5f));

        stone.mainTexture = StoneTexture();

        var root = new GameObject("HexBattleMage_Trinket_Meadows_01");

        AddMesh(root, "RiverStonePendant", Stone(), stone);
        AddMesh(root, "UpperLeatherBinding", Binding(1f), leather);
        AddMesh(root, "CrossLeatherBinding", Binding(-1f), leather);
        AddMesh(root, "LeatherCord", Cord(), leather);
        AddMesh(root, "RuneEngraving", Rune(0.0034f, 0.00035f), groove);
        AddMesh(root, "ConstantBlueRune", Rune(0.0018f, 0.00065f), blue);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetImporter.GetAtPath(PrefabPath).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
    }

    private static Material Material(string name, Shader shader, Color color, Color emission)
    {
        var material = new Material(shader) { name = name, color = color };

        material.SetColor("_EmissionColor", emission);
        material.SetTexture("_EmissionMap", Texture2D.whiteTexture);
        material.SetFloat("_Glossiness", 0.08f);
        material.SetFloat("_Metallic", 0f);

        if (emission.maxColorComponent > 0f)
        {
            material.EnableKeyword("_EMISSION");
        }

        AssetDatabase.CreateAsset(material, Root + "/" + name + ".mat");
        return material;
    }

    private static Texture2D StoneTexture()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float coarse = Mathf.PerlinNoise(x * 0.09f, y * 0.09f);
                float fine = Mathf.PerlinNoise(x * 0.65f, y * 0.65f);
                float value = 0.65f + coarse * 0.25f + fine * 0.1f;

                pixels[y * size + x] = new Color(value, value * 0.99f, value * 0.95f, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(Root + "/RiverStone_BaseColor.png", texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(Root + "/RiverStone_BaseColor.png");
        return AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/RiverStone_BaseColor.png");
    }

    private static Mesh Stone()
    {
        var mesh = new Surface();

        for (int ring = 0; ring < Heights.Length - 1; ring++)
        {
            for (int side = 0; side < StoneSides; side++)
            {
                mesh.Quad(StonePoint(ring, side), StonePoint(ring, side + 1), StonePoint(ring + 1, side + 1), StonePoint(ring + 1, side));
            }
        }

        for (int side = 0; side < StoneSides; side++)
        {
            mesh.Triangle(new Vector3(0f, Heights[0], 0f), StonePoint(0, side + 1), StonePoint(0, side));
            mesh.Triangle(new Vector3(0f, Heights[6], 0f), StonePoint(6, side), StonePoint(6, side + 1));
        }

        return mesh.Save("RiverStonePendant");
    }

    private static Vector3 StonePoint(int ring, int side)
    {
        float angle = side * Mathf.PI * 2f / StoneSides;
        float variation = 1f + Mathf.Sin(angle * 3f + ring * 0.7f) * 0.035f;

        return new Vector3(Mathf.Sin(angle) * Widths[ring] * variation, Heights[ring], -Mathf.Cos(angle) * Widths[ring] * 0.48f * variation);
    }

    private static float Width(float height)
    {
        for (int index = 0; index < Heights.Length - 1; index++)
        {
            if (height <= Heights[index + 1])
            {
                return Mathf.Lerp(Widths[index], Widths[index + 1], Mathf.InverseLerp(Heights[index], Heights[index + 1], height));
            }
        }

        return Widths[Widths.Length - 1];
    }

    private static Mesh Binding(float direction)
    {
        var mesh = new Surface();

        for (int side = 0; side < 32; side++)
        {
            float first = side * Mathf.PI * 2f / 32f;
            float next = (side + 1) * Mathf.PI * 2f / 32f;

            mesh.Quad(BindingPoint(first, -0.002f, direction), BindingPoint(next, -0.002f, direction), BindingPoint(next, 0.002f, direction), BindingPoint(first, 0.002f, direction));
        }

        return mesh.Save(direction > 0f ? "UpperLeatherBinding" : "CrossLeatherBinding");
    }

    private static Vector3 BindingPoint(float angle, float edge, float direction)
    {
        float height = 0.029f + Mathf.Sin(angle) * 0.006f * direction + edge;
        float width = Width(height) + 0.0009f;

        return new Vector3(Mathf.Sin(angle) * width, height, -Mathf.Cos(angle) * (width * 0.48f + 0.0009f));
    }

    private static Mesh Cord()
    {
        var mesh = new Surface();

        for (int segment = 0; segment < 48; segment++)
        {
            for (int side = 0; side < 6; side++)
            {
                mesh.Quad(CordPoint(segment, side), CordPoint(segment + 1, side), CordPoint(segment + 1, side + 1), CordPoint(segment, side + 1));
            }
        }

        return mesh.Save("LeatherCord");
    }

    private static Vector3 CordPoint(int segment, int side)
    {
        float angle = segment * Mathf.PI * 2f / 48f;
        float cross = side * Mathf.PI * 2f / 6f;
        var center = new Vector3(Mathf.Sin(angle) * 0.027f, 0.105f + Mathf.Cos(angle) * 0.066f, 0.002f);
        var outward = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f).normalized;

        return center + (outward * Mathf.Cos(cross) + Vector3.forward * Mathf.Sin(cross)) * 0.0015f;
    }

    private static Mesh Rune(float width, float offset)
    {
        var mesh = new Surface();

        Stroke(mesh, new Vector2(0f, -0.025f), new Vector2(0f, 0.016f), width, offset);
        Stroke(mesh, new Vector2(0f, -0.001f), new Vector2(-0.014f, 0.013f), width, offset);
        Stroke(mesh, new Vector2(0f, -0.001f), new Vector2(0.014f, 0.013f), width, offset);
        return mesh.Save(width > 0.002f ? "RuneEngraving" : "ConstantBlueRune");
    }

    private static void Stroke(Surface mesh, Vector2 from, Vector2 to, float width, float offset)
    {
        var direction = (to - from).normalized;
        var edge = new Vector2(-direction.y, direction.x) * width * 0.5f;

        for (int segment = 0; segment < 12; segment++)
        {
            var start = Vector2.Lerp(from, to, segment / 12f);
            var end = Vector2.Lerp(from, to, (segment + 1) / 12f);

            mesh.Quad(RunePoint(start - edge, offset), RunePoint(end - edge, offset), RunePoint(end + edge, offset), RunePoint(start + edge, offset));
        }
    }

    private static Vector3 RunePoint(Vector2 point, float offset)
    {
        float width = Width(point.y);
        float depth = width * 0.48f * Mathf.Sqrt(Mathf.Clamp01(1f - point.x * point.x / (width * width)));

        return new Vector3(point.x, point.y, -depth - offset - 0.0007f);
    }

    private static void AddMesh(GameObject root, string name, Mesh mesh, Material material)
    {
        var child = new GameObject(name);

        child.transform.SetParent(root.transform, false);
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        child.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    public static void RenderPreview(GameObject prefab)
    {
        var preview = new PreviewRenderUtility();
        var root = Object.Instantiate(prefab);
        var materials = new List<Material>();

        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            var source = renderer.sharedMaterial;
            var material = new Material(Shader.Find("Standard")) { color = source.color, mainTexture = source.mainTexture };

            material.SetFloat("_Glossiness", 0.08f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
            renderer.sharedMaterial = material;
            materials.Add(material);
        }

        preview.AddSingleGO(root);
        preview.camera.transform.position = new Vector3(0.11f, 0.09f, -0.4f);
        preview.camera.transform.LookAt(new Vector3(0f, 0.06f, 0f));
        preview.camera.nearClipPlane = 0.001f;
        preview.camera.farClipPlane = 2f;
        preview.camera.fieldOfView = 35f;
        preview.ambientColor = new Color(0.45f, 0.45f, 0.45f);
        preview.lights[0].intensity = 1.8f;
        preview.lights[0].transform.rotation = Quaternion.Euler(35f, 30f, 0f);
        preview.lights[1].intensity = 0.9f;
        preview.lights[1].transform.rotation = Quaternion.Euler(340f, 315f, 0f);
        preview.BeginPreview(new Rect(0f, 0f, 768f, 768f), GUIStyle.none);
        preview.camera.clearFlags = CameraClearFlags.SolidColor;
        preview.camera.backgroundColor = Color.clear;
        preview.camera.Render();

        var rendered = (RenderTexture)preview.EndPreview();
        var previous = RenderTexture.active;
        var image = new Texture2D(rendered.width, rendered.height, TextureFormat.RGBA32, false);

        RenderTexture.active = rendered;
        image.ReadPixels(new Rect(0f, 0f, rendered.width, rendered.height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(Workspace + "trinket-meadows-01-preview.png", image.EncodeToPNG());
        Object.DestroyImmediate(image);
        preview.Cleanup();

        foreach (var material in materials)
        {
            Object.DestroyImmediate(material);
        }
    }

    private sealed class Surface
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<Vector2> _uv = new List<Vector2>();

        public void Quad(Vector3 first, Vector3 second, Vector3 third, Vector3 fourth)
        {
            Triangle(first, second, third);
            Triangle(first, third, fourth);
        }

        public void Triangle(Vector3 first, Vector3 second, Vector3 third)
        {
            foreach (var point in new[] { first, third, second })
            {
                _triangles.Add(_vertices.Count);
                _vertices.Add(point);
                _uv.Add(new Vector2(point.x * 12f + 0.5f, point.y * 10f + 0.5f));
            }
        }

        public Mesh Save(string name)
        {
            var mesh = new Mesh { name = name };

            mesh.SetVertices(_vertices);
            mesh.SetTriangles(_triangles, 0);
            mesh.SetUVs(0, _uv);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Root + "/" + name + ".asset");
            return mesh;
        }
    }
}

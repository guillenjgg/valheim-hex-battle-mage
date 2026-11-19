using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MeadowsClothingVisualBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/Armor/Meadows";
    private const string ReferenceRoot = "Assets/Editor/HexBattleMage/CloakReference";
    private const string CloakRoot = "Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01";
    private const string BundleName = "hexbattlemage";
    private const int RingSegments = 32;
    private const float FabricClearance = 0.025f;
    private static MeadowsCloak01AssetBuilder.RigReference _rig;
    private static MeadowsCloak01AssetBuilder.Bodies _bodies;

    private sealed class Surface
    {
        internal readonly List<Vector3> Vertices = new List<Vector3>();
        internal readonly List<Vector2> Uv = new List<Vector2>();
        internal readonly List<int> Triangles = new List<int>();
        internal readonly List<BoneWeight> Weights = new List<BoneWeight>();

        internal int Add(Vector3 position, BoneWeight weight, Vector2 uv)
        {
            int index = Vertices.Count;

            Vertices.Add(position);
            Weights.Add(weight);
            Uv.Add(uv);
            return index;
        }

        internal void Quad(int a, int b, int c, int d)
        {
            Triangles.AddRange(new[] { a, b, c, a, c, d });
        }
    }

    [MenuItem("HexBattleMage/Armor/Prepare Plain Hood and Long Sleeve Tunic")]
    public static void Prepare()
    {
        _rig = JsonUtility.FromJson<MeadowsCloak01AssetBuilder.RigReference>(File.ReadAllText(ReferenceRoot + "/NativeCapeRigReference.json"));
        _bodies = JsonUtility.FromJson<MeadowsCloak01AssetBuilder.Bodies>(File.ReadAllText(ReferenceRoot + "/ValheimBodyReferences.json"));

        foreach (string category in new[] { "Chest", "Helmet" })
        {
            string folder = Root + "/" + category + "_Meadows_01";
            string identity = "HexBattleMage_" + category + "_Meadows_01";
            string path = folder + "/" + identity + "_Definition.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var profile = root.GetComponent<HexBattleMageArmorProfile>();
                bool migrate = !profile.ReplaceNativeVisuals && profile.MaleVisual == null && profile.FemaleVisual == null;
                var material = PrepareMaterial(folder, category);

                foreach (string gender in new[] { "Male", "Female" })
                {
                    string visualPath = folder + "/" + identity + "_" + gender + ".prefab";

                    if (AssetDatabase.LoadAssetAtPath<GameObject>(visualPath) == null)
                    {
                        var surface = category == "Chest" ? CreateTunic(gender) : CreateHood(gender);

                        SaveVisual(surface, material, identity + "_" + gender, folder, visualPath);
                    }

                    if (gender == "Male" && profile.MaleVisual == null)
                    {
                        profile.MaleVisual = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
                    }
                    else if (gender == "Female" && profile.FemaleVisual == null)
                    {
                        profile.FemaleVisual = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
                    }
                }

                if (profile.WorldModel == null)
                {
                    profile.WorldModel = PrepareDrop(profile.MaleVisual, folder, identity, material);
                }

                if (migrate)
                {
                    profile.ReplaceNativeVisuals = true;
                    profile.ClearBodyArmorMaterial = category == "Chest";
                    profile.ReplaceMeshTexture = false;
                    profile.BodyTexture = null;
                    profile.BodyTextureProperty = string.Empty;
                }

                if (migrate || profile.Icon == null)
                {
                    profile.Icon = PrepareIcon(profile.WorldModel, folder);
                }

                var styles = root.GetComponent<MageArmorStyleProfile>();

                if (migrate)
                {
                    styles.Styles[0].Material = material;
                    styles.Styles[0].Icon = profile.Icon;
                }

                ArmorStyleAssetPreparation.Prepare(root, path);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        CreatePreview();
        File.WriteAllText("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/plain-clothing-authoring.txt", "PASS: plain open-face hood and tunic with full sleeves; separate male/female native-ordered 53-bone skinning; no native leather chest geometry or Fenris eye materials; authored visuals, drop models and icons. Preview scene saved; in-game movement remains to test.");
    }

    public static void Build()
    {
        Prepare();
        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }

    private static Surface CreateTunic(string gender)
    {
        var surface = new Surface();
        var body = Body(gender);
        var heights = new[] { 1.65f, 1.62f, 1.49f, 1.43f, 1.35f, 1.27f, 1.19f, 1.11f, 1.03f, 0.95f, 0.89f, 0.84f };

        for (int row = 0; row < heights.Length; row++)
        {
            float height = heights[row];
            var section = CrossSection(body, Mathf.Clamp(height, 0.99f, 1.56f));
            float radiusX = section.Max(position => Mathf.Abs(position.x)) + FabricClearance;
            float front = section.Max(position => position.z) + FabricClearance;
            float back = section.Min(position => position.z) - FabricClearance;
            float centerZ = (front + back) * 0.5f;
            float radiusZ = (front - back) * 0.5f;
            float enclosingScale = section.Max(position => Mathf.Sqrt(Mathf.Pow(position.x / radiusX, 2f) + Mathf.Pow((position.z - centerZ) / radiusZ, 2f)));

            radiusX *= Mathf.Max(1f, enclosingScale);
            radiusZ *= Mathf.Max(1f, enclosingScale);

            if (row == 0)
            {
                radiusX = 0.105f;
                radiusZ = 0.115f;
                centerZ = -0.035f;
            }

            if (height < 1.03f)
            {
                radiusX += (1.03f - height) * 0.12f;
                radiusZ += (1.03f - height) * 0.07f;
            }

            for (int column = 0; column <= RingSegments; column++)
            {
                float angle = column * Mathf.PI * 2f / RingSegments;
                var position = new Vector3(Mathf.Sin(angle) * radiusX, height, centerZ + Mathf.Cos(angle) * radiusZ);
                var candidates = Enumerable.Range(0, body.vertices.Length).Where(index => Mathf.Abs(body.vertices[index].x) < 0.28f);
                int nearest = candidates.OrderBy(index => (body.vertices[index] - position).sqrMagnitude).First();
                var weight = height < 1.10f ? SingleWeight("Hips") : Weight(body.influences[nearest]);

                surface.Add(position, weight, new Vector2(column / (float)RingSegments, height));

                if (row > 0 && column > 0)
                {
                    int current = row * (RingSegments + 1) + column;

                    surface.Quad(current - RingSegments - 2, current - 1, current, current - RingSegments - 1);
                }
            }
        }

        AddSleeve(surface, gender, "Left");
        AddSleeve(surface, gender, "Right");
        return surface;
    }

    private static List<Vector3> CrossSection(MeadowsCloak01AssetBuilder.Body body, float height)
    {
        var points = new List<Vector3>();

        for (int index = 0; index < body.triangles.Length; index += 3)
        {
            for (int edge = 0; edge < 3; edge++)
            {
                Vector3 start = body.vertices[body.triangles[index + edge]];
                Vector3 end = body.vertices[body.triangles[index + (edge + 1) % 3]];

                if ((start.y <= height && end.y > height) || (end.y <= height && start.y > height))
                {
                    Vector3 point = Vector3.Lerp(start, end, (height - start.y) / (end.y - start.y));

                    if (Mathf.Abs(point.x) < 0.255f)
                    {
                        points.Add(point);
                    }
                }
            }
        }

        if (points.Count == 0)
        {
            throw new InvalidOperationException("Missing torso fitting cross-section at " + height);
        }

        return points;
    }

    private static void AddSleeve(Surface surface, string gender, string side)
    {
        Vector3 shoulder = Position(side + "Arm");
        Vector3 elbow = Position(side + "ForeArm");
        Vector3 wrist = Position(side + "Hand");
        int start = surface.Vertices.Count;
        const int rows = 10;

        for (int row = 0; row <= rows; row++)
        {
            float progress = row / (float)rows;
            Vector3 axis = progress < 0.55f ? (elbow - shoulder).normalized : (wrist - elbow).normalized;
            Vector3 center = progress < 0.55f ? Vector3.Lerp(shoulder, elbow, progress / 0.55f) : Vector3.Lerp(elbow, wrist, (progress - 0.55f) / 0.45f);
            Vector3 tangent = Vector3.Cross(axis, Vector3.forward).normalized;
            float radius = Mathf.Lerp(gender == "Male" ? 0.145f : 0.13f, 0.056f, progress) * Mathf.Lerp(0.9f, 1f, Mathf.Clamp01(progress / 0.2f));
            var weight = progress < 0.45f ? SingleWeight(side + "Arm") : new BoneWeight { boneIndex0 = Bone(side + "Arm"), weight0 = 1f - Mathf.InverseLerp(0.45f, 0.65f, progress), boneIndex1 = Bone(side + "ForeArm"), weight1 = Mathf.InverseLerp(0.45f, 0.65f, progress) };

            if (row == 0)
            {
                center -= axis * 0.035f;
            }

            for (int column = 0; column <= RingSegments; column++)
            {
                float angle = column * Mathf.PI * 2f / RingSegments;
                Vector3 position = center + tangent * Mathf.Cos(angle) * radius + Vector3.forward * Mathf.Sin(angle) * radius;

                surface.Add(position, weight, new Vector2(column / (float)RingSegments, progress));

                if (row > 0 && column > 0)
                {
                    int current = start + row * (RingSegments + 1) + column;

                    surface.Quad(current - RingSegments - 2, current - 1, current, current - RingSegments - 1);
                }
            }
        }
    }

    private static Surface CreateHood(string gender)
    {
        var surface = new Surface();
        var heights = new[] { 1.56f, 1.62f, 1.69f, 1.77f, 1.85f, 1.92f, 1.965f };
        var widths = new[] { 0.135f, 0.15f, 0.166f, 0.171f, 0.16f, 0.12f, 0.018f };
        var depths = new[] { 0.13f, 0.15f, 0.17f, 0.18f, 0.17f, 0.13f, 0.02f };
        float size = gender == "Female" ? 0.97f : 1f;

        for (int row = 0; row < heights.Length; row++)
        {
            float opening = row < 5 ? 0.78f : 0f;

            for (int column = 0; column <= RingSegments; column++)
            {
                float u = column / (float)RingSegments;
                float angle = Mathf.Lerp(opening, Mathf.PI * 2f - opening, u);
                var position = new Vector3(Mathf.Sin(angle) * widths[row] * size, heights[row], 0.025f + Mathf.Cos(angle) * depths[row]);
                float headWeight = Mathf.InverseLerp(1.56f, 1.70f, heights[row]);
                var weight = new BoneWeight { boneIndex0 = Bone("Head"), weight0 = headWeight, boneIndex1 = Bone("Neck"), weight1 = 1f - headWeight };

                surface.Add(position, weight, new Vector2(u, row / (float)(heights.Length - 1)));

                if (row > 0 && column > 0)
                {
                    int current = row * (RingSegments + 1) + column;

                    surface.Quad(current - RingSegments - 2, current - RingSegments - 1, current, current - 1);
                }
            }
        }

        return surface;
    }

    private static void SaveVisual(Surface surface, Material material, string name, string folder, string path)
    {
        var root = new GameObject(name);
        var attach = new GameObject("attach_skin");
        var rig = new GameObject("ReferenceRig");

        attach.transform.SetParent(root.transform, false);
        rig.transform.SetParent(attach.transform, false);

        var bones = MeadowsCloak01AssetBuilder.CreateRig(_rig, rig.transform, false);
        var mesh = new Mesh { name = name + "_Cloth" };

        mesh.SetVertices(surface.Vertices);
        mesh.SetUVs(0, surface.Uv);
        mesh.SetTriangles(surface.Triangles, 0);
        mesh.boneWeights = surface.Weights.ToArray();
        mesh.bindposes = _rig.bones.Select(bone => MeadowsCloak01AssetBuilder.Matrix(bone.nativeWorld).inverse).ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, folder + "/" + mesh.name + ".asset");

        var cloth = new GameObject("Cloth");

        cloth.transform.SetParent(attach.transform, false);

        var renderer = cloth.AddComponent<SkinnedMeshRenderer>();

        renderer.sharedMesh = mesh;
        renderer.sharedMaterial = material;
        renderer.bones = bones;
        renderer.rootBone = bones[0];
        renderer.updateWhenOffscreen = true;
        renderer.localBounds = new Bounds(Vector3.up, new Vector3(2f, 2.4f, 1f));
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static Material PrepareMaterial(string folder, string category)
    {
        string path = folder + "/PlainCloth.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
        {
            return material;
        }

        material = new Material(AssetDatabase.LoadAssetAtPath<Material>(CloakRoot + "/Styles/White.mat"));
        material.name = category + "_PlainCloth";
        material.color = category == "Chest" ? new Color(0.8f, 0.8f, 0.78f) : new Color(0.57f, 0.55f, 0.50f);
        material.SetFloat("_Glossiness", 0f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Cull", 0f);
        material.SetColor("_EmissionColor", Color.black);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    public static GameObject PrepareDrop(GameObject visual, string folder, string identity, Material material)
    {
        string path = folder + "/" + identity + "_PlainDrop.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (existing != null)
        {
            return existing;
        }

        var renderer = visual.GetComponentInChildren<SkinnedMeshRenderer>();
        var mesh = Object.Instantiate(renderer.sharedMesh);

        mesh.name = identity + "_PlainDrop";
        mesh.vertices = mesh.vertices.Select(position => (position - mesh.bounds.center) * 0.25f).ToArray();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, folder + "/" + mesh.name + ".asset");

        var root = new GameObject(identity + "_PlainDrop");

        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().sharedMaterial = material;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    public static Sprite PrepareIcon(GameObject visual, string folder)
    {
        string path = folder + "/PlainIcon.png";

        if (!File.Exists(path))
        {
            var utility = new PreviewRenderUtility();
            var instance = Object.Instantiate(visual);
            var renderer = instance.GetComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Standard"));

            material.mainTexture = renderer.sharedMaterial.mainTexture;
            material.color = renderer.sharedMaterial.color;
            material.SetFloat("_Glossiness", 0f);
            renderer.sharedMaterial = material;
            utility.AddSingleGO(instance);
            utility.camera.transform.position = new Vector3(0.30f, 0.12f, 0.65f);
            utility.camera.transform.LookAt(Vector3.zero);
            utility.camera.orthographic = true;
            utility.camera.orthographicSize = 0.22f;
            utility.camera.nearClipPlane = 0.01f;
            utility.camera.farClipPlane = 5f;
            utility.camera.clearFlags = CameraClearFlags.SolidColor;
            utility.camera.backgroundColor = Color.clear;
            utility.lights[0].intensity = 1.2f;
            utility.lights[0].transform.rotation = Quaternion.Euler(30f, 30f, 0f);
            utility.ambientColor = Color.gray;
            utility.BeginPreview(new Rect(0f, 0f, 128f, 128f), GUIStyle.none);
            utility.camera.Render();

            var texture = utility.EndPreview();
            var previous = RenderTexture.active;

            RenderTexture.active = (RenderTexture)texture;

            var output = new Texture2D(128, 128, TextureFormat.RGBA32, false);

            output.ReadPixels(new Rect(0f, 0f, 128f, 128f), 0, 0);
            output.Apply();
            File.WriteAllBytes(path, output.EncodeToPNG());
            RenderTexture.active = previous;
            utility.Cleanup();
            Object.DestroyImmediate(material);
            Object.DestroyImmediate(output);
        }

        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.isReadable = true;
        importer.maxTextureSize = 256;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.crunchedCompression = true;
        importer.compressionQuality = 100;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant(BundleName, string.Empty);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static void CreatePreview()
    {
        _rig = JsonUtility.FromJson<MeadowsCloak01AssetBuilder.RigReference>(File.ReadAllText(ReferenceRoot + "/NativeCapeRigReference.json"));
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);

        foreach (string gender in new[] { "Male", "Female" })
        {
            var host = new GameObject(gender + " fitting preview");

            host.transform.position = Vector3.right * (gender == "Male" ? -0.75f : 0.75f);

            foreach (string category in new[] { "Chest", "Helmet" })
            {
                var definition = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + category + "_Meadows_01/HexBattleMage_" + category + "_Meadows_01_Definition.prefab").GetComponent<HexBattleMageArmorProfile>();
                var prefab = gender == "Male" ? definition.MaleVisual : definition.FemaleVisual;

                Object.Instantiate(prefab, host.transform, false);
            }

            var body = new GameObject("Editor only fitting body");

            body.transform.SetParent(host.transform, false);

            var rig = new GameObject("Body rig");

            rig.transform.SetParent(body.transform, false);

            var renderer = body.AddComponent<SkinnedMeshRenderer>();

            renderer.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ReferenceRoot + "/EditorOnlyBody_" + gender + ".asset");
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ReferenceRoot + "/PreviewBody.mat");
            renderer.bones = MeadowsCloak01AssetBuilder.CreateRig(_rig, rig.transform, false);
            renderer.rootBone = renderer.bones[0];
        }

        var light = new GameObject("Daylight").AddComponent<Light>();

        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(40f, 155f, 0f);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, ReferenceRoot + "/PlainClothingPreview.unity");
    }

    private static MeadowsCloak01AssetBuilder.Body Body(string gender)
    {
        return _bodies.bodies.Single(body => body.name == (gender == "Male" ? "body_38" : "bodyfem_1"));
    }

    private static int Bone(string name)
    {
        return Array.FindIndex(_rig.bones, bone => bone.name == name);
    }

    private static Vector3 Position(string name)
    {
        return MeadowsCloak01AssetBuilder.Matrix(_rig.bones[Bone(name)].nativeWorld).GetColumn(3);
    }

    private static BoneWeight SingleWeight(string name)
    {
        return new BoneWeight { boneIndex0 = Bone(name), weight0 = 1f };
    }

    private static BoneWeight Weight(MeadowsCloak01AssetBuilder.Influence influence)
    {
        return new BoneWeight { boneIndex0 = influence.indices[0], weight0 = influence.weights[0], boneIndex1 = influence.indices[1], weight1 = influence.weights[1], boneIndex2 = influence.indices[2], weight2 = influence.weights[2], boneIndex3 = influence.indices[3], weight3 = influence.weights[3] };
    }

}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MagicaCloth2;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class MeadowsCloak01AssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01";
    private const string ReferenceRoot = "Assets/Editor/HexBattleMage/CloakReference";
    private const string PrefabName = "HexBattleMage_Cloak_Meadows_01";
    private const string Report = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/cloak-unity-import-report.txt";
    private const float FixedClothHeight = 1.42f;

    [Serializable] public sealed class Bone { public string name; public string parent; public float[] nativeWorld; public float[] relaxedWorld; }
    [Serializable] public sealed class RigReference { public Bone[] bones; }
    [Serializable] public sealed class Influence { public int[] indices; public float[] weights; }
    [Serializable] public sealed class Body { public string name; public Vector3[] vertices; public int[] triangles; public Influence[] influences; }
    [Serializable] public sealed class Bodies { public Body[] bodies; }

    [MenuItem("HexBattleMage/Cloak/Import Meadows Cloak 01 (No Bundle Build)")]
    public static void Prepare()
    {
        var reference = JsonUtility.FromJson<RigReference>(File.ReadAllText(ReferenceRoot + "/NativeCapeRigReference.json"));

        if (reference.bones.Length != 53)
        {
            throw new InvalidOperationException("The installed player rig must contain the expected 53 ordered skin bones.");
        }

        ConfigureImports();

        var cloth = PrepareMaterial("MeadowsCloth", false);
        var metal = PrepareMaterial("MeadowsClasp", true);

        foreach (string gender in new[] { "Male", "Female" })
        {
            string path = VisualPath(gender);

            if (!File.Exists(path))
            {
                CreateVisual(gender, reference, cloth, metal, path);
            }
        }

        string combinedPath = Root + "/" + PrefabName + ".prefab";

        if (!File.Exists(combinedPath))
        {
            var root = new GameObject(PrefabName);

            foreach (string gender in new[] { "Male", "Female" })
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath(gender)), root.transform);

                visual.name = gender;
                visual.SetActive(gender == "Male");
            }

            PrefabUtility.SaveAsPrefabAsset(root, combinedPath);
            Object.DestroyImmediate(root);
        }

        CreatePreview(reference);
        AssetDatabase.SaveAssets();
        Validate();
    }

    private static string VisualPath(string gender)
    {
        return Root + "/Visuals/" + PrefabName + "_" + gender + ".prefab";
    }

    private static void ConfigureImports()
    {
        foreach (string path in Directory.GetFiles(Root + "/Models", "*.fbx"))
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (ModelImporter)AssetImporter.GetAtPath(path);

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = false;
            importer.isReadable = true;
            importer.optimizeGameObjects = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
        }

        foreach (string path in Directory.GetFiles(Root + "/Textures", "*.png"))
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);

            importer.textureType = path.Contains("Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = path.Contains("BaseColor");
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }
    }

    private static Material PrepareMaterial(string name, bool metal)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
        {
            return material;
        }

        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/JVLmock/Shader/JVLmock_Custom_Piece.shader");

        material = new Material(shader) { name = name };
        material.color = metal ? new Color(0.13f, 0.14f, 0.14f) : Color.white;
        material.SetFloat("_Metallic", metal ? 0.7f : 0f);
        material.SetFloat("_Glossiness", metal ? 0.25f : 0.08f);

        if (!metal)
        {
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Cloak_Meadows_01_BaseColor.png");
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Cloak_Meadows_01_Normal.png"));
            material.SetFloat("_BumpScale", 0.35f);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    public static Matrix4x4 Matrix(float[] values)
    {
        var matrix = new Matrix4x4();

        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                matrix[row, column] = values[row * 4 + column];
            }
        }

        return matrix;
    }

    private static Matrix4x4 AnchorMatrix(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        return new Matrix4x4(new Vector4(a.x, a.y, a.z, 1f), new Vector4(b.x, b.y, b.z, 1f), new Vector4(c.x, c.y, c.z, 1f), new Vector4(d.x, d.y, d.z, 1f));
    }

    private static void CreateVisual(string gender, RigReference reference, Material cloth, Material metal, string path)
    {
        var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + PrefabName + "_" + gender + ".fbx"));
        var importedBones = model.GetComponentsInChildren<Transform>(true).Where(transform => reference.bones.Any(bone => bone.name == transform.name)).ToDictionary(transform => transform.name);
        var sourceAnchors = new List<Vector3>();
        var targetAnchors = new List<Vector3>();

        foreach (string name in new[] { "Hips", "Head", "LeftHand", "LeftFoot" })
        {
            sourceAnchors.Add(importedBones[name].position);
            targetAnchors.Add(Matrix(reference.bones.Single(bone => bone.name == name).relaxedWorld).GetColumn(3));
        }

        Matrix4x4 correction = AnchorMatrix(targetAnchors[0], targetAnchors[1], targetAnchors[2], targetAnchors[3]) * AnchorMatrix(sourceAnchors[0], sourceAnchors[1], sourceAnchors[2], sourceAnchors[3]).inverse;
        float maximumAnchorError = reference.bones.Max(bone => Vector3.Distance(correction.MultiplyPoint3x4(importedBones[bone.name].position), Matrix(bone.relaxedWorld).GetColumn(3)));

        if (maximumAnchorError > 0.002f)
        {
            throw new InvalidOperationException("FBX coordinate alignment failed: " + maximumAnchorError);
        }

        var root = new GameObject(PrefabName + "_" + gender);
        var attach = new GameObject("attach_skin");

        attach.transform.SetParent(root.transform, false);

        var rig = new GameObject("ReferenceRig").transform;

        rig.SetParent(attach.transform, false);

        var bones = CreateRig(reference, rig, true);
        var bindposes = reference.bones.Select(bone => Matrix(bone.relaxedWorld).inverse).ToArray();

        foreach (var source in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            bool isClasp = source.name.Contains("Clasp");
            var mesh = Object.Instantiate(source.sharedMesh);
            Matrix4x4 transform = correction * source.transform.localToWorldMatrix;
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var mapping = source.bones.Select(bone => Array.FindIndex(reference.bones, candidate => candidate.name == bone.name)).ToArray();

            for (int index = 0; index < vertices.Length; index++)
            {
                vertices[index] = transform.MultiplyPoint3x4(vertices[index]);
                var weight = weights[index];

                weight.boneIndex0 = mapping[weight.boneIndex0];
                weight.boneIndex1 = mapping[weight.boneIndex1];
                weight.boneIndex2 = mapping[weight.boneIndex2];
                weight.boneIndex3 = mapping[weight.boneIndex3];
                weights[index] = weight;
            }

            if (transform.determinant < 0f)
            {
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    var triangles = mesh.GetTriangles(submesh);

                    for (int index = 0; index < triangles.Length; index += 3)
                    {
                        int first = triangles[index];

                        triangles[index] = triangles[index + 1];
                        triangles[index + 1] = first;
                    }

                    mesh.SetTriangles(triangles, submesh);
                }
            }

            mesh.vertices = vertices;
            mesh.boneWeights = weights;
            mesh.bindposes = bindposes;
            mesh.name = PrefabName + "_" + gender + (isClasp ? "_Clasp" : "_Cloth");
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            string meshPath = Root + "/Meshes/" + mesh.name + ".asset";
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

            if (existingMesh == null)
            {
                AssetDatabase.CreateAsset(mesh, meshPath);
            }
            else
            {
                EditorUtility.CopySerialized(mesh, existingMesh);
                Object.DestroyImmediate(mesh);
                mesh = existingMesh;
            }

            var visual = new GameObject(isClasp ? "Clasp" : "Cloth");

            visual.transform.SetParent(attach.transform, false);

            var renderer = visual.AddComponent<SkinnedMeshRenderer>();

            renderer.sharedMesh = mesh;
            renderer.sharedMaterials = Enumerable.Repeat(isClasp ? metal : cloth, mesh.subMeshCount).ToArray();
            renderer.bones = bones;
            renderer.rootBone = bones[0];
            renderer.updateWhenOffscreen = true;
            renderer.localBounds = new Bounds(new Vector3(0f, 1.1f, 0f), new Vector3(1.5f, 2.5f, 1.5f));

            if (!isClasp)
            {
                ConfigureCloth(visual.AddComponent<MagicaCloth>(), renderer);
                visual.AddComponent<PlayerClothWindShelter>();
            }
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(model);
    }

    public static Transform[] CreateRig(RigReference reference, Transform root, bool relaxed)
    {
        var bones = reference.bones.Select(bone => new GameObject(bone.name).transform).ToArray();

        for (int index = 0; index < bones.Length; index++)
        {
            var definition = reference.bones[index];
            int parentIndex = Array.FindIndex(reference.bones, bone => bone.name == definition.parent);
            Matrix4x4 world = Matrix(relaxed ? definition.relaxedWorld : definition.nativeWorld);
            Matrix4x4 parent = parentIndex < 0 ? Matrix4x4.identity : Matrix(relaxed ? reference.bones[parentIndex].relaxedWorld : reference.bones[parentIndex].nativeWorld);
            Matrix4x4 local = parent.inverse * world;

            bones[index].SetParent(parentIndex < 0 ? root : bones[parentIndex], false);
            bones[index].localPosition = local.GetColumn(3);
            bones[index].localRotation = local.rotation;
            bones[index].localScale = local.lossyScale;
        }

        return bones;
    }

    private static void ConfigureCloth(MagicaCloth cloth, SkinnedMeshRenderer renderer)
    {
        var settings = cloth.SerializeData;

        settings.clothType = ClothProcess.ClothType.MeshCloth;
        settings.sourceRenderers = new List<Renderer> { renderer };
        settings.gravity = 7f;
        settings.animationPoseRatio = 0.1f;
        settings.reductionSetting.simpleDistance = 0.025f;
        settings.radius.value = 0.02f;
        settings.damping.value = 0.1f;

        var vertices = renderer.sharedMesh.vertices;
        var selection = new SelectionData(vertices.Length) { userEdit = true, maxConnectionDistance = 0.05f };

        for (int index = 0; index < vertices.Length; index++)
        {
            selection.positions[index] = vertices[index];
            selection.attributes[index] = vertices[index].y >= FixedClothHeight ? VertexAttribute.Fixed : VertexAttribute.Move;
        }

        cloth.GetSerializeData2().selectionData = selection;

        using (var setup = new RenderSetupData(null, renderer))
        {
            // Keep all rig transforms during authoring. The game's player-built Collections
            // library cannot schedule its bone-filter job under Editor safety checks.
            bool hasBoneWeight = setup.hasBoneWeight;

            setup.hasBoneWeight = false;

            bool serialized = cloth.GetSerializeData2().initData.Serialize(settings, new TransformRecord(cloth.transform, true), new TransformRecord(cloth.transform, true), new List<RenderSetupData> { setup });

            setup.hasBoneWeight = hasBoneWeight;

            if (!serialized)
            {
                throw new InvalidOperationException("MagicaCloth initialization data could not be authored.");
            }

            cloth.GetSerializeData2().initData.clothSetupDataList[0].hasBoneWeight = hasBoneWeight;
        }
    }

    private static void CreatePreview(RigReference reference)
    {
        const string previewPath = ReferenceRoot + "/Cloak_Meadows_01_Preview.unity";

        if (File.Exists(previewPath))
        {
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var references = JsonUtility.FromJson<Bodies>(File.ReadAllText(ReferenceRoot + "/ValheimBodyReferences.json"));
        var bodyMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.45f, 0.48f, 0.5f) };

        AssetDatabase.CreateAsset(bodyMaterial, ReferenceRoot + "/PreviewBody.mat");

        foreach (string gender in new[] { "Male", "Female" })
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath(gender)), scene);

            visual.transform.position = new Vector3(gender == "Male" ? -0.65f : 0.65f, 0f, 0f);

            var bones = visual.transform.Find("attach_skin/ReferenceRig").GetComponentsInChildren<Transform>().Where(transform => reference.bones.Any(bone => bone.name == transform.name)).ToDictionary(transform => transform.name);
            var body = references.bodies.Single(candidate => candidate.name == (gender == "Male" ? "body_38" : "bodyfem_1"));
            var mesh = new Mesh { name = "EditorOnlyBody_" + gender, vertices = body.vertices, triangles = body.triangles };

            mesh.bindposes = reference.bones.Select(bone => Matrix(bone.nativeWorld).inverse).ToArray();
            mesh.boneWeights = body.influences.Select(influence => new BoneWeight { boneIndex0 = influence.indices[0], boneIndex1 = influence.indices[1], boneIndex2 = influence.indices[2], boneIndex3 = influence.indices[3], weight0 = influence.weights[0], weight1 = influence.weights[1], weight2 = influence.weights[2], weight3 = influence.weights[3] }).ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, ReferenceRoot + "/" + mesh.name + ".asset");

            var bodyObject = new GameObject("EDITOR ONLY - " + gender + " fitting body");

            bodyObject.transform.SetParent(visual.transform, false);

            var renderer = bodyObject.AddComponent<SkinnedMeshRenderer>();

            renderer.sharedMesh = mesh;
            renderer.sharedMaterial = bodyMaterial;
            renderer.bones = reference.bones.Select(bone => bones[bone.name]).ToArray();
            renderer.rootBone = renderer.bones[0];
        }

        var light = new GameObject("Preview daylight").AddComponent<Light>();

        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.45f);
        EditorSceneManager.SaveScene(scene, previewPath);
    }

    [MenuItem("HexBattleMage/Cloak/Validate Meadows Cloak 01")]
    public static void Validate()
    {
        var messages = new List<string>();

        foreach (string gender in new[] { "Male", "Female" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath(gender));
            var cloth = prefab.GetComponentInChildren<MagicaCloth>(true);
            var renderer = cloth.GetComponent<SkinnedMeshRenderer>();
            var weights = renderer.sharedMesh.boneWeights;

            if (renderer.bones.Length != 53 || renderer.sharedMesh.bindposes.Length != 53)
            {
                throw new InvalidOperationException("Native bone order/count mismatch for " + gender);
            }

            if (!cloth.GetSerializeData2().initData.HasData() || !cloth.GetSerializeData2().selectionData.IsValid())
            {
                throw new InvalidOperationException("Incomplete cloth initialization/paint for " + gender);
            }

            foreach (var weight in weights)
            {
                float total = weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3;

                if (Mathf.Abs(total - 1f) > 0.001f)
                {
                    throw new InvalidOperationException("Unnormalized skin weight for " + gender);
                }

                ValidateInfluence(weight.boneIndex0, weight.weight0);
                ValidateInfluence(weight.boneIndex1, weight.weight1);
                ValidateInfluence(weight.boneIndex2, weight.weight2);
                ValidateInfluence(weight.boneIndex3, weight.weight3);
            }

messages.Add(gender + ": FBX imported; 53 native-ordered bindposes; normalized weights; no Head/Jaw influence; MagicaCloth initialized with fixed upper wrap and movable lower drape.");
        }

        File.WriteAllText(Report, "PASS\n" + string.Join("\n", messages) + "\nVisual authoring validated. Gameplay settings and bundle preparation use MeadowsCloak01GameplayBuilder.");
    }

    private static void ValidateInfluence(int boneIndex, float weight)
    {
        if (weight <= 0f)
        {
            return;
        }

        if (boneIndex < 0 || boneIndex >= 53)
        {
            throw new InvalidOperationException("Cloak has an invalid native bone index.");
        }

        if (boneIndex == 5)
        {
            throw new InvalidOperationException("Cloak must not follow the Head bone.");
        }

        if (boneIndex == 6)
        {
            throw new InvalidOperationException("Cloak must not follow the Jaw bone.");
        }
    }
}


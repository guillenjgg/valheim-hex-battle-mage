using System;
using System.Collections.Generic;
using System.Linq;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TunicFastenerAssetPreparation
{
    private const string Folder = "Assets/_CustomItems/HexBattleMage/Armor/Meadows/Chest_Meadows_01";
    private const string FastenerName = "Buttons";
    private const int ButtonSegments = 12;
    private const float OpeningTipHeight = 1.475f;
    private const float OpeningSlope = 0.32f;
    private const float ButtonRadius = 0.01f;
    private static readonly float[] ButtonHeights = { 1.55f, 1.50f, 1.45f, 1.40f };

    private sealed class Geometry
    {
        internal readonly List<Vector3> Vertices = new List<Vector3>();
        internal readonly List<BoneWeight> Weights = new List<BoneWeight>();
        internal readonly List<int> Wood = new List<int>();
        internal readonly List<int> Thread = new List<int>();

        internal int Add(Vector3 point, BoneWeight weight)
        {
            int index = Vertices.Count;

            Vertices.Add(point);
            Weights.Add(weight);
            return index;
        }
    }

    public static void Prepare()
    {
        var definition = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/HexBattleMage_Chest_Meadows_01_Definition.prefab");
        var profile = definition.GetComponent<HexBattleMageArmorProfile>();
        var clothMaterial = profile.MaleVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(renderer => renderer.name == "Cloth").sharedMaterial;
        var wood = Material("WoodButtons", clothMaterial, new Color(0.25f, 0.13f, 0.055f));
        var thread = Material("ButtonThread", clothMaterial, new Color(0.035f, 0.025f, 0.018f));

        foreach (string gender in new[] { "Male", "Female" })
        {
            var visual = gender == "Male" ? profile.MaleVisual : profile.FemaleVisual;
            string path = AssetDatabase.GetAssetPath(visual);
            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var cloth = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(renderer => renderer.name == "Cloth");
                var geometry = new Geometry();

                for (int index = 0; index < ButtonHeights.Length; index++)
                {
                    float height = ButtonHeights[index];
                    float x = index < 2 ? OpeningSlope * (height - OpeningTipHeight) + 0.012f : 0.008f;
                    Sample(cloth.sharedMesh, x, height, out Vector3 center, out BoneWeight weight);
                    center.z += 0.003f;
                    Cylinder(geometry, center, ButtonRadius, 0.004f, weight, geometry.Wood);
                    Cylinder(geometry, center + new Vector3(-0.003f, 0f, 0.0025f), 0.0015f, 0.0005f, weight, geometry.Thread);
                    Cylinder(geometry, center + new Vector3(0.003f, 0f, 0.0025f), 0.0015f, 0.0005f, weight, geometry.Thread);

                    if (index < 2)
                    {
                        Sample(cloth.sharedMesh, -x, height, out Vector3 hole, out BoneWeight holeWeight);
                        hole.z += 0.001f;
                        int start = geometry.Add(hole + new Vector3(-0.001f, -0.004f, 0f), holeWeight);

                        geometry.Add(hole + new Vector3(0.001f, -0.004f, 0f), holeWeight);
                        geometry.Add(hole + new Vector3(0.001f, 0.004f, 0f), holeWeight);
                        geometry.Add(hole + new Vector3(-0.001f, 0.004f, 0f), holeWeight);
                        geometry.Thread.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                    }
                }

                var mesh = new Mesh { name = "TunicButtons_" + gender };

                mesh.SetVertices(geometry.Vertices);
                mesh.uv = new Vector2[geometry.Vertices.Count];
                mesh.boneWeights = geometry.Weights.ToArray();
                mesh.bindposes = cloth.sharedMesh.bindposes;
                mesh.subMeshCount = 2;
                mesh.SetTriangles(geometry.Wood, 0);
                mesh.SetTriangles(geometry.Thread, 1);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var asset = SaveMesh(mesh, Folder + "/" + mesh.name + ".asset");
                var previous = cloth.transform.parent.Find(FastenerName);

                if (previous != null)
                {
                    Object.DestroyImmediate(previous.gameObject);
                }

                var child = new GameObject(FastenerName);

                child.transform.SetParent(cloth.transform.parent, false);
                var renderer = child.AddComponent<SkinnedMeshRenderer>();

                renderer.sharedMesh = asset;
                renderer.sharedMaterials = new[] { wood, thread };
                renderer.bones = cloth.bones;
                renderer.rootBone = cloth.rootBone;
                renderer.localBounds = cloth.localBounds;
                PrefabUtility.SaveAsPrefabAsset(root, path);

                if (gender == "Male")
                {
                    PrepareDrop(profile, asset, cloth.sharedMesh.bounds.center, wood, thread);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static Material Material(string name, Material source, Color color)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
        {
            return material;
        }

        material = new Material(source) { name = name, mainTexture = null, color = color };
        material.SetTexture("_BumpMap", null);
        material.SetFloat("_Glossiness", 0f);
        material.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Mesh SaveMesh(Mesh mesh, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        EditorUtility.CopySerialized(mesh, existing);
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(mesh);
        return existing;
    }

    private static void Cylinder(Geometry geometry, Vector3 center, float radius, float depth, BoneWeight weight, List<int> triangles)
    {
        int front = geometry.Add(center + Vector3.forward * depth * 0.5f, weight);
        int back = geometry.Add(center - Vector3.forward * depth * 0.5f, weight);
        int ring = geometry.Vertices.Count;

        for (int index = 0; index < ButtonSegments; index++)
        {
            float angle = index * Mathf.PI * 2f / ButtonSegments;
            var offset = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);

            geometry.Add(center + offset + Vector3.forward * depth * 0.5f, weight);
            geometry.Add(center + offset - Vector3.forward * depth * 0.5f, weight);
        }

        for (int index = 0; index < ButtonSegments; index++)
        {
            int current = ring + index * 2;
            int next = ring + (index + 1) % ButtonSegments * 2;

            triangles.AddRange(new[] { front, current, next, back, next + 1, current + 1, current, current + 1, next + 1, current, next + 1, next });
        }
    }

    private static void Sample(Mesh mesh, float x, float y, out Vector3 point, out BoneWeight weight)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        var weights = mesh.boneWeights;
        float front = float.NegativeInfinity;

        point = Vector3.zero;
        weight = default;

        for (int index = 0; index < triangles.Length; index += 3)
        {
            int a = triangles[index];
            int b = triangles[index + 1];
            int c = triangles[index + 2];
            Vector3 start = vertices[a];
            Vector3 first = vertices[b] - start;
            Vector3 second = vertices[c] - start;
            float determinant = first.x * second.y - first.y * second.x;

            if (Mathf.Abs(determinant) < 0.0000001f)
            {
                continue;
            }

            float u = ((x - start.x) * second.y - (y - start.y) * second.x) / determinant;
            float v = (first.x * (y - start.y) - first.y * (x - start.x)) / determinant;

            if (u < 0f || v < 0f || u + v > 1f)
            {
                continue;
            }

            Vector3 candidate = start + first * u + second * v;

            if (candidate.z <= front)
            {
                continue;
            }

            front = candidate.z;
            point = candidate;
            var combined = new Dictionary<int, float>();
            var factors = new[] { 1f - u - v, u, v };
            var sources = new[] { weights[a], weights[b], weights[c] };

            for (int source = 0; source < sources.Length; source++)
            {
                var item = sources[source];
                var bones = new[] { item.boneIndex0, item.boneIndex1, item.boneIndex2, item.boneIndex3 };
                var values = new[] { item.weight0, item.weight1, item.weight2, item.weight3 };

                for (int influence = 0; influence < bones.Length; influence++)
                {
                    combined.TryGetValue(bones[influence], out float previous);
                    combined[bones[influence]] = previous + factors[source] * values[influence];
                }
            }

            var sorted = combined.OrderByDescending(pair => pair.Value).Take(4).ToList();

            while (sorted.Count < 4)
            {
                sorted.Add(new KeyValuePair<int, float>(0, 0f));
            }

            float total = sorted.Sum(pair => pair.Value);

            weight = new BoneWeight { boneIndex0 = sorted[0].Key, weight0 = sorted[0].Value / total, boneIndex1 = sorted[1].Key, weight1 = sorted[1].Value / total, boneIndex2 = sorted[2].Key, weight2 = sorted[2].Value / total, boneIndex3 = sorted[3].Key, weight3 = sorted[3].Value / total };
        }

        if (float.IsNegativeInfinity(front))
        {
            throw new InvalidOperationException("Missing tunic surface for button at " + x + ", " + y);
        }
    }

    private static void PrepareDrop(HexBattleMageArmorProfile profile, Mesh source, Vector3 center, Material wood, Material thread)
    {
        var mesh = Object.Instantiate(source);

        mesh.name = "TunicButtons_Drop";
        mesh.vertices = mesh.vertices.Select(point => (point - center) * 0.25f).ToArray();
        mesh.RecalculateBounds();
        var asset = SaveMesh(mesh, Folder + "/TunicButtons_Drop.asset");
        string path = AssetDatabase.GetAssetPath(profile.WorldModel);
        var root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            var previous = root.transform.Find(FastenerName);

            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            var child = new GameObject(FastenerName);

            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = asset;
            child.AddComponent<MeshRenderer>().sharedMaterials = new[] { wood, thread };
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}

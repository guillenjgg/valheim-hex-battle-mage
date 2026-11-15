using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WandRuneAssetBuilder
{
    private const string WandPath = "Assets/_CustomItems/HexBattleMage/Wands/HexBattleMageWand/HexBattleMageWand.prefab";
    private const string RuneDirectory = "Assets/_CustomItems/HexBattleMage/Progression/Wands/Apprentice";
    private const string RequestPath = "Temp/HexBattleMageWandRunes.request";
    private const string ResultPath = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/wand-runes-result.txt";
    private const int StrokeSegments = 12;
    private const int RuneSlotCount = 10;
    private const float RuneRegionStart = 0.4f;
    private const float RuneRegionEnd = 0.9f;
    private const float RuneLength = 0.026f;
    private const float RuneHalfWidth = 0.0065f;

    static WandRuneAssetBuilder()
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
            PrepareAndBuild();
        }
        catch (Exception exception)
        {
            File.WriteAllText(ResultPath, exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("HexBattleMage/Prepare Apprentice Wand Rune")]
    public static void Prepare()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();

        if (stage != null && stage.scene.isDirty)
        {
            throw new InvalidOperationException("Save the open prefab before preparing wand runes.");
        }

        Directory.CreateDirectory(RuneDirectory);
        AssetDatabase.Refresh();

        var wand = PrefabUtility.LoadPrefabContents(WandPath);

        try
        {
            var model = wand.transform.Find("attach/WandModel");
            var mesh = model.GetComponent<MeshFilter>().sharedMesh;
            var bounds = mesh.bounds;
            float regionStart = Mathf.Lerp(bounds.min.z, bounds.max.z, RuneRegionStart);
            float regionEnd = Mathf.Lerp(bounds.min.z, bounds.max.z, RuneRegionEnd);
            float slotSpacing = (regionEnd - regionStart) / RuneSlotCount;

            if (RuneLength >= slotSpacing)
            {
                throw new InvalidOperationException("Rune length must leave a gap between progression slots.");
            }

            float centerZ = regionStart + slotSpacing * 0.5f;
            float minimumX = float.PositiveInfinity;
            float maximumX = float.NegativeInfinity;

            foreach (var vertex in mesh.vertices)
            {
                if (Mathf.Abs(vertex.z - centerZ) > RuneLength)
                {
                    continue;
                }

                minimumX = Mathf.Min(minimumX, vertex.x);
                maximumX = Mathf.Max(maximumX, vertex.x);
            }

            if (float.IsInfinity(minimumX))
            {
                throw new InvalidOperationException("Cannot locate the wand shaft for the Apprentice rune.");
            }

            float centerX = (minimumX + maximumX) * 0.5f;
            var rune = new GameObject("ApprenticeRune");

            try
            {
                var sourceMaterial = model.GetComponent<MeshRenderer>().sharedMaterial;
                var groove = PrepareMaterial("RuneGroove", sourceMaterial, new Color(0.04f, 0.025f, 0.015f), Color.black);
                var glow = PrepareMaterial("RuneGlow", sourceMaterial, new Color(0.35f, 0.75f, 1f), new Color(3f, 6f, 9f));

                AddSurface(rune, "EngravedBorder", BuildRune(mesh, centerX, centerZ, 0.004f, 0.0006f), groove);
                AddSurface(rune, "GlowingInlay", BuildRune(mesh, centerX, centerZ, 0.0024f, 0.0009f), glow);

                string runePath = RuneDirectory + "/ApprenticeRune.prefab";
                var runeAsset = PrefabUtility.SaveAsPrefabAsset(rune, runePath);
                AssetImporter.GetAtPath(runePath).SetAssetBundleNameAndVariant("hexbattlemage", string.Empty);
                var progression = model.Find("ProgressionRunes");

                if (progression == null)
                {
                    progression = new GameObject("ProgressionRunes").transform;
                    progression.SetParent(model, false);
                }

                var existing = progression.Find("Tier01_Apprentice");

                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }

                Transform firstSlot = null;

                for (int slotIndex = 0; slotIndex < RuneSlotCount; slotIndex++)
                {
                    string slotName = "RuneSlot" + (slotIndex + 1).ToString("D2");
                    var slot = progression.Find(slotName);

                    if (slot == null)
                    {
                        slot = new GameObject(slotName).transform;
                        slot.SetParent(progression, false);
                    }

                    slot.localPosition = new Vector3(0f, 0f, regionStart + slotSpacing * (slotIndex + 0.5f));

                    if (slotIndex == 0)
                    {
                        firstSlot = slot;
                    }
                }

                var previousRune = firstSlot.Find("Tier01_Apprentice");

                if (previousRune != null)
                {
                    Object.DestroyImmediate(previousRune.gameObject);
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(runeAsset, firstSlot);

                instance.transform.localPosition = new Vector3(centerX, 0f, 0f);

                instance.name = "Tier01_Apprentice";
                PrefabUtility.SaveAsPrefabAsset(wand, WandPath);
                SavePreview(model.gameObject, new Vector3(centerX, 0f, centerZ));
                File.WriteAllText(ResultPath, "SUCCESS: One Apprentice rune, repeated around the shaft for visibility from all sides, with surface-conforming dark engraving border and emissive inlay.\nMesh bounds: " + bounds + "\nRune center: " + centerX + ", " + centerZ + "\nTen evenly spaced progression slots reserved; 2.6 cm runes with gaps. Only the Apprentice slot is populated. No upgrade unlock rule or quality mapping added. Bundle needs rebuilding.");
            }
            finally
            {
                Object.DestroyImmediate(rune);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(wand);
        }

        AssetDatabase.SaveAssets();
    }

    public static void PrepareAndBuild()
    {
        Prepare();
        MageTableAssetBuilder.PrepareAndBuild();
        File.AppendAllText(ResultPath, "\nBundle rebuilt, validated and copied into the mod.");
    }

    private static void SavePreview(GameObject model, Vector3 center)
    {
        var preview = new PreviewRenderUtility();

        try
        {
            preview.BeginStaticPreview(new Rect(0f, 0f, 768f, 768f));
            preview.camera.orthographic = true;
            preview.camera.orthographicSize = 0.075f;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 2f;
            preview.camera.transform.position = center + Vector3.up * 0.3f;
            preview.camera.transform.LookAt(center, Vector3.forward);
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            preview.lights[0].intensity = 1.2f;
            preview.lights[0].transform.rotation = Quaternion.Euler(65f, 0f, 0f);

            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();

                if (renderer == null)
                {
                    continue;
                }

                var matrix = model.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;

                preview.DrawMesh(filter.sharedMesh, matrix, renderer.sharedMaterial, 0);
            }

            preview.Render();

            var texture = preview.EndStaticPreview();

            File.WriteAllBytes("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/apprentice-rune-preview.png", texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        finally
        {
            preview.Cleanup();
        }
    }

    private static Material PrepareMaterial(string name, Material source, Color color, Color emission)
    {
        string path = RuneDirectory + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(source);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = source.shader;
        material.mainTexture = Texture2D.whiteTexture;
        material.SetTexture("_EmissionMap", Texture2D.whiteTexture);
        material.SetTexture("_BumpMap", null);
        material.SetColor("_Color", color);
        material.SetColor("_EmissionColor", emission);
        material.SetFloat("_Glossiness", 0.1f);
        material.SetFloat("_Cull", 0f);
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void AddSurface(GameObject root, string name, Mesh mesh, Material material)
    {
        string path = RuneDirectory + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            existing = mesh;
        }
        else
        {
            existing.Clear();
            existing.vertices = mesh.vertices;
            existing.triangles = mesh.triangles;
            existing.uv = mesh.uv;
            existing.RecalculateNormals();
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
        }

        var child = new GameObject(name);

        child.transform.SetParent(root.transform, false);
        child.AddComponent<MeshFilter>().sharedMesh = existing;
        child.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Mesh BuildRune(Mesh shaft, float x, float z, float width, float offset)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var center = new Vector2(x, z);
        var starts = new[] { new Vector2(0f, -RuneLength * 0.5f), new Vector2(0f, 0f), new Vector2(0f, 0f) };
        var ends = new[] { new Vector2(0f, RuneLength * 0.5f), new Vector2(-RuneHalfWidth, RuneLength * 0.28f), new Vector2(RuneHalfWidth, RuneLength * 0.28f) };

        const int radialFaces = 6;

        for (int face = 0; face < radialFaces; face++)
        {
            var rotation = Quaternion.Euler(0f, 0f, face * 360f / radialFaces);
            var inverseRotation = Quaternion.Inverse(rotation);
            var rotatedVertices = shaft.vertices;

            for (int index = 0; index < rotatedVertices.Length; index++)
            {
                rotatedVertices[index] = rotation * rotatedVertices[index];
            }

            var projectionMesh = new Mesh { vertices = rotatedVertices, triangles = shaft.triangles };
            for (int stroke = 0; stroke < starts.Length; stroke++)
            {
                var direction = (ends[stroke] - starts[stroke]).normalized;
                var across = new Vector2(-direction.y, direction.x) * width * 0.5f;
                int first = vertices.Count;

                for (int segment = 0; segment <= StrokeSegments; segment++)
                {
                    var point = center + Vector2.Lerp(starts[stroke], ends[stroke], segment / (float)StrokeSegments);

                    vertices.Add(inverseRotation * Project(projectionMesh, point - across, 1, offset, x) - new Vector3(x, 0f, z));
                    vertices.Add(inverseRotation * Project(projectionMesh, point + across, 1, offset, x) - new Vector3(x, 0f, z));

                    if (segment == 0)
                    {
                        continue;
                    }

                    int index = first + segment * 2;

                    triangles.AddRange(new[] { index - 2, index - 1, index, index - 1, index + 1, index });
                }
            }

            Object.DestroyImmediate(projectionMesh);
        }

        var result = new Mesh { name = "ApprenticeRune" };

        result.SetVertices(vertices);
        result.SetTriangles(triangles, 0);
        result.uv = new Vector2[vertices.Count];
        result.RecalculateNormals();
        result.RecalculateBounds();
        return result;
    }

    private static Vector3 Project(Mesh shaft, Vector2 point, int face, float offset, float originX)
    {
        var vertices = shaft.vertices;
        var indices = shaft.triangles;
        float minimumX = float.PositiveInfinity;
        float maximumX = float.NegativeInfinity;

        for (int edge = 0; edge < indices.Length; edge++)
        {
            int triangleStart = edge - edge % 3;
            var a = vertices[indices[edge]];
            var b = vertices[indices[triangleStart + (edge % 3 + 1) % 3]];

            if (Mathf.Abs(b.z - a.z) < 0.000001f)
            {
                continue;
            }

            float along = (point.y - a.z) / (b.z - a.z);

            if (along < 0f || along > 1f)
            {
                continue;
            }

            float edgeX = Mathf.Lerp(a.x, b.x, along);

            minimumX = Mathf.Min(minimumX, edgeX);
            maximumX = Mathf.Max(maximumX, edgeX);
        }

        float halfWidth = (maximumX - minimumX) * 0.5f;
        float widthScale = Mathf.Min(1f, halfWidth * 0.85f / (RuneHalfWidth + 0.0025f));

        point.x = (minimumX + maximumX) * 0.5f + (point.x - originX) * widthScale;

        float surface = face > 0 ? float.NegativeInfinity : float.PositiveInfinity;

        for (int index = 0; index < indices.Length; index += 3)
        {
            var a = vertices[indices[index]];
            var b = vertices[indices[index + 1]];
            var c = vertices[indices[index + 2]];
            float determinant = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);

            if (Mathf.Abs(determinant) < 0.00000001f)
            {
                continue;
            }

            float u = ((b.z - c.z) * (point.x - c.x) + (c.x - b.x) * (point.y - c.z)) / determinant;
            float v = ((c.z - a.z) * (point.x - c.x) + (a.x - c.x) * (point.y - c.z)) / determinant;

            if (u < 0f || v < 0f || u + v > 1f)
            {
                continue;
            }

            float y = u * a.y + v * b.y + (1f - u - v) * c.y;

            surface = face > 0 ? Mathf.Max(surface, y) : Mathf.Min(surface, y);
        }

        if (float.IsInfinity(surface))
        {
            throw new InvalidOperationException("Rune falls outside the wand shaft at " + point + ".");
        }

        return new Vector3(point.x, surface + face * offset, point.y);
    }
}

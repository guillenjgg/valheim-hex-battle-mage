using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MageTableAlchemyDetails
{
    private const string DetailsName = "AlchemyDetails";
    private const string AssetDirectory = "Assets/_CustomItems/HexBattleMage/Crafting/AlchemyDetails";
    private const int RadialSegments = 12;

    public static void Prepare(GameObject prefab, Material wood, Material stone)
    {
        PrepareOppositeSupportRunes(prefab);

        if (prefab.transform.Find(DetailsName) != null)
        {
            return;
        }

        Directory.CreateDirectory(AssetDirectory);
        AssetDatabase.Refresh();

        float surface = FindSurfaceHeight(prefab);
        var root = new GameObject(DetailsName).transform;

        root.SetParent(prefab.transform, false);
        root.localPosition = new Vector3(0f, surface, 0f);
        AddBox(root, "Backboard", wood, new Vector3(0f, 0.12f, 0.46f), new Vector3(2f, 0.24f, 0.065f), true);
        AddBox(root, "BackboardCap", wood, new Vector3(0f, 0.255f, 0.46f), new Vector3(2.04f, 0.035f, 0.09f), true);
        AddBox(root, "LeftPanel", wood, new Vector3(-0.97f, 0.1f, 0f), new Vector3(0.06f, 0.2f, 0.94f), true);
        AddBox(root, "RightPanel", wood, new Vector3(0.97f, 0.1f, 0f), new Vector3(0.06f, 0.2f, 0.94f), true);

        var blue = PrepareMaterial("BlueStoneware", stone, new Color(0.22f, 0.36f, 0.4f));
        var green = PrepareMaterial("GreenStoneware", stone, new Color(0.32f, 0.4f, 0.23f));
        var violet = PrepareMaterial("VioletStoneware", stone, new Color(0.36f, 0.25f, 0.4f));
        var leather = PrepareMaterial("BookLeather", stone, new Color(0.18f, 0.09f, 0.055f));
        var pages = PrepareMaterial("AgedPages", stone, new Color(0.68f, 0.59f, 0.41f));
        var bottle = PrepareLathe("AlchemyBottle", new[] { new Vector2(0f, 0f), new Vector2(0.055f, 0f), new Vector2(0.075f, 0.035f), new Vector2(0.065f, 0.14f), new Vector2(0.025f, 0.18f), new Vector2(0.025f, 0.23f), new Vector2(0f, 0.23f) });

        AddBottle(root, bottle, blue, wood, "BlueBottle", new Vector3(-0.68f, 0.015f, 0.29f), 1f);
        AddBottle(root, bottle, green, wood, "GreenBottle", new Vector3(-0.49f, 0.015f, 0.32f), 0.8f);
        AddBottle(root, bottle, violet, wood, "VioletBottle", new Vector3(-0.31f, 0.015f, 0.29f), 0.65f);

        var book = new GameObject("WornNotebook").transform;

        book.SetParent(root, false);
        book.localPosition = new Vector3(-0.57f, 0.015f, -0.23f);
        book.localRotation = Quaternion.Euler(0f, -12f, 0f);
        AddBox(book, "BottomCover", leather, Vector3.zero, new Vector3(0.33f, 0.015f, 0.24f), false);
        AddBox(book, "Pages", pages, new Vector3(0f, 0.027f, 0f), new Vector3(0.3f, 0.045f, 0.215f), false);
        AddBox(book, "TopCover", leather, new Vector3(0f, 0.057f, 0f), new Vector3(0.33f, 0.015f, 0.24f), false);

        var bowl = PrepareLathe("MortarBowl", new[] { new Vector2(0f, 0f), new Vector2(0.09f, 0f), new Vector2(0.14f, 0.07f), new Vector2(0.14f, 0.09f), new Vector2(0.115f, 0.09f), new Vector2(0.075f, 0.025f), new Vector2(0f, 0.025f) });

        AddMesh(root, "StoneMortar", bowl, stone, new Vector3(0.63f, 0.015f, 0.25f), Vector3.one);

        var pestle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        pestle.name = "Pestle";
        pestle.transform.SetParent(root, false);
        pestle.transform.localPosition = new Vector3(0.66f, 0.105f, 0.25f);
        pestle.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        pestle.transform.localScale = new Vector3(0.045f, 0.12f, 0.045f);
        pestle.GetComponent<MeshRenderer>().sharedMaterial = stone;
        Object.DestroyImmediate(pestle.GetComponent<Collider>());
    }

    private static void PrepareOppositeSupportRunes(GameObject prefab)
    {
        const string oppositeRunesName = "OppositeSupportRunes";
        var model = prefab.transform.Find("mage-table");

        if (model == null)
        {
            throw new InvalidOperationException("Mage Table model was not found.");
        }

        if (prefab.transform.Find(oppositeRunesName) != null)
        {
            return;
        }

        var runes = new GameObject(oppositeRunesName).transform;

        runes.SetParent(prefab.transform, false);

        int count = 0;

        foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
        {
            bool isSupportRune = renderer.name.StartsWith("Left_Support_Rune_", StringComparison.Ordinal) || renderer.name.StartsWith("Right_Support_Rune_", StringComparison.Ordinal);

            if (!isSupportRune)
            {
                continue;
            }

            var copy = Object.Instantiate(renderer.gameObject, runes, true);

            copy.name = renderer.name + "_Opposite";
            count++;
        }

        if (count != 6)
        {
            throw new InvalidOperationException("Expected six support rune meshes, found " + count + ".");
        }

        runes.localRotation = Quaternion.Euler(0f, 180f, 0f);
    }
    public static void SavePreview(GameObject prefab)
    {
        var preview = new PreviewRenderUtility();

        try
        {
            preview.BeginStaticPreview(new Rect(0f, 0f, 768f, 768f));
            preview.camera.fieldOfView = 35f;
            preview.camera.nearClipPlane = 0.1f;
            preview.camera.farClipPlane = 20f;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
            preview.camera.transform.position = new Vector3(2.8f, 2.4f, -3.6f);
            preview.camera.transform.LookAt(new Vector3(0f, 0.65f, 0f));
            preview.lights[0].intensity = 1.2f;
            preview.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
            preview.lights[1].intensity = 0.7f;
            preview.lights[1].transform.rotation = Quaternion.Euler(20f, -145f, 0f);

            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();

                if (renderer == null)
                {
                    continue;
                }

                if (!renderer.enabled)
                {
                    continue;
                }

                var mesh = filter.sharedMesh;
                var materials = renderer.sharedMaterials;

                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    preview.DrawMesh(mesh, filter.transform.localToWorldMatrix, materials[Mathf.Min(submesh, materials.Length - 1)], submesh);
                }
            }

            preview.Render();

            var texture = preview.EndStaticPreview();

            File.WriteAllBytes("C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/mage-table-alchemy-preview.png", texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        finally
        {
            preview.Cleanup();
        }
    }

    private static float FindSurfaceHeight(GameObject prefab)
    {
        float height = 0f;
        bool found = false;

        foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!renderer.name.StartsWith("Oak_Top_Plank_", StringComparison.Ordinal))
            {
                continue;
            }

            found = true;
            height = Mathf.Max(height, prefab.transform.InverseTransformPoint(renderer.bounds.max).y);
        }

        if (!found)
        {
            throw new InvalidOperationException("Mage Table top planks were not found; refusing to guess prop placement.");
        }

        return height;
    }

    private static Material PrepareMaterial(string name, Material source, Color color)
    {
        string path = AssetDirectory + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(source);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = source.shader;
        material.mainTexture = Texture2D.whiteTexture;
        material.SetTexture("_BumpMap", null);
        material.SetTexture("_EmissionMap", null);
        material.SetColor("_Color", color);
        material.SetColor("_EmissionColor", Color.black);
        material.SetFloat("_Glossiness", 0.15f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void AddBox(Transform parent, string name, Material material, Vector3 position, Vector3 scale, bool keepCollider)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);

        box.name = name;
        box.layer = LayerMask.NameToLayer("piece");
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localScale = scale;
        box.GetComponent<MeshRenderer>().sharedMaterial = material;

        if (!keepCollider)
        {
            Object.DestroyImmediate(box.GetComponent<Collider>());
        }
    }

    private static void AddBottle(Transform parent, Mesh mesh, Material material, Material cork, string name, Vector3 position, float scale)
    {
        var bottle = AddMesh(parent, name, mesh, material, position, Vector3.one * scale);

        AddBox(bottle, "Cork", cork, new Vector3(0f, 0.242f, 0f), new Vector3(0.04f, 0.035f, 0.04f), false);
    }

    private static Transform AddMesh(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale)
    {
        var child = new GameObject(name).transform;

        child.SetParent(parent, false);
        child.localPosition = position;
        child.localScale = scale;
        child.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        child.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        return child;
    }

    private static Mesh PrepareLathe(string name, Vector2[] profile)
    {
        string path = AssetDirectory + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (existing != null)
        {
            return existing;
        }

        var vertices = new Vector3[(RadialSegments + 1) * profile.Length];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[RadialSegments * (profile.Length - 1) * 6];
        int triangle = 0;

        for (int segment = 0; segment <= RadialSegments; segment++)
        {
            float angle = segment / (float)RadialSegments * Mathf.PI * 2f;

            for (int point = 0; point < profile.Length; point++)
            {
                int index = segment * profile.Length + point;

                vertices[index] = new Vector3(Mathf.Cos(angle) * profile[point].x, profile[point].y, Mathf.Sin(angle) * profile[point].x);
                uv[index] = new Vector2(segment / (float)RadialSegments, point / (float)(profile.Length - 1));

                if (segment == RadialSegments || point == profile.Length - 1)
                {
                    continue;
                }

                int next = index + profile.Length;

                triangles[triangle++] = index;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = next;
                triangles[triangle++] = next;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = next + 1;
            }
        }

        var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = triangles };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
}


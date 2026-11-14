using Jotunn.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Visuals
{
    internal static class MageRunestoneStructure
    {
        internal const float TableSurfaceHeight = 1f;
        private const float TableWidth = 2f;
        private const float TableDepth = 1.15f;
        private const float TabletopThickness = 0.18f;
        private const float SupportOffset = 0.65f;
        private const float SupportWidth = 0.3f;
        private const float SupportDepth = 0.85f;
        private const float SupportHeight = TableSurfaceHeight - TabletopThickness;
        private const float StoneWidth = 1.3f;
        private const float StoneHeight = 1.6f;
        private const float StoneDepth = 0.5f;

        internal static void ConfigureCollision(GameObject prefab)
        {
            foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.isTrigger)
                {
                    collider.enabled = false;
                }
            }

            AddCollision(prefab, new Vector3(0f, TableSurfaceHeight - TabletopThickness * 0.5f, 0f), new Vector3(TableWidth, TabletopThickness, TableDepth));
            AddCollision(prefab, new Vector3(-SupportOffset, SupportHeight * 0.5f, 0f), new Vector3(SupportWidth, SupportHeight, SupportDepth));
            AddCollision(prefab, new Vector3(SupportOffset, SupportHeight * 0.5f, 0f), new Vector3(SupportWidth, SupportHeight, SupportDepth));
        }

        private static void AddCollision(GameObject prefab, Vector3 center, Vector3 size)
        {
            var collider = prefab.AddComponent<BoxCollider>();

            collider.center = center;
            collider.size = size;
        }

        internal static void ReplaceWoodenVisuals(GameObject prefab)
        {
            var stonePrefab = PrefabManager.Instance.GetPrefab("stone_wall_1x1");

            if (stonePrefab == null)
            {
                DebugLog.LogError("Cannot create Mage Table stone body: stone_wall_1x1 is unavailable.");
                return;
            }

            var sourceRenderer = stonePrefab.GetComponentInChildren<MeshRenderer>(true);

            if (sourceRenderer == null)
            {
                DebugLog.LogError("Cannot create Mage Table stone body: no stone renderer was found.");
                return;
            }

            var material = new Material(sourceRenderer.sharedMaterial)
            {
                name = "MageTableRunestone"
            };

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", new Color(0.4f, 0.42f, 0.48f, 1f));
            }

            foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true))
            {
                lod.enabled = false;
            }

            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.enabled = false;
            }

            var mesh = CreateStoneMesh();

            var tabletopPosition = new Vector3(0f, TableSurfaceHeight - TabletopThickness * 0.5f, -TableDepth * 0.5f);
            var tabletopScale = new Vector3(TableWidth / StoneWidth, TableDepth / StoneHeight, TabletopThickness / StoneDepth);
            var supportScale = new Vector3(SupportWidth / StoneWidth, SupportHeight / StoneHeight, SupportDepth / StoneDepth);

            AddStone(prefab.transform, "RunicTabletop", mesh, material, tabletopPosition, tabletopScale, Quaternion.Euler(90f, 0f, 0f));
            AddStone(prefab.transform, "LeftStoneSupport", mesh, material, new Vector3(-SupportOffset, 0f, 0f), supportScale, Quaternion.identity);
            AddStone(prefab.transform, "RightStoneSupport", mesh, material, new Vector3(SupportOffset, 0f, 0f), supportScale, Quaternion.identity);
        }

        private static void AddStone(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            var body = new GameObject(name);

            body.transform.SetParent(parent, false);
            body.transform.localPosition = position;
            body.transform.localScale = scale;
            body.transform.localRotation = rotation;
            body.AddComponent<MeshFilter>().sharedMesh = mesh;
            body.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh CreateStoneMesh()
        {
            var outline = new[]
            {
                new Vector2(-StoneWidth * 0.5f, 0f), new Vector2(StoneWidth * 0.5f, 0f),
                new Vector2(StoneWidth * 0.5f, StoneHeight * 0.72f), new Vector2(StoneWidth * 0.2f, StoneHeight),
                new Vector2(-StoneWidth * 0.2f, StoneHeight), new Vector2(-StoneWidth * 0.5f, StoneHeight * 0.72f)
            };
            int count = outline.Length;
            var vertices = new Vector3[count * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(count - 2) * 6 + count * 6];
            int triangle = 0;

            for (int index = 0; index < count; index++)
            {
                vertices[index] = new Vector3(outline[index].x, outline[index].y, -StoneDepth * 0.5f);
                vertices[index + count] = new Vector3(outline[index].x, outline[index].y, StoneDepth * 0.5f);
                uv[index] = new Vector2(outline[index].x / StoneWidth + 0.5f, outline[index].y / StoneHeight);
                uv[index + count] = uv[index];
            }

            for (int index = 1; index < count - 1; index++)
            {
                triangles[triangle++] = 0;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index;
                triangles[triangle++] = count;
                triangles[triangle++] = count + index;
                triangles[triangle++] = count + index + 1;
            }

            for (int index = 0; index < count; index++)
            {
                int next = (index + 1) % count;

                triangles[triangle++] = index;
                triangles[triangle++] = next;
                triangles[triangle++] = next + count;
                triangles[triangle++] = index;
                triangles[triangle++] = next + count;
                triangles[triangle++] = index + count;
            }

            var mesh = new Mesh { name = "MageRunestoneBody", vertices = vertices, triangles = triangles, uv = uv };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
using System;
using System.IO;
using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;

public static class WorldDropAssetPreparation
{
    private const string AssetRoot = "Assets/_CustomItems/HexBattleMage";
    private const float ColliderPadding = 0.012f;
    private const float MinimumColliderSize = 0.035f;

    public static void PrepareAll()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { AssetRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (source.GetComponent<MageEquipmentDefinition>() == null)
            {
                continue;
            }

            bool hasAuthoredColliderSize = File.ReadAllText(path).Contains("\n  DropColliderSize:");
            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var definition = root.GetComponent<MageEquipmentDefinition>();
                var armor = root.GetComponent<HexBattleMageArmorProfile>();

                if (armor != null && armor.ReplaceNativeVisuals && !hasAuthoredColliderSize)
                {
                    FitCollider(armor);
                }

                var body = root.GetComponent<Rigidbody>();

                if (body != null)
                {
                    body.collisionDetectionMode = definition.DropCollisionDetection;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
    }

    private static void FitCollider(HexBattleMageArmorProfile profile)
    {
        if (profile.WorldModel == null)
        {
            throw new InvalidOperationException("Custom armor needs an authored world model: " + profile.name);
        }

        Bounds bounds = default;
        bool initialized = false;

        foreach (var filter in profile.WorldModel.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
            {
                continue;
            }

            var meshBounds = filter.sharedMesh.bounds;
            var matrix = filter.transform.localToWorldMatrix;

            for (int corner = 0; corner < 8; corner++)
            {
                var direction = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                Vector3 point = matrix.MultiplyPoint3x4(meshBounds.center + Vector3.Scale(meshBounds.extents, direction));

                if (!initialized)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        if (!initialized)
        {
            throw new InvalidOperationException("Cannot fit an empty world model: " + profile.name);
        }

        profile.DropColliderCenter = bounds.center;
        profile.DropColliderSize = Vector3.Max(bounds.size + Vector3.one * ColliderPadding, Vector3.one * MinimumColliderSize);
    }
}

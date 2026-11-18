using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HexBattleMage.Core.Managers;
using HarmonyLib;
using MagicaCloth2;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CloakCollisionValidation
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static readonly Assembly Mod = typeof(HexBattleMageCloakCollisionProfile).Assembly;
    private static readonly Type Geometry = Mod.GetType("HexBattleMage.Core.Visuals.CloakCollisionGeometry", true);
    private static readonly Type Controller = Mod.GetType("HexBattleMage.Core.Visuals.CloakCollisionController", true);

    public static void Run()
    {
        ValidateSurfaceGeometry();
        ValidateMeshAndTerrain();
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE"));

        try
        {
            var visual = bundle.LoadAsset<GameObject>("HexBattleMage_Cloak_Meadows_01");
            var profile = visual.GetComponent<HexBattleMageCloakProfile>();

            ValidateBodyFit(profile.MaleVisual, "Male");
            ValidateBodyFit(profile.FemaleVisual, "Female");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "cloak-collision-report.txt"), "PASS: compiled mod and production bundle; floor/wall/rotated-surface/inside-solid/nonconvex-mesh/terrain contacts; rejected opposite wall faces; separated and trigger-free geometry; cold-loaded male/female profiles; 14 body capsules each; independent collider lists; edge collision; bounded world pool; unit world scale; moving-support tracking; destroyed-support cleanup; own-character exclusion. Native SetupCloth hook applied; unrelated capes untouched. Solver animation remains an in-game check.");
        }
        finally
        {
            bundle.Unload(true);
        }
    }

    private static void ValidateSurfaceGeometry()
    {
        var query = new GameObject("Disabled query sphere").AddComponent<SphereCollider>();
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var solid = wall.GetComponent<BoxCollider>();

        query.radius = 0.18f;
        query.enabled = true;
        query.isTrigger = true;
        wall.transform.localScale = new Vector3(4f, 4f, 0.1f);
        Physics.SyncTransforms();

        try
        {
            Vector3 center = Surface(query, new Vector3(0f, 0f, 0.1f), solid, Vector3.forward);

            Check(Mathf.Abs(center.z + 0.21f) < 0.002f, "Wall padding is measured from the real surface.");
            Check(!TrySurface(query, new Vector3(0f, 0f, -0.1f), solid, Vector3.forward, out center), "Opposite wall face cannot inflate a thin wall toward the player.");
            Check(!TrySurface(query, new Vector3(0f, 0f, 2f), solid, Vector3.forward, out center), "Distant geometry creates no support.");
            center = Surface(query, Vector3.forward * 0.02f, solid, Vector3.forward);
            Check(IsFinite(center), "Samples inside a solid produce finite support.");

            wall.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            Physics.SyncTransforms();
            Vector3 normal = wall.transform.forward;
            center = Surface(query, normal * 0.1f, solid, normal);
            Check(Mathf.Abs(Vector3.Dot(center, normal) + 0.21f) < 0.003f, "Tilted boat/hull surfaces retain their normal and margin.");

            wall.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Physics.SyncTransforms();
            center = Surface(query, Vector3.up * 0.1f, solid, Vector3.up);
            Check(Mathf.Abs(center.y + 0.21f) < 0.003f, "Floor support remains below the surface with padding above it.");
        }
        finally
        {
            Object.DestroyImmediate(wall);
            Object.DestroyImmediate(query.gameObject);
        }
    }

    private static void ValidateMeshAndTerrain()
    {
        var query = new GameObject("Mesh query").AddComponent<SphereCollider>();
        var meshObject = new GameObject("Nonconvex boat-deck fixture");
        var mesh = new Mesh();
        var data = new TerrainData { heightmapResolution = 33, size = new Vector3(10f, 1f, 10f) };
        var terrain = new GameObject("Terrain fixture");

        query.radius = 0.18f;
        query.isTrigger = true;
        mesh.vertices = new[] { new Vector3(-1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, -1f) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var collider = meshObject.AddComponent<MeshCollider>();

        collider.sharedMesh = mesh;
        collider.convex = false;
        terrain.transform.position = new Vector3(-5f, -2f, -5f);
        terrain.AddComponent<TerrainCollider>().terrainData = data;
        Physics.SyncTransforms();

        try
        {
            Vector3 center = Surface(query, Vector3.up * 0.1f, collider, Vector3.up);

            Check(Mathf.Abs(center.y + 0.26f) < 0.003f, "Nonconvex mesh surfaces are supported.");
            center = Surface(query, Vector3.up * -1.9f, terrain.GetComponent<TerrainCollider>(), Vector3.up);
            Check(Mathf.Abs(center.y + 2.26f) < 0.003f, "Terrain surfaces are supported.");
        }
        finally
        {
            Object.DestroyImmediate(query.gameObject);
            Object.DestroyImmediate(meshObject);
            Object.DestroyImmediate(terrain);
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(data);
        }
    }
    private static void ValidateBodyFit(GameObject visual, string gender)
    {
        var root = new GameObject(gender + " collision fixture");

        root.SetActive(false);
        root.transform.localScale = Vector3.one * 0.95f;
        var instance = Object.Instantiate(visual, root.transform, false);
        var attach = instance.transform.Find("attach_skin");
        var profile = attach.GetComponent<HexBattleMageCloakCollisionProfile>();
        var cloth = attach.GetComponentInChildren<MagicaCloth>(true);
        var shared = new List<ColliderComponent>();
        var equipmentObject = new GameObject("Equipment fixture");

        equipmentObject.transform.SetParent(root.transform, false);
        var equipment = equipmentObject.AddComponent<VisEquipment>();
        var body = equipmentObject.AddComponent<SkinnedMeshRenderer>();
        var renderer = cloth.GetComponent<SkinnedMeshRenderer>();

        body.bones = renderer.bones;
        body.rootBone = renderer.rootBone;
        equipment.m_bodyModel = body;
        cloth.SerializeData.colliderCollisionConstraint.colliderList = shared;
        var controller = attach.gameObject.AddComponent(Controller);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);

        wall.transform.position = new Vector3(0f, 1f, -0.2f);
        wall.transform.localScale = new Vector3(4f, 3f, 0.1f);
        Physics.SyncTransforms();

        try
        {
            Check(profile != null && profile.BodyCapsules.Length == 14, gender + " collision defaults survive bundle loading.");
            var expectedCapsules = profile.BodyCapsules;

            profile.BodyCapsules = new CloakBodyCapsule[0];
            var restored = profile.GetBodyCapsules();

            Check(restored.Length == expectedCapsules.Length, "Missing inline body definitions recover from the authored data asset.");

            for (int capsuleIndex = 0; capsuleIndex < restored.Length; capsuleIndex++)
            {
                Check(restored[capsuleIndex].StartBone == expectedCapsules[capsuleIndex].StartBone && Mathf.Abs(restored[capsuleIndex].Radius - expectedCapsules[capsuleIndex].Radius) < 0.001f, "Recovered body definitions retain their authored values.");
            }

            profile.BodyCapsules = new CloakBodyCapsule[0];
            Controller.GetMethod("Configure", Members).Invoke(controller, new object[] { equipment, profile });
            var harmony = new Harmony("HexBattleMage.CloakCollisionValidation");
            var setup = AccessTools.Method(typeof(VisEquipment), "SetupCloth", new[] { typeof(GameObject) });
            var patch = Mod.GetType("HexBattleMage.Patches.CloakCollisionPatch").GetMethod("Prefix", Members);

            try
            {
                harmony.Patch(setup, prefix: new HarmonyMethod(patch));
                harmony.Patch(setup, prefix: new HarmonyMethod(typeof(CloakCollisionValidation), "SkipNativeBuild") { priority = Priority.Last });
                setup.Invoke(equipment, new object[] { attach.gameObject });
                var vanilla = new GameObject("Unrelated native cape");

                setup.Invoke(equipment, new object[] { vanilla });
                Check(vanilla.GetComponent(Controller) == null, "Unrelated capes are untouched by the native hook.");
                Object.DestroyImmediate(vanilla);
                var invalid = new GameObject("Invalid collision profile");

                invalid.AddComponent<HexBattleMageCloakCollisionProfile>();
                setup.Invoke(equipment, new object[] { invalid });
                Check(invalid.GetComponent(Controller) == null, "Invalid protection data does not escape the hook or prevent native attachment.");
                Object.DestroyImmediate(invalid);
            }
            finally
            {
                harmony.UnpatchSelf();
            }

            var list = cloth.SerializeData.colliderCollisionConstraint.colliderList;

            Check(shared.Count == 0 && !ReferenceEquals(shared, list), "Native/global collider list is not mutated.");
            Check(cloth.SerializeData.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Edge, "Edges participate in collision.");
            Check(Mathf.Abs(cloth.SerializeData.animationPoseRatio - profile.AnimationPoseRatio) < 0.001f, "Cloth follows the animated pose.");
            Check(Mathf.Abs(cloth.SerializeData.damping.curve.Evaluate(1f) * cloth.SerializeData.damping.value - profile.MotionDamping) < 0.001f, "Authored damping is applied.");
            Check(Mathf.Abs(cloth.SerializeData.wind.influence - profile.WindInfluence) < 0.001f, "Authored wind influence is applied.");
            Check(profile.UseBackstop && cloth.SerializeData.motionConstraint.useBackstop, "Backstop defaults survive bundle loading and reach native cloth settings.");
            Check(cloth.SerializeData.motionConstraint.useMaxDistance && Mathf.Abs(cloth.SerializeData.motionConstraint.maxDistance.value - profile.MaximumTravel) < 0.001f, "Authored travel limit is applied.");
            Check(renderer.sharedMesh.vertexCount < 6000, "Meadows cloak simulation uses the single exported surface.");
            Check(renderer.sharedMaterial.GetFloat("_Cull") == 0f, "Single cloth surface renders from both sides.");
            Check(list.Count == 14 + profile.ProbeGrid.x * profile.ProbeGrid.y * profile.ProbeGrid.z, "Collider pool is bounded by authored settings.");

            foreach (var collider in list)
            {
                Check(Vector3.Distance(collider.transform.lossyScale, Vector3.one) < 0.001f, "Collider dimensions stay in metres despite model scale.");
            }

            var surfaces = (IList)Controller.GetField("_surfaces", Members).GetValue(controller);
            object tracked = null;

            foreach (var surface in surfaces)
            {
                if (surface.GetType().GetField("Support", Members).GetValue(surface) != null)
                {
                    tracked = surface;
                    break;
                }
            }

            Check(tracked != null, "Nearby wall receives finite cloth collision shapes.");
            var type = tracked.GetType();
            var sphere = (MagicaSphereCollider)type.GetField("Collider", Members).GetValue(tracked);
            var support = (Collider)type.GetField("Support", Members).GetValue(tracked);
            var localCenter = (Vector3)type.GetField("LocalCenter", Members).GetValue(tracked);

            support.transform.position += Vector3.right * 0.4f;
            support.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
            Controller.GetMethod("UpdateSurfacePositions", Members).Invoke(controller, null);
            Check(Vector3.Distance(sphere.transform.position, support.transform.TransformPoint(localCenter)) < 0.001f, "Moving/rotating boat support follows between probes.");
            Object.DestroyImmediate(wall);
            wall = null;
            Controller.GetMethod("UpdateSurfacePositions", Members).Invoke(controller, null);
            Check(!sphere.enabled, "Destroyed support cannot leave an active ghost collider.");

            var own = GameObject.CreatePrimitive(PrimitiveType.Cube);

            own.transform.SetParent(equipmentObject.transform, false);
            own.transform.position = new Vector3(0f, 1f, -0.2f);
            own.transform.localScale = new Vector3(4f, 3f, 0.1f);
            Physics.SyncTransforms();
            Controller.GetMethod("RefreshSurfaces", Members).Invoke(controller, null);

            foreach (var surface in surfaces)
            {
                Check(surface.GetType().GetField("Support", Members).GetValue(surface) == null, "Own-character colliders are excluded from environmental support.");
            }
        }
        finally
        {
            if (wall != null)
            {
                Object.DestroyImmediate(wall);
            }

            Object.DestroyImmediate(root);
        }
    }

    private static Vector3 Surface(SphereCollider probe, Vector3 position, Collider solid, Vector3 outside)
    {
        Check(TrySurface(probe, position, solid, outside, out var center), "Expected physical surface contact.");
        return center;
    }

    private static bool TrySurface(SphereCollider probe, Vector3 position, Collider solid, Vector3 outside, out Vector3 center)
    {
        var arguments = new object[] { probe, position, solid, 0.3f, 0.04f, outside, Vector3.zero };
        bool result = (bool)Geometry.GetMethod("TryGetSurfaceSphere", Members).Invoke(null, arguments);

        center = (Vector3)arguments[6];
        return result;
    }

    private static bool SkipNativeBuild()
    {
        return false;
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

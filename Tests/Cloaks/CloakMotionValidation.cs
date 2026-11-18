using System;
using System.IO;
using System.Reflection;
using MagicaCloth2;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class CloakMotionValidation
{
    public static void Run()
    {
        var attributes = new NativeArray<VertexAttribute>(1, Allocator.Temp);
        var depths = new NativeArray<float>(1, Allocator.Temp);
        var positions = new NativeArray<float3>(1, Allocator.Temp);
        var rotations = new NativeArray<quaternion>(1, Allocator.Temp);
        var next = new NativeArray<float3>(1, Allocator.Temp);
        var velocity = new NativeArray<float3>(1, Allocator.Temp);
        var friction = new NativeArray<float>(1, Allocator.Temp);
        var normals = new NativeArray<float3>(1, Allocator.Temp);

        try
        {
            attributes[0] = VertexAttribute.Move;
            depths[0] = 0.5f;
            var rotation = Quaternion.FromToRotation(Vector3.up, Vector3.back);

            rotations[0] = new quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
            var settings = new ClothSerializeData();

            settings.motionConstraint.useBackstop = true;
            settings.motionConstraint.backstopRadius = 0.75f;
            settings.motionConstraint.backstopDistance.value = 0f;
            settings.motionConstraint.useMaxDistance = true;
            settings.motionConstraint.maxDistance.value = 0.18f;
            settings.motionConstraint.stiffness = 1f;
            var parameters = settings.GetClothParameters();
            var team = new TeamManager.TeamData { particleChunk = new DataChunk(0, 1), proxyCommonChunk = new DataChunk(0, 1) };
            var solver = typeof(MotionConstraint).GetMethod("SolverConstraint", BindingFlags.Static | BindingFlags.NonPublic);

            next[0] = new float3(0f, 0f, 0.12f);
            var arguments = new object[] { new DataChunk(0, 1), team, parameters, attributes, depths, positions, rotations, next, velocity, friction, normals };

            solver.Invoke(null, arguments);
            Check(next[0].z < 0.001f, "Native backstop ejects a particle that crossed inward through the rest surface.");
            next[0] = new float3(0f, 0f, -0.12f);
            solver.Invoke(null, arguments);
            Check(math.distance(next[0], new float3(0f, 0f, -0.12f)) < 0.001f, "Outward flutter is retained.");
            next[0] = new float3(0f, 0f, -2f);
            solver.Invoke(null, arguments);
            Check(math.length(next[0]) <= 0.181f, "Native travel limit constrains a large displacement.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "cloak-motion-report.txt"), "PASS: actual installed MagicaCloth MotionConstraint solver, live NativeArrays, inward-crossing correction, preserved outward motion and bounded large displacement. This is a constraint-level test, not a complete Valheim animation session.");
        }
        finally
        {
            attributes.Dispose();
            depths.Dispose();
            positions.Dispose();
            rotations.Dispose();
            next.Dispose();
            velocity.Dispose();
            friction.Dispose();
            normals.Dispose();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

using UnityEngine;

namespace HexBattleMage.Core.Visuals
{
    internal static class CloakCollisionGeometry
    {
        internal static bool TryGetSurfaceSphere(SphereCollider probe, Vector3 position, Collider solid, float radius, float padding, Vector3 outsidePoint, out Vector3 center)
        {
            center = Vector3.zero;

            if (!Physics.ComputePenetration(probe, position, Quaternion.identity, solid, solid.transform.position, solid.transform.rotation, out var outward, out float distance))
            {
                return false;
            }

            if (outward.sqrMagnitude < 0.5f)
            {
                return false;
            }

            Vector3 surface = position + outward * (distance - probe.radius);

            if (Vector3.Dot(outsidePoint - surface, outward) < -padding)
            {
                return false;
            }

            center = surface - outward * (radius - padding);
            return true;
        }

        internal static Bounds TransformBounds(Bounds local, Matrix4x4 matrix)
        {
            Vector3 x = matrix.MultiplyVector(Vector3.right * local.extents.x);
            Vector3 y = matrix.MultiplyVector(Vector3.up * local.extents.y);
            Vector3 z = matrix.MultiplyVector(Vector3.forward * local.extents.z);
            Vector3 extent = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x), Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));

            return new Bounds(matrix.MultiplyPoint3x4(local.center), extent * 2f);
        }

        internal static Vector3 GridPosition(Bounds bounds, Vector3Int grid, int index)
        {
            int x = index % grid.x;
            int y = index / grid.x % grid.y;
            int z = index / (grid.x * grid.y);

            return new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, x / (float)(grid.x - 1)), Mathf.Lerp(bounds.min.y, bounds.max.y, y / (float)(grid.y - 1)), Mathf.Lerp(bounds.min.z, bounds.max.z, z / (float)(grid.z - 1)));
        }
    }
}

using System;
using UnityEngine;

namespace HexBattleMage.Core
{
    internal static class WorldDropPhysics
    {
        internal static void Configure(GameObject prefab, CollisionDetectionMode collisionDetection)
        {
            var body = prefab.GetComponent<Rigidbody>();

            if (body == null)
            {
                body = prefab.AddComponent<Rigidbody>();
            }

            bool hasSolidCollider = false;

            foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                if (collider.GetComponentInParent<Rigidbody>(true) != body)
                {
                    continue;
                }

                var current = collider.transform;
                bool active = true;

                while (current != prefab.transform)
                {
                    if (!current.gameObject.activeSelf)
                    {
                        active = false;
                        break;
                    }

                    current = current.parent;
                }

                if (active)
                {
                    hasSolidCollider = true;
                    break;
                }
            }

            if (!hasSolidCollider)
            {
                throw new InvalidOperationException("Dropped equipment needs an active solid collider: " + prefab.name);
            }

            body.isKinematic = false;
            body.useGravity = true;
            body.collisionDetectionMode = collisionDetection;
        }
    }
}

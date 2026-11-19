using System;
using System.Collections.Generic;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using MagicaCloth2;
using UnityEngine;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class CloakCollisionController : MonoBehaviour
    {
        private sealed class BodyCollider
        {
            internal CloakBodyCapsule Definition;
            internal Transform Start;
            internal Transform End;
            internal MagicaCapsuleCollider Collider;
            internal float Length;
            internal float Radius;
            internal float AppliedRadius;
            internal Vector3 FitStart;
            internal Vector3 FitSegment;
            internal float FitInverseSquaredLength;
            internal float FitLimit;

        }

        private sealed class SurfaceCollider
        {
            internal MagicaSphereCollider Collider;
            internal Collider Support;
            internal Vector3 LocalCenter;
        }

        private static readonly AccessTools.FieldRef<VisEquipment, List<GameObject>> ReadChestInstances = AccessTools.FieldRefAccess<VisEquipment, List<GameObject>>("m_chestItemInstances");
        private VisEquipment _equipment;
        private List<GameObject> _chestInstances;
        private Mesh _armorMesh;
        private readonly List<Vector3> _armorVertices = new List<Vector3>();
        private const float MinimumLength = 0.001f;
        private const int MaximumProbeCount = 192;
        private const float QueryParkingDepth = -100000f;
        private readonly List<BodyCollider> _body = new List<BodyCollider>();
        private readonly List<SurfaceCollider> _surfaces = new List<SurfaceCollider>();
        private readonly List<ColliderComponent> _colliders = new List<ColliderComponent>();
        private HexBattleMageCloakCollisionProfile _profile;
        private Transform _character;
        private Transform _helper;
        private Transform _hips;
        private SkinnedMeshRenderer _renderer;
        private SphereCollider _probe;
        private Mesh _bakedMesh;
        private Collider[] _nearby;
        private Vector3Int _grid;
        private float _nextProbe;
        private float _nextArmorFit;

        internal void Configure(VisEquipment equipment, HexBattleMageCloakCollisionProfile profile)
        {
            if (_profile != null)
            {
                return;
            }

            _equipment = equipment;
            _armorMesh = new Mesh { name = "Cloak armor fitting mesh" };
            _bakedMesh = new Mesh { name = "Cloak collision probe mesh" };
            _profile = profile;
            _renderer = GetComponentInChildren<MagicaCloth>(true)?.GetComponent<SkinnedMeshRenderer>();

            if (_renderer == null)
            {
                throw new InvalidOperationException("Cloak collision needs its skinned cloth renderer.");
            }

            var character = equipment.GetComponentInParent<Character>();

            _character = character != null ? character.transform : equipment.transform;
            _helper = new GameObject("HexBattleMage Cloak Collision").transform;
            _helper.SetParent(transform, false);
            NormalizeScale();

            var bones = new Dictionary<string, Transform>(StringComparer.Ordinal);

            foreach (var bone in equipment.m_bodyModel.bones)
            {
                if (bone != null)
                {
                    bones[bone.name] = bone;
                }
            }

            if (!bones.TryGetValue("Hips", out _hips))
            {
                throw new InvalidOperationException("Cloak collision needs the character hip bone.");
            }

            if (profile.ProtectBody)
            {
                foreach (var definition in profile.GetBodyCapsules())
                {
                    if (!bones.TryGetValue(definition.StartBone, out var start))
                    {
                        throw new InvalidOperationException("Missing cloak collision bone: " + definition.StartBone);
                    }

                    if (!bones.TryGetValue(definition.EndBone, out var end))
                    {
                        throw new InvalidOperationException("Missing cloak collision bone: " + definition.EndBone);
                    }

                    var capsule = CreateHelper("Body " + definition.StartBone).AddComponent<MagicaCapsuleCollider>();

                    capsule.direction = MagicaCapsuleCollider.Direction.Y;
                    capsule.alignedOnCenter = true;
                    var body = new BodyCollider { Definition = definition, Start = start, End = end, Collider = capsule, Radius = definition.Radius };


                    _body.Add(body);
                    _colliders.Add(capsule);
                }
            }

            if (profile.ProtectWorld)
            {
                _grid = new Vector3Int(Mathf.Clamp(profile.ProbeGrid.x, 2, 8), Mathf.Clamp(profile.ProbeGrid.y, 2, 10), Mathf.Clamp(profile.ProbeGrid.z, 2, 5));
                int count = _grid.x * _grid.y * _grid.z;

                if (count > MaximumProbeCount)
                {
                    throw new InvalidOperationException("Cloak collision grid exceeds " + MaximumProbeCount + " probes.");
                }

                _nearby = new Collider[Mathf.Clamp(profile.MaximumNearbySolids, 8, 256)];
                // The penetration API needs a live shape. Park it outside the world and
                // outside the player Rigidbody; query poses are supplied explicitly.
                var query = new GameObject("HexBattleMage cloak surface query");

                query.layer = LayerMask.NameToLayer("Ignore Raycast");
                query.hideFlags = HideFlags.DontSave;
                query.transform.position = Vector3.up * QueryParkingDepth;
                _probe = query.AddComponent<SphereCollider>();
                _probe.radius = Mathf.Max(0.05f, profile.ProbeRadius);
                _probe.isTrigger = true;
                Physics.SyncTransforms();

                for (int index = 0; index < count; index++)
                {
                    var sphere = CreateHelper("Surface " + index).AddComponent<MagicaSphereCollider>();

                    sphere.SetSize(Mathf.Max(0.05f, profile.SurfaceSphereRadius));
                    sphere.enabled = false;
                    _surfaces.Add(new SurfaceCollider { Collider = sphere });
                    _colliders.Add(sphere);
                }
            }

            foreach (var cloth in GetComponentsInChildren<MagicaCloth>(true))
            {
                var collision = cloth.SerializeData.colliderCollisionConstraint;

                // Own the list: native capes may otherwise share the character's collider list.
                collision.colliderList = new List<ColliderComponent>(collision.colliderList);
                collision.colliderList.AddRange(_colliders);
                CloakMotionSettings.Apply(profile, cloth.SerializeData);
                cloth.OnBuildComplete += OnClothBuilt;
            }

            FitChestArmor();
            UpdateBody();
            RefreshSurfaces();
            Plugin.Log?.LogInfo($"Cloak protection configured: body shapes={_body.Count}; surface probes={_surfaces.Count}; animation follow={profile.AnimationPoseRatio}; armor padding={profile.ArmorPadding}m; armor fit interval={profile.ArmorFitInterval}s; neck anchor radius={profile.NeckAnchorRadius}m.");
        }

        private GameObject CreateHelper(string name)
        {
            var child = new GameObject(name);

            child.layer = LayerMask.NameToLayer("Ignore Raycast");
            child.transform.SetParent(_helper, false);
            return child;
        }

        private void NormalizeScale()
        {
            Vector3 scale = transform.lossyScale;

            _helper.localScale = new Vector3(1f / Mathf.Max(Mathf.Abs(scale.x), MinimumLength), 1f / Mathf.Max(Mathf.Abs(scale.y), MinimumLength), 1f / Mathf.Max(Mathf.Abs(scale.z), MinimumLength));
        }

        private void LateUpdate()
        {
            if (_profile == null)
            {
                return;
            }

            NormalizeScale();

            if (_equipment != null && !ReferenceEquals(_chestInstances, ReadChestInstances(_equipment)))
            {
                FitChestArmor();
            }

            UpdateBody();

            if (!_renderer.isVisible)
            {
                foreach (var surface in _surfaces)
                {
                    surface.Collider.enabled = false;
                }

                _nextProbe = 0f;
                return;
            }

            if (Time.unscaledTime >= _nextArmorFit)
            {
                FitChestArmorPose(false);
                UpdateBody();
                _nextArmorFit = Time.unscaledTime + Mathf.Max(0.02f, _profile.ArmorFitInterval);
            }

            if (Time.unscaledTime >= _nextProbe)
            {
                RefreshSurfaces();
                _nextProbe = Time.unscaledTime + Mathf.Max(0.02f, _profile.ProbeInterval);
            }

            UpdateSurfacePositions();
        }

        private void UpdateSurfacePositions()
        {
            foreach (var surface in _surfaces)
            {
                if (surface.Support == null)
                {
                    surface.Collider.enabled = false;
                    continue;
                }

                if (!surface.Support.enabled || !surface.Support.gameObject.activeInHierarchy)
                {
                    surface.Collider.enabled = false;
                    continue;
                }

                surface.Collider.transform.position = surface.Support.transform.TransformPoint(surface.LocalCenter);
            }
        }

        private void FitChestArmor()
        {
            _chestInstances = ReadChestInstances(_equipment);
            FitChestArmorPose(true);
            _nextArmorFit = Time.unscaledTime + Mathf.Max(0.02f, _profile.ArmorFitInterval);
        }

        private void FitChestArmorPose(bool reset)
        {
            // Retain the largest pose envelope until armor changes, avoiding pulsing cloth.
            if (reset || !_profile.FitEquippedChestArmor || _chestInstances == null)
            {
                foreach (var body in _body)
                {
                    body.Radius = body.Definition.Radius;
                }
            }

            if (!_profile.FitEquippedChestArmor)
            {
                return;
            }

            if (_chestInstances == null)
            {
                return;
            }

            foreach (var body in _body)
            {
                body.FitStart = body.Start.position + CharacterOffset(body.Definition.StartOffset);
                body.FitSegment = body.End.position + CharacterOffset(body.Definition.EndOffset) - body.FitStart;
                body.FitInverseSquaredLength = 1f / Mathf.Max(body.FitSegment.sqrMagnitude, MinimumLength);
                body.FitLimit = body.Definition.Radius + Mathf.Max(0f, _profile.MaximumArmorExpansion);
            }

            float padding = Mathf.Max(0f, _profile.ArmorPadding);

            foreach (var instance in _chestInstances)
            {
                if (instance == null)
                {
                    continue;
                }

                foreach (var armor in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (armor.sharedMesh == null || !armor.enabled)
                    {
                        continue;
                    }

                    armor.BakeMesh(_armorMesh);
                    _armorMesh.GetVertices(_armorVertices);
                    var matrix = armor.localToWorldMatrix;

                    for (int index = 0; index < _armorVertices.Count; index++)
                    {
                        Vector3 position = matrix.MultiplyPoint3x4(_armorVertices[index]);

                        foreach (var body in _body)
                        {
                            float along = Mathf.Clamp01(Vector3.Dot(position - body.FitStart, body.FitSegment) * body.FitInverseSquaredLength);
                            float distance = Vector3.Distance(position, body.FitStart + body.FitSegment * along);

                            if (distance > body.FitLimit)
                            {
                                continue;
                            }

                            body.Radius = Mathf.Max(body.Radius, Mathf.Min(body.FitLimit, distance + padding));
                        }
                    }
                }
            }
        }

        private void UpdateBody()
        {
            foreach (var body in _body)
            {
                if (body.Start == null || body.End == null)
                {
                    body.Collider.enabled = false;
                    continue;
                }

                body.Collider.enabled = true;
                Vector3 start = body.Start.position + CharacterOffset(body.Definition.StartOffset);
                Vector3 end = body.End.position + CharacterOffset(body.Definition.EndOffset);
                Vector3 direction = end - start;
                float radius = Mathf.Max(0.01f, body.Radius);
                float length = direction.magnitude + radius * 2f;

                body.Collider.transform.SetPositionAndRotation((start + end) * 0.5f, direction.sqrMagnitude > MinimumLength * MinimumLength ? Quaternion.FromToRotation(Vector3.up, direction) : Quaternion.identity);

                if (Mathf.Abs(length - body.Length) > MinimumLength || Mathf.Abs(radius - body.AppliedRadius) > MinimumLength)
                {
                    body.Collider.SetSize(radius, radius, length);
                    body.Collider.UpdateParameters();
                    body.Length = length;
                    body.AppliedRadius = radius;
                }
            }
        }

        private Vector3 CharacterOffset(Vector3 offset)
        {
            return _character.right * offset.x + _character.up * offset.y + _character.forward * offset.z;
        }

        private void RefreshSurfaces()
        {
            if (!_profile.ProtectWorld)
            {
                return;
            }

            _renderer.BakeMesh(_bakedMesh);
            _bakedMesh.RecalculateBounds();

            Bounds bounds = CloakCollisionGeometry.TransformBounds(_bakedMesh.bounds, _renderer.localToWorldMatrix);

            bounds.Expand(Mathf.Max(0f, _profile.SearchPadding) * 2f);

            float probeRadius = Mathf.Max(0.05f, _profile.ProbeRadius);
            float sphereRadius = Mathf.Max(0.05f, _profile.SurfaceSphereRadius);
            float padding = Mathf.Clamp(_profile.SurfacePadding, 0.001f, sphereRadius * 0.5f);
            Vector3 extent = bounds.extents + Vector3.one * probeRadius;
            int count = Physics.OverlapBoxNonAlloc(bounds.center, extent, _nearby, Quaternion.identity, _profile.SolidLayers, QueryTriggerInteraction.Ignore);

            for (int index = 0; index < _surfaces.Count; index++)
            {
                var surface = _surfaces[index];
                Vector3 position = CloakCollisionGeometry.GridPosition(bounds, _grid, index);
                float nearestDistance = float.PositiveInfinity;
                Collider support = null;
                Vector3 center = Vector3.zero;

                for (int candidateIndex = 0; candidateIndex < count; candidateIndex++)
                {
                    var candidate = _nearby[candidateIndex];

                    if (candidate == null)
                    {
                        continue;
                    }

                    if (candidate.transform.IsChildOf(_character))
                    {
                        continue;
                    }

                    if (candidate.bounds.SqrDistance(position) > probeRadius * probeRadius)
                    {
                        continue;
                    }

                    if (!CloakCollisionGeometry.TryGetSurfaceSphere(_probe, position, candidate, sphereRadius, padding, _hips.position, out var candidateCenter))
                    {
                        continue;
                    }

                    float distance = Mathf.Abs(Vector3.Distance(position, candidateCenter) - (sphereRadius - padding));

                    if (distance >= nearestDistance)
                    {
                        continue;
                    }

                    nearestDistance = distance;
                    support = candidate;
                    center = candidateCenter;
                }

                if (surface.Support != support)
                {
                    surface.Collider.enabled = false;
                }

                surface.Support = support;

                if (support == null)
                {
                    surface.Collider.enabled = false;
                    continue;
                }

                surface.LocalCenter = support.transform.InverseTransformPoint(center);
                surface.Collider.transform.position = center;
                surface.Collider.enabled = true;
            }
        }

        private void OnClothBuilt(MagicaCloth cloth, bool success)
        {
            Plugin.Log?.LogInfo($"Cloak cloth build: success={success}; team={cloth.Process.TeamId}; colliders={cloth.SerializeData.colliderCollisionConstraint.colliderList.Count}; backstop={cloth.SerializeData.motionConstraint.useBackstop}; max travel={cloth.SerializeData.motionConstraint.maxDistance.value}m.");
        }

        private void OnDestroy()
        {
            if (_probe != null)
            {
                Destroy(_probe.gameObject);
            }

            if (_armorMesh != null)
            {
                Destroy(_armorMesh);
            }

            if (_bakedMesh != null)
            {
                Destroy(_bakedMesh);
            }
        }

        private void OnDisable()
        {
            foreach (var collider in _colliders)
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }
    }
}

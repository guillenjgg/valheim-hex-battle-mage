using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    [Serializable]
    public sealed class CloakBodyCapsule
    {
        public string StartBone;
        public string EndBone;
        [Tooltip("Offsets in metres along the character's right, up and forward axes.")]
        public Vector3 StartOffset;
        public Vector3 EndOffset;
        [Min(0.01f)] public float Radius = 0.1f;
    }

    public sealed class HexBattleMageCloakCollisionProfile : MonoBehaviour
    {
        public bool ProtectBody = true;
        public CloakBodyCapsule[] BodyCapsules = new CloakBodyCapsule[0];
        public TextAsset BakedBodyCapsules;
        public bool FitEquippedChestArmor = true;
        [Min(0f)] public float ArmorPadding = 0.025f;
        [Min(0f)] public float MaximumArmorExpansion = 0.12f;
        [Header("Cloth anchors; other fabric responds to collision")]
        public int AnchorLayoutVersion;
        public float NeckAnchorHeight = 1.59f;
        public float NeckAnchorRadius = 0.21f;
        public float ClaspAnchorRadius = 0.045f;
        [Header("Follow the animated player")]
        [Range(0f, 1f)] public float AnimationPoseRatio = 0.1f;
        [Range(0f, 1f)] public float MotionDamping = 0.1f;
        [Range(0f, 1f)] public float WindInfluence = 0.15f;
        [Range(0f, 1f)] public float WindDepthWeight = 1f;
        [Range(0f, 1f)] public float WindTurbulence = 0.2f;
        [Range(0f, 1f)] public float UpperWrapDamping = 0.8f;
        [Range(0f, 1f)] public float UpperWrapRestoration = 0.9f;
        [Range(0f, 1f)] public float DrapeRestoration = 0.2f;
        [Range(0.01f, 0.99f)] public float UpperWrapDepth = 0.35f;
        [Header("Nearby solid surfaces; metres and seconds")]
        public bool ProtectWorld = true;
        [Min(0.02f)] public float ProbeInterval = 0.05f;
        [Min(0.01f)] public float SurfacePadding = 0.04f;
        [Min(0.05f)] public float ProbeRadius = 0.18f;
        [Min(0.05f)] public float SurfaceSphereRadius = 0.3f;
        [Min(0f)] public float SearchPadding = 0.08f;
        public Vector3Int ProbeGrid = new Vector3Int(5, 7, 3);
        [Range(8, 256)] public int MaximumNearbySolids = 64;
        public LayerMask SolidLayers = Physics.DefaultRaycastLayers;
        [Header("Keep cloth outside its skinned rest surface")]
        public bool UseBackstop = true;
        [Range(0.01f, 10f)] public float BackstopRadius = 0.75f;
        [Range(0f, 1f)] public float BackstopDistance;
        [Range(0.01f, 5f)] public float MaximumTravel = 0.18f;
        [Header("Native cloth collision")]
        [Min(0.001f)] public float ClothThickness = 0.035f;
        [Range(0f, 1f)] public float Friction = 0.2f;

        [DataContract]
        private sealed class BodyData
        {
            [DataMember] public CapsuleData[] Capsules;
        }

        [DataContract]
        private sealed class CapsuleData
        {
            [DataMember] public string StartBone;
            [DataMember] public string EndBone;
            [DataMember] public float Radius;
            [DataMember] public float StartX;
            [DataMember] public float StartY;
            [DataMember] public float StartZ;
            [DataMember] public float EndX;
            [DataMember] public float EndY;
            [DataMember] public float EndZ;
        }

        public string GetBakedBodyData()
        {
            var capsules = new CapsuleData[BodyCapsules.Length];

            for (int index = 0; index < capsules.Length; index++)
            {
                var capsule = BodyCapsules[index];

                capsules[index] = new CapsuleData { StartBone = capsule.StartBone, EndBone = capsule.EndBone, Radius = capsule.Radius, StartX = capsule.StartOffset.x, StartY = capsule.StartOffset.y, StartZ = capsule.StartOffset.z, EndX = capsule.EndOffset.x, EndY = capsule.EndOffset.y, EndZ = capsule.EndOffset.z };
            }

            var serializer = new DataContractJsonSerializer(typeof(BodyData));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, new BodyData { Capsules = capsules });
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public CloakBodyCapsule[] GetBodyCapsules()
        {
            if (BodyCapsules != null && BodyCapsules.Length > 0)
            {
                return BodyCapsules;
            }

            if (BakedBodyCapsules == null)
            {
                throw new InvalidOperationException("Missing authored body collision data on " + gameObject.name);
            }

            var serializer = new DataContractJsonSerializer(typeof(BodyData));
            BodyData data;

            using (var stream = new MemoryStream(BakedBodyCapsules.bytes))
            {
                data = (BodyData)serializer.ReadObject(stream);
            }

            if (data == null || data.Capsules == null || data.Capsules.Length == 0)
            {
                throw new InvalidOperationException("Invalid authored body collision data on " + gameObject.name);
            }

            var capsules = new CloakBodyCapsule[data.Capsules.Length];

            for (int index = 0; index < capsules.Length; index++)
            {
                var capsule = data.Capsules[index];

                capsules[index] = new CloakBodyCapsule { StartBone = capsule.StartBone, EndBone = capsule.EndBone, Radius = capsule.Radius, StartOffset = new Vector3(capsule.StartX, capsule.StartY, capsule.StartZ), EndOffset = new Vector3(capsule.EndX, capsule.EndY, capsule.EndZ) };
            }

            BodyCapsules = capsules;
            return BodyCapsules;
        }
    }
}

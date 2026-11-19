using System;
using System.IO;
using HexBattleMage.Core.Managers;
using MagicaCloth2;
using UnityEditor;
using UnityEngine;

public static class CloakCollisionAssetPreparation
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01/Visuals/HexBattleMage_Cloak_Meadows_01_";

    public static void PrepareAll()
    {
        foreach (string gender in new[] { "Male", "Female" })
        {
            string path = Root + gender + ".prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var attach = prefab.transform.Find("attach_skin");
                var profile = attach.GetComponent<HexBattleMageCloakCollisionProfile>();

                if (profile == null)
                {
                    profile = attach.gameObject.AddComponent<HexBattleMageCloakCollisionProfile>();
                    profile.BodyCapsules = CreateBodyCapsules(gender == "Female");
                }

                if (profile.AnchorLayoutVersion < 1)
                {
                    ApplyNeckAndClaspAnchors(attach, profile);
                    profile.AnchorLayoutVersion = 1;
                }

                string dataPath = "Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01/Collision_" + gender + ".json";

                File.WriteAllText(dataPath, profile.GetBakedBodyData());
                AssetDatabase.ImportAsset(dataPath);
                profile.BakedBodyCapsules = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);

                foreach (var cloth in attach.GetComponentsInChildren<MagicaCloth>())
                {
                    CloakMotionSettings.Apply(profile, cloth.SerializeData);
                }

                if (profile.BodyCapsules.Length == 0)
                {
                    throw new InvalidOperationException("Cloak body protection needs authored collision shapes.");
                }

                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }
    }

    private static void ApplyNeckAndClaspAnchors(Transform attach, HexBattleMageCloakCollisionProfile profile)
    {
        var clasp = attach.Find("Clasp").GetComponent<SkinnedMeshRenderer>();
        Vector3 claspCenter = clasp.sharedMesh.bounds.center;

        foreach (var cloth in attach.GetComponentsInChildren<MagicaCloth>())
        {
            var vertices = cloth.GetComponent<SkinnedMeshRenderer>().sharedMesh.vertices;
            var selection = cloth.GetSerializeData2().selectionData;
            int anchors = 0;

            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 position = vertices[index];
                bool neck = position.y >= profile.NeckAnchorHeight && new Vector2(position.x, position.z).magnitude <= profile.NeckAnchorRadius;
                bool nearClasp = Vector3.Distance(position, claspCenter) <= profile.ClaspAnchorRadius;

                selection.attributes[index] = neck || nearClasp ? VertexAttribute.Fixed : VertexAttribute.Move;

                if (neck || nearClasp)
                {
                    anchors++;
                }
            }

            if (anchors == 0)
            {
                throw new InvalidOperationException("Cloak needs neck/clasp anchor vertices.");
            }
        }
    }
    private static CloakBodyCapsule[] CreateBodyCapsules(bool female)
    {
        return new[]
        {
            Capsule("Hips", "Spine1", female ? 0.19f : 0.18f),
            Capsule("Spine1", "Spine2", female ? 0.20f : 0.19f),
            Capsule("LeftUpLeg", "RightUpLeg", female ? 0.18f : 0.16f),
            Capsule("Neck", "Head", 0.115f),
            Capsule("LeftArm", "LeftForeArm", 0.10f),
            Capsule("RightArm", "RightForeArm", 0.10f),
            Capsule("LeftForeArm", "LeftHand", 0.08f),
            Capsule("RightForeArm", "RightHand", 0.08f),
            Capsule("LeftUpLeg", "LeftLeg", female ? 0.14f : 0.13f),
            Capsule("RightUpLeg", "RightLeg", female ? 0.14f : 0.13f),
            Capsule("LeftLeg", "LeftFoot", 0.105f),
            Capsule("RightLeg", "RightFoot", 0.105f),
            Capsule("LeftFoot", "LeftToeBase", 0.09f),
            Capsule("RightFoot", "RightToeBase", 0.09f)
        };
    }

    private static CloakBodyCapsule Capsule(string start, string end, float radius)
    {
        return new CloakBodyCapsule { StartBone = start, EndBone = end, Radius = radius };
    }
}

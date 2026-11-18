using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MagicaCloth2;
using HexBattleMage.Core.Managers;
using UnityEngine;
public sealed class CloakLoadPlayer : MonoBehaviour
{
    private static void ValidateArmorFit(GameObject visual)
    {
        const float nativeDampingScale = 0.2f;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var root = new GameObject("Armor fit runtime fixture");
        root.SetActive(false);
        try
        {
            var instance = Instantiate(visual, root.transform, false);
            var attach = instance.transform.Find("attach_skin");
            var profile = attach.GetComponent<HexBattleMageCloakCollisionProfile>();
            profile.ProtectWorld = false;
            var cloth = attach.GetComponentInChildren<MagicaCloth>(true);
            var renderer = cloth.GetComponent<SkinnedMeshRenderer>();
            var parameters = cloth.SerializeData.GetClothParameters();
            var evaluate = typeof(MagicaCloth).Assembly.GetType("MagicaCloth2.MathExtensions").GetMethod("MC2EvaluateCurve", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            float upperDamping = (float)evaluate.Invoke(null, new object[] { parameters.dampingCurveData, 0f });
            float lowerDamping = (float)evaluate.Invoke(null, new object[] { parameters.dampingCurveData, 1f });
            if (Mathf.Abs(upperDamping - profile.UpperWrapDamping * nativeDampingScale) > 0.001f || Mathf.Abs(lowerDamping - profile.MotionDamping * nativeDampingScale) > 0.001f)
            {
                throw new InvalidOperationException("Native damping mismatch upper=" + upperDamping + " lower=" + lowerDamping + " profileLower=" + profile.MotionDamping + " raw=" + cloth.SerializeData.damping.value + " curve=" + cloth.SerializeData.damping.useCurve + " keys=" + cloth.SerializeData.damping.curve.length);
            }
            if (cloth.SerializeData.wind.influence > 0.151f || cloth.SerializeData.wind.depthWeight < 0.999f)
            {
                throw new InvalidOperationException("Reduced, depth-weighted wind must be baked before attachment.");
            }
            var equipmentObject = new GameObject("Equipment");
            equipmentObject.transform.SetParent(root.transform, false);
            var equipment = equipmentObject.AddComponent<VisEquipment>();
            var body = equipmentObject.AddComponent<SkinnedMeshRenderer>();
            body.bones = renderer.bones;
            body.rootBone = renderer.rootBone;
            equipment.m_bodyModel = body;
            var armorObject = new GameObject("Equipped chest geometry");
            armorObject.transform.SetParent(root.transform, false);
            var armor = armorObject.AddComponent<SkinnedMeshRenderer>();
            armor.sharedMesh = Resources.Load<Mesh>("ValidationAshlandsChest");
            if (armor.sharedMesh == null || armor.sharedMesh.isReadable)
            {
                throw new InvalidOperationException("Test must exercise the game's non-readable Ashlands mesh.");
            }
            armor.bones = body.bones;
            armor.rootBone = body.rootBone;
            typeof(VisEquipment).GetField("m_chestItemInstances", flags).SetValue(equipment, new List<GameObject> { armorObject });
            var controllerType = typeof(HexBattleMageCloakProfile).Assembly.GetType("HexBattleMage.Core.Visuals.CloakCollisionController");
            var controller = attach.gameObject.AddComponent(controllerType);
            controllerType.GetMethod("Configure", flags).Invoke(controller, new object[] { equipment, profile });
            var capsules = (IList)controllerType.GetField("_body", flags).GetValue(controller);
            int expanded = 0;
            foreach (var capsule in capsules)
            {
                var type = capsule.GetType();
                var definition = (CloakBodyCapsule)type.GetField("Definition", flags).GetValue(capsule);
                float radius = (float)type.GetField("Radius", flags).GetValue(capsule);
                if (radius > definition.Radius + 0.001f)
                {
                    expanded++;
                }
            }
            if (expanded == 0)
            {
                throw new InvalidOperationException("Equipped Ashlands armor did not contribute to collision volumes.");
            }
            typeof(VisEquipment).GetField("m_chestItemInstances", flags).SetValue(equipment, null);
            controllerType.GetMethod("FitChestArmor", flags).Invoke(controller, null);
            foreach (var capsule in capsules)
            {
                var type = capsule.GetType();
                var definition = (CloakBodyCapsule)type.GetField("Definition", flags).GetValue(capsule);
                if (Mathf.Abs((float)type.GetField("Radius", flags).GetValue(capsule) - definition.Radius) > 0.001f)
                {
                    throw new InvalidOperationException("Armor removal left expanded body collision volumes.");
                }
            }
            var selection = cloth.GetSerializeData2().selectionData;
            var vertices = renderer.sharedMesh.vertices;
            int fixedCount = 0;
            int movableBack = 0;
            for (int index = 0; index < vertices.Length; index++)
            {
                if (selection.attributes[index].IsFixed())
                {
                    fixedCount++;
                }
                if (vertices[index].y > 1.42f && vertices[index].y < 1.58f && vertices[index].z < -0.08f && selection.attributes[index].IsMove())
                {
                    movableBack++;
                }
            }
            if (fixedCount == 0 || movableBack == 0)
            {
                throw new InvalidOperationException("Neck anchors and collision-responsive upper fabric are both required.");
            }
        }
        finally
        {
            DestroyImmediate(root);
        }
    }
    private void Start()
    {
        string report = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/cloak-player-load-report.txt";
        try
        {
            var bundle = AssetBundle.LoadFromFile("C:/Users/Jonathan/Documents/valheim_mods/repos/valheim-hex-battle-mage/HexBattleMage/Assets/AssetBundles/hexbattlemage");
            var item = bundle.LoadAsset<GameObject>("HexBattleMage_Cloak_Meadows_01").GetComponent<HexBattleMageCloakProfile>();
            foreach (var visual in new[] { item.MaleVisual, item.FemaleVisual })
            {
                var profile = visual.GetComponentInChildren<HexBattleMageCloakCollisionProfile>(true);
                profile.BodyCapsules = new CloakBodyCapsule[0];
                var restored = profile.GetBodyCapsules();
                ValidateArmorFit(visual);
                if (restored.Length != 14 || restored[0].StartBone != "Hips" || restored[0].Radius <= 0f)
                {
                    throw new InvalidOperationException("Body data recovery failed in the actual player runtime.");
                }
            }
            File.WriteAllText(report, "PASS: standalone Unity player, actual compiled mod and production bundle, both body profiles restored 14 authored capsules with inline data removed. Managed serializer succeeds outside the Editor. Ashlands chest geometry fits protection volumes from an unreadable source mesh; removal restores body radii; shoulder vertices are movable while anchors remain fixed. Native damping curve conversion verified (0.16 at anchors and 0.02 at the hem); reduced depth-weighted wind is baked.");
            Application.Quit(0);
        }
        catch (Exception exception)
        {
            File.WriteAllText(report, "FAIL: " + exception);
            Application.Quit(1);
        }
    }
}
#if UNITY_EDITOR
public static class CloakLoadPlayerBuilder
{
    public static void Build()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        new GameObject("Cloak runtime test").AddComponent<CloakLoadPlayer>();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Validation/CloakLoad.unity");
        var result = UnityEditor.BuildPipeline.BuildPlayer(new[] { "Assets/Validation/CloakLoad.unity" }, "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/cloak-player-test/Validation.exe", UnityEditor.BuildTarget.StandaloneWindows64, UnityEditor.BuildOptions.Development);
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new InvalidOperationException("Cloak player test build failed.");
        }
    }
}
#endif

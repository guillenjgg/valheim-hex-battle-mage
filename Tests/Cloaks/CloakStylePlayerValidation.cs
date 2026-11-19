using System;
using System.IO;
using System.Reflection;
using System.Linq;
using HexBattleMage.Core.Managers;
using UnityEngine;

public sealed class CloakStylePlayerValidation : MonoBehaviour
{
    private void Start()
    {
        string path = "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/cloak-style-player-report.txt";

        try
        {
            var helper = typeof(MageArmorStyleProfile).Assembly.GetType("HexBattleMage.Assets.AssetBundleHelper");
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            if (!(bool)helper.GetMethod("LoadAssets", flags).Invoke(null, null))
            {
                throw new InvalidOperationException("Embedded bundle load failed.");
            }

            var bundle = (AssetBundle)helper.GetProperty("Bundle", flags).GetValue(null);
            var root = bundle.LoadAllAssets<GameObject>().Single(prefab => prefab.name == "HexBattleMage_Cloak_Meadows_01");
            var styles = root.GetComponent<MageArmorStyleProfile>();

            if (styles == null || styles.Styles.Length != 3)
            {
                throw new InvalidOperationException("Standalone loaded style profile=" + (styles != null) + "; inline style count=" + (styles == null ? -1 : styles.Styles.Length));
            }

            styles.Styles = new MageArmorStyle[0];
            styles.BakedStyles = bundle.LoadAsset<TextAsset>("Styles_HexBattleMage_Cloak_Meadows_01");
            styles.ApplyBakedDefaults(bundle);

            if (styles.Styles.Length != 3)
            {
                throw new InvalidOperationException("Baked style recovery failed with inline data cleared.");
            }

            foreach (var style in styles.Styles)
            {
                if (style.Material == null || style.Icon == null || string.IsNullOrEmpty(style.DisplayName))
                {
                    throw new InvalidOperationException("Standalone style reference missing.");
                }
            }

            var host = new GameObject("Style clone runtime test");

            host.SetActive(false);
            var source = new GameObject(styles.RendererName);

            source.transform.SetParent(host.transform, false);
            source.AddComponent<MeshRenderer>().sharedMaterial = styles.Styles[0].Material;
            var visualType = typeof(MageArmorStyleProfile).Assembly.GetType("HexBattleMage.Core.Visuals.MageArmorStyleVisual");
            var visual = source.AddComponent(visualType);
            const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            visualType.GetMethod("Configure", instanceFlags).Invoke(visual, new object[] { styles, false });
            var clone = Instantiate(source, host.transform, false);
            var copiedVisual = clone.GetComponent(visualType);

            clone.name = styles.RendererName;
            visualType.GetMethod("Setup", instanceFlags).Invoke(copiedVisual, new object[] { 1 });

            if (clone.GetComponent<Renderer>().sharedMaterial != styles.Styles[1].Material)
            {
                throw new InvalidOperationException("Cloned visual lost the configured style reference in standalone runtime.");
            }

            ValidateHelmet(bundle, visualType, instanceFlags);
            Destroy(host);
            var cloak = root.GetComponent<HexBattleMageCloakProfile>();
            var eitr = root.GetComponent<MageEitrProfile>();
            var upgrades = root.GetComponent<MageUpgradeProfile>();

            if (cloak.MaximumQuality != 3 || cloak.ArmorPerQuality != 1f || eitr.GetMaximumEitr(3) != 7f || upgrades == null || upgrades.RequiredStationLevel != 3 || !upgrades.FixedResourceCosts)
            {
                throw new InvalidOperationException("Standalone cloak upgrade defaults missing.");
            }

            File.WriteAllText(path, "PASS: embedded style recovery and cloned visual material application pass in standalone runtime; cloak upgrade defaults retained; four helmet styles recover from named baked data and apply on both cloned body fits and the drop.");
            Application.Quit(0);
        }
        catch (Exception exception)
        {
            File.WriteAllText(path, "FAIL: " + exception);
            Application.Quit(1);
        }
    }
    private static void ValidateHelmet(AssetBundle bundle, Type visualType, BindingFlags flags)
    {
        var root = bundle.LoadAsset<GameObject>("HexBattleMage_Helmet_Meadows_01_Definition");
        var styles = root.GetComponent<MageArmorStyleProfile>();

        styles.Styles = new MageArmorStyle[0];
        styles.BakedStyles = bundle.LoadAsset<TextAsset>("Styles_HexBattleMage_Helmet_Meadows_01_Definition");
        styles.ApplyBakedDefaults(bundle);

        if (styles.Styles.Length != 4)
        {
            throw new InvalidOperationException("Standalone helmet style recovery failed.");
        }

        var profile = root.GetComponent<HexBattleMageArmorProfile>();
        var host = new GameObject("Standalone helmet colors");

        host.SetActive(false);

        foreach (var source in new[] { profile.MaleVisual, profile.FemaleVisual, profile.WorldModel })
        {
            var instance = Instantiate(source, host.transform, false);
            bool drop = source == profile.WorldModel;
            var target = drop ? instance : instance.transform.Find("attach_skin").gameObject;
            var visual = target.AddComponent(visualType);

            visualType.GetMethod("Configure", flags).Invoke(visual, new object[] { styles, drop });

            for (int index = 0; index < 4; index++)
            {
                visualType.GetMethod("Setup", flags).Invoke(visual, new object[] { index });

                foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.sharedMaterial != styles.Styles[index].Material)
                    {
                        throw new InvalidOperationException("Standalone helmet color failed on body/drop clone.");
                    }
                }
            }
        }

        Destroy(host);
    }

}
#if UNITY_EDITOR
public static class CloakStylePlayerBuilder
{
    public static void Build()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);

        new GameObject("Style runtime validation").AddComponent<CloakStylePlayerValidation>();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Validation/CloakStylePlayer.unity");
        var result = UnityEditor.BuildPipeline.BuildPlayer(new[] { "Assets/Validation/CloakStylePlayer.unity" }, "C:/Users/Jonathan/Documents/ChatGPT/HexBattleMage/cloak-style-player-test/Validation.exe", UnityEditor.BuildTarget.StandaloneWindows64, UnityEditor.BuildOptions.Development);

        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new InvalidOperationException("Standalone style validation build failed.");
        }
    }
}
#endif

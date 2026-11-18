using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HexBattleMage.Core.Managers;
using MagicaCloth2;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CloakSingleSurfaceMigration
{
    private const string Root = "Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01";
    private const string Identity = "HexBattleMage_Cloak_Meadows_01";
    private const float PaintMatchScale = 10000f;

    public static void Run()
    {
        AssetDatabase.Refresh();
        var reference = JsonUtility.FromJson<MeadowsCloak01AssetBuilder.RigReference>(File.ReadAllText("Assets/Editor/HexBattleMage/CloakReference/NativeCapeRigReference.json"));
        var clothMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/MeadowsCloth.mat");
        var metalMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/MeadowsClasp.mat");
        var create = typeof(MeadowsCloak01AssetBuilder).GetMethod("CreateVisual", BindingFlags.Static | BindingFlags.NonPublic);

        clothMaterial.SetFloat("_Cull", 0f);
        clothMaterial.SetFloat("_TwoSidedNormals", 1f);
        EditorUtility.SetDirty(clothMaterial);

        foreach (string gender in new[] { "Male", "Female" })
        {
            string path = Root + "/Visuals/" + Identity + "_" + gender + ".prefab";
            string temporary = Root + "/Visuals/TemporarySurface_" + gender + ".prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var cloth = prefab.GetComponentInChildren<MagicaCloth>(true);
                var paint = cloth.GetSerializeData2().selectionData;
                var previous = new Dictionary<Vector3Int, VertexAttribute>();

                for (int index = 0; index < paint.positions.Length; index++)
                {
                    previous[Key(paint.positions[index])] = paint.attributes[index];
                }

                create.Invoke(null, new object[] { gender, reference, clothMaterial, metalMaterial, temporary });
                var renderer = cloth.GetComponent<SkinnedMeshRenderer>();
                var vertices = renderer.sharedMesh.vertices;
                var selection = new SelectionData(vertices.Length) { userEdit = true, maxConnectionDistance = paint.maxConnectionDistance };

                for (int index = 0; index < vertices.Length; index++)
                {
                    selection.positions[index] = vertices[index];
                    selection.attributes[index] = previous.TryGetValue(Key(vertices[index]), out var attribute) ? attribute : vertices[index].y >= 1.42f ? VertexAttribute.Fixed : VertexAttribute.Move;
                }

                cloth.GetSerializeData2().selectionData = selection;

                using (var setup = new RenderSetupData(null, renderer))
                {
                    bool weighted = setup.hasBoneWeight;

                    setup.hasBoneWeight = false;

                    if (!cloth.GetSerializeData2().initData.Serialize(cloth.SerializeData, new TransformRecord(cloth.transform, true), new TransformRecord(cloth.transform, true), new List<RenderSetupData> { setup }))
                    {
                        throw new InvalidOperationException("Could not rebuild the single-surface cloth initialization.");
                    }

                    setup.hasBoneWeight = weighted;
                    cloth.GetSerializeData2().initData.clothSetupDataList[0].hasBoneWeight = weighted;
                }

                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                Debug.Log(gender + " single-surface cloth: " + vertices.Length + " vertices; paint and collision profile retained.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
                AssetDatabase.DeleteAsset(temporary);
            }
        }

        MeadowsCloak01GameplayBuilder.Build();
        MeadowsCloak01AssetBuilder.Validate();
    }

    private static Vector3Int Key(Vector3 position)
    {
        return new Vector3Int(Mathf.RoundToInt(position.x * PaintMatchScale), Mathf.RoundToInt(position.y * PaintMatchScale), Mathf.RoundToInt(position.z * PaintMatchScale));
    }
}

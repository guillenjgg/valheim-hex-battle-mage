using HexBattleMage.Core.Managers;
using UnityEditor;
using UnityEngine;

public static class MageUiDefaultsMigration
{
    public static void Build()
    {
        const string path = "Assets/_CustomItems/HexBattleMage/Wands/Meadows/Wand_Meadows_01/HexBattleMage_Wand_Meadows_01.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);

        try
        {
            prefab.GetComponent<HexBattleMageWandProfile>().FullDamageRange = 15f;
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }
}

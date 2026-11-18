using System;
using System.IO;
using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Trinkets;
using UnityEditor;
using UnityEngine;

public static class PlayerTextLocalizationAssetBuilder
{
    private const string Root = "Assets/_CustomItems/HexBattleMage";

    [MenuItem("HexBattleMage/Tokenize Player Text and Build Bundle")]
    public static void Build()
    {
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

        if (stage != null && stage.scene.isDirty)
        {
            throw new InvalidOperationException("Save the open prefab before tokenizing player text.");
        }

        Edit(Root + "/Trinkets/Meadows/Trinket_Meadows_01/HexBattleMage_Trinket_Meadows_01.prefab", prefab =>
        {
            var profile = prefab.GetComponent<HexBattleMageTrinketProfile>();

            profile.DisplayName = Tokenize(profile.DisplayName, "Stone of Reprisal", "$item_hexbattlemage_trinket_meadows_01");
            profile.Description = Tokenize(profile.Description, "A rune-bound river stone that turns a well-timed defense into power.", "$item_hexbattlemage_trinket_meadows_01_description");
            profile.PassiveHeading = Tokenize(profile.PassiveHeading, "Passive Effects", "$hexbattlemage_passive_effects");
            profile.RewardHeading = Tokenize(profile.RewardHeading, "Combat Reward", "$hexbattlemage_combat_reward");
            profile.RewardCondition = Tokenize(profile.RewardCondition, "On a successful parry", "$hexbattlemage_on_successful_parry");
            profile.RewardDetails = Tokenize(profile.RewardDetails, "Further parries refresh the duration.", "$hexbattlemage_parry_refresh");
            profile.BossHeading = Tokenize(profile.BossHeading, "Boss Effect", "$hexbattlemage_boss_effect");
            profile.BossCondition = Tokenize(profile.BossCondition, "On a successful boss parry", "$hexbattlemage_on_successful_boss_parry");
            profile.BuffDisplayName = Tokenize(profile.BuffDisplayName, "Rune Reprisal", "$se_hexbattlemage_trinket_meadows_01");
            profile.BuffDescription = Tokenize(profile.BuffDescription, "Wand damage increased after a successful parry.", "$se_hexbattlemage_parry_wand_damage_description");
        });
        Edit(Root + "/Crafting/Extensions/HexMageRuneStone.prefab", prefab =>
        {
            var piece = prefab.GetComponent<Piece>();

            piece.m_name = Tokenize(piece.m_name, "Rune Stone", "$piece_hexbattlemage_runestone");
            piece.m_description = Tokenize(piece.m_description, "A protection rune that enhances the Mage Table.", "$piece_hexbattlemage_runestone_description");
        });
        Edit(Root + "/Crafting/Extensions/HexMageReagentStand.prefab", prefab =>
        {
            var piece = prefab.GetComponent<Piece>();

            piece.m_name = Tokenize(piece.m_name, "Reagent Stand", "$piece_hexbattlemage_reagentstand");
            piece.m_description = Tokenize(piece.m_description, "Meadows reagents prepared for stronger magic.", "$piece_hexbattlemage_reagentstand_description");
        });

        foreach (WandElement element in Enum.GetValues(typeof(WandElement)))
        {
            Edit(Root + "/Elements/HexWandElement" + element + ".prefab", prefab =>
            {
                var profile = prefab.GetComponent<WandElementProfile>();

                profile.StatusDisplayName = Tokenize(profile.StatusDisplayName, element + " Ward Mark", "$se_hexbattlemage_wand_" + element.ToString().ToLowerInvariant());
            });
        }

        AssetDatabase.SaveAssets();
        PrefabDefaultsAssetBuilder.PrepareAndBuild();
    }

    private static string Tokenize(string current, string previous, string token)
    {
        if (current == null)
        {
            return token;
        }

        if (current.StartsWith("$", StringComparison.Ordinal))
        {
            return current;
        }

        if (current != previous)
        {
            throw new InvalidOperationException($"Player text '{current}' was customized. Add its translation before assigning '{token}'.");
        }

        return token;
    }

    private static void Edit(string path, Action<GameObject> update)
    {
        var prefab = PrefabUtility.LoadPrefabContents(path);

        try
        {
            update(prefab);
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }
}

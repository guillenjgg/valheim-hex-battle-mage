using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class HexBattleMageRuneBlockProfile : MonoBehaviour
    {
        public const string PrefabName = "HexBattleMage_RuneBlock";
        [HideInInspector] public int AuthoredVersion;
        public string DisplayName = "$piece_hexbattlemage_runeblock";
        public string Description = "$piece_hexbattlemage_runeblock_description";
        public string PieceTable = "Hammer";
        public string Category = "Battle Mage";
        [Min(1)] public int BaseLevel = 1;
        [Min(0.1f)] public float RefreshInterval = 1f;
        [Header("Solid stump collision; bottom aligns with the ground origin")]
        public Vector3 BodyColliderCenter = new Vector3(0f, 0.39f, 0f);
        public Vector3 BodyColliderSize = new Vector3(1.34f, 0.78f, 1.34f);
        [Header("Character attunements")]
        [HideInInspector] public int AttunementVersion;
        [Range(1, 5)] public int MaximumActiveAttunements = 1;
        public GameObject[] Attunements = new GameObject[0];
        public string UpgradeMessage = "$hexbattlemage_runeblock_empowered";
        [Tooltip("Each boss key unlocks the corresponding next station level, shared across the world.")]
        public string[] BossDefeatKeys = { "defeated_eikthyr", "defeated_gdking", "defeated_bonemass", "defeated_dragon", "defeated_goblinking", "defeated_queen", "defeated_fader" };
        [Tooltip("Relative paths to cumulative visual stages, in the same order as boss keys. These are children, never extension prefabs.")]
        public string[] StageVisualPaths = { "Progression/BlackForest", "Progression/Swamp", "Progression/Mountains", "Progression/Plains", "Progression/Mistlands", "Progression/Ashlands", "Progression/DeepNorth" };
    }
}

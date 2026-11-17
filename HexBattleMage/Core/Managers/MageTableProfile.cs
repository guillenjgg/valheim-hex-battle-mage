using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class MageTableProfile : MonoBehaviour
    {
        public string DisplayName = "$piece_hexbattlemage_magetable";
        public string Description = "$piece_hexbattlemage_magetable_description";
        public string PieceTable = "Hammer";
        public string Category = "Battle Mage";
        public bool ShowBasicRecipes;
        public bool EnableVisuals = true;

        [Header("Halos and crystals")]
        public Color ArcaneColor = new Color(0.7f, 0.3f, 1f, 0.9f);
        [Min(3)] public int CircleSegments = 48;
        [Min(1)] public int RuneCount = 8;
        public float SurfaceClearance = 0.04f;
        public float RuneLineWidth = 0.02f;
        public Vector3 OuterHalo = new Vector3(0.62f, 0.3f, 18f);
        public Vector3 InnerHalo = new Vector3(0.42f, 0.48f, -55f);
        public Vector2 GlyphHalfSize = new Vector2(0.07f, 0.04f);
        public Vector3 CentralCrystalPosition = new Vector3(0f, 0.55f, 0f);
        public Vector3 SideCrystalPosition = new Vector3(0.48f, 0.3f, 0.12f);
        public float CentralCrystalScale = 0.8f;
        public float SideCrystalScale = 0.38f;
        public float RingRotationSpeed = 12f;
        public float CrystalBobHeight = 0.035f;
        public float CrystalBobSpeed = 1.8f;
        public float CrystalRotationSpeed = 25f;

        [Header("Light and sparks")]
        public float LightIntensity = 1.4f;
        public float LightRange = 3.5f;
        public float SparkHeight = 0.1f;
        public float SparkLifetime = 2f;
        public float SparkSpeed = 0.18f;
        public Vector2 SparkSize = new Vector2(0.025f, 0.055f);
        public int MaximumSparks = 40;
        public float SparkRate = 12f;
        public float SparkRadius = 0.5f;
    }
}

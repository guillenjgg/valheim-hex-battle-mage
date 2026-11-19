using HexBattleMage.Core.Elements;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class HexBattleMageWandProfile : MonoBehaviour
    {
        [Header("Combat defaults; development configuration overrides these values")]
        public float WandBluntDamage = 5f;
        public float WandPierceDamage = 5f;
        public float WandBluntDamagePerQuality = 1f;
        public float WandPierceDamagePerQuality = 1f;
        public string AttackAnimation = "swing_axe";
        public int AttackChainLevels = 3;
        public int RandomAttackAnimations;
        public float AnimationSpeedMultiplier = 1f;
        public float ProjectileVelocity = 100f;
        public float ProjectileAccuracy;
        public float ProjectileAccuracyMin;
        public bool SkillAccuracy;
        public float FullDamageRange = 15f;
        public float DamageFalloffDistance = 20f;
        public float MinimumDamageMultiplier = 0.2f;
        public bool EnableElements = true;
        public WandElement DefaultElement = WandElement.Fire;
        public WandElementProfile[] Elements = new WandElementProfile[0];
        public GameObject ProjectilePrefab;

        [Header("Lighting and constant rune glow")]
        public float WandLightRange = 5f;
        public float WandLightIntensity = 3f;
        public Color LightColor = new Color(0.7f, 0.8f, 1f);
        public Color RuneColor = new Color(0.35f, 0.75f, 1f);
        public float RuneEmissionStrength = 9f;
        public Color UnattunedTipEmission = new Color(1.8f, 3f, 4.5f);

        [Header("Cosmetic styles; array order matches saved item variants")]
        public Material[] StyleMaterials = new Material[0];

        [Header("Sheathed placement")]
        public string HipBoneName = "Hips";
        public Vector3 HipOffset = new Vector3(0.24f, 0.02f, 0.18f);
        public Vector3 HolsterAngles = new Vector3(90f, 0f, 15f);
    }
}

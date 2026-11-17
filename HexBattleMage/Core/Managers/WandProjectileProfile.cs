using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class WandProjectileProfile : MonoBehaviour
    {
        [Header("Collision and damage area")]
        public float AreaOfEffectRadius = 3f;
        public float CollisionRadius = 0.4f;
        public bool OnlyStopOnTerrain;

        [Header("Flight visuals")]
        public float BoltLength = 1.5f;
        public float BoltWidth = 0.025f;
        public float BoltJaggedness = 0.07f;
        public float BoltBrightness = 2f;
        public Color BoltColor = new Color(0.3f, 0.6f, 1f, 1f);
        [Min(2)] public int BoltPointCount = 11;
        public float GlowWidthMultiplier = 4f;
        public float FlickerFrequency = 24f;
        public float MinimumVisibleLength = 0.08f;
        public float HaloOpacity = 0.65f;
        public AnimationCurve BoltWidthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.2f, 0.8f), new Keyframe(1f, 0f));

        [Header("Impact visuals and sound")]
        public float ImpactSoundVolume = 1f;
        public float ImpactVisualDuration = 0.18f;
        public float ImpactBurstOpacity = 0.85f;
        public float ImpactLineWidth = 0.012f;
        [Min(1)] public int SparkCount = 18;
        public float MaximumBurstRadius = 0.5f;
        public float DirectHitBurstRadius = 0.12f;
        public float MinimumSparkLength = 0.55f;
        public float SparkWidthMultiplier = 2f;
        public float InitialExpansion = 0.15f;
        public float SparkMidpoint = 0.65f;
        public float SparkCoreWhitening = 0.2f;
        public float SparkTailOpacity = 0.15f;
        public float SurfaceProbeDistance = 2f;
        public float SurfaceOffset = 0.06f;
        public string ImpactSoundPrefab = "fx_fireball_staff_explosion";
        public float SoundMinimumDistance = 1f;
        public float SoundMaximumDistance = 20f;
        public bool SuppressNativeParticles = true;
        public bool SuppressNativeRenderers = true;
        public bool SuppressNativeLights = true;
        public bool SuppressNativeHitEffects = true;
    }
}

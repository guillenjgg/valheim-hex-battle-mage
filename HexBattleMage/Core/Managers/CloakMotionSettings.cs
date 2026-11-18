using MagicaCloth2;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public static class CloakMotionSettings
    {
        public static void Apply(HexBattleMageCloakCollisionProfile profile, ClothSerializeData settings)
        {
            settings.colliderCollisionConstraint.mode = ColliderCollisionConstraint.Mode.Edge;
            settings.colliderCollisionConstraint.friction = Mathf.Clamp01(profile.Friction);
            settings.radius.value = Mathf.Max(0.001f, profile.ClothThickness);
            settings.animationPoseRatio = Mathf.Clamp01(profile.AnimationPoseRatio);
            settings.damping.value = 1f;
            settings.damping.useCurve = true;
            settings.damping.curve = RegionCurve(profile, profile.UpperWrapDamping, profile.MotionDamping);
            settings.wind.influence = Mathf.Clamp01(profile.WindInfluence);
            settings.wind.depthWeight = Mathf.Clamp01(profile.WindDepthWeight);
            settings.wind.turbulence = Mathf.Clamp01(profile.WindTurbulence);
            settings.angleRestorationConstraint.useAngleRestoration = true;
            settings.angleRestorationConstraint.stiffness.value = 1f;
            settings.angleRestorationConstraint.stiffness.useCurve = true;
            settings.angleRestorationConstraint.stiffness.curve = RegionCurve(profile, profile.UpperWrapRestoration, profile.DrapeRestoration);

            var motion = settings.motionConstraint;

            motion.useBackstop = profile.UseBackstop;
            motion.backstopRadius = Mathf.Clamp(profile.BackstopRadius, 0.01f, 10f);
            motion.backstopDistance.value = Mathf.Clamp01(profile.BackstopDistance);
            motion.useMaxDistance = true;
            motion.maxDistance.value = Mathf.Clamp(profile.MaximumTravel, 0.01f, 5f);
            motion.stiffness = 1f;
        }

        private static AnimationCurve RegionCurve(HexBattleMageCloakCollisionProfile profile, float upper, float lower)
        {
            float split = Mathf.Clamp(profile.UpperWrapDepth, 0.01f, 0.99f);

            return new AnimationCurve(new Keyframe(0f, Mathf.Clamp01(upper)), new Keyframe(split, Mathf.Clamp01(upper)), new Keyframe(1f, Mathf.Clamp01(lower)));
        }
    }
}

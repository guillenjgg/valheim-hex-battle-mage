using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class MagicBracerProfile : MonoBehaviour
    {
        public string BaseShieldPrefab = "ShieldWood";
        public string DisplayName = "$item_hexbattlemage_magicbracer";
        public string Description = "$item_hexbattlemage_magicbracer_description";
        public Sprite Icon;
        public float BlockPower = 6f;
        public float BlockPowerPerQuality = 6f;
        public float BlockForce = 20f;
        public float BlockForcePerQuality = 5f;
        public float ParryBonus = 1.5f;
        public float BlockStaminaModifier;
        public float Durability = 200f;
        public float DurabilityPerQuality = 50f;
        public int MaximumQuality = 3;
        public float Weight = 4f;
        public bool UseDurability = true;
        public float DurabilityDrain;
        public float UseDurabilityDrain = 1f;
        public float MovementModifier;

        [Header("Native inherited effects and combat rules")]
        [Tooltip("Block effect references come from BaseShieldPrefab; disabling this clears only the block effect list. Other native shield rules still come from the named source.")]
        public bool InheritNativeBlockEffects = true;
        public Vector3 DropColliderSize = new Vector3(0.15f, 0.2f, 0.15f);

        [Header("Wearable fit; current body mesh selects radial scale")]
        public string ForearmBoneName = "LeftForeArm";
        public string WristBoneName = "LeftHand";
        public int FemaleModelIndex = 1;
        [Range(0f, 1f)] public float BracerForearmPosition = 0.62f;
        [Min(0.001f)] public float ReferenceForearmLength = 0.2600273f;
        public float MaleBracerRadialScale = 1.2f;
        public float FemaleBracerRadialScale = 1.05f;

        [Header("Constant rune glow and blocking field")]
        public float RuneBrightness = 2f;
        [Min(0.001f)] public float OpenDuration = 0.14f;
        public float FieldScale = 1.5f;
        public float FieldForwardOffset = 0.22f;
        public float RuneDegreesPerSecond = 8f;
        public float PulseFrequency = 2.5f;
        public float PulseAmplitude = 0.012f;
        public float InitialFieldScale = 0.08f;
        public Color FieldTint = Color.white;
    }
}

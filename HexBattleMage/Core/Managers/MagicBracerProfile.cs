using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class MagicBracerProfile : MonoBehaviour
    {
        public string BaseShieldPrefab = "ShieldWood";
        public float BlockPower = 6f;
        public float BlockPowerPerQuality = 6f;
        public float BlockForce = 20f;
        public float BlockForcePerQuality = 5f;
        public float ParryBonus = 1.5f;
        public float BlockStaminaModifier;
        public float Durability = 200f;
        public float DurabilityPerQuality = 50f;
        public int MaximumQuality = 3;
    }
}

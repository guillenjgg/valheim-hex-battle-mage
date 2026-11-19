using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class HexBattleMageVitalityProfile : MonoBehaviour
    {
        [Header("Flat equipped bonuses; item quality does not increase these values")]
        [Min(0f)] public float MaximumHealth;
        [Min(0f)] public float MaximumStamina;
    }
}

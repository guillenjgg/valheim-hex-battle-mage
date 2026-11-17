using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class MageEitrProfile : MonoBehaviour
    {
        [Min(0f)] public float MaximumEitr = 25f;
        [Min(0f)] public float RegenerationModifier = 0.15f;
    }
}

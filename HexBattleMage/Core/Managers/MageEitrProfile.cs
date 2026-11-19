using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class MageEitrProfile : MonoBehaviour
    {
        [Min(0f)] public float MaximumEitr = 25f;
        [Min(0f)] public float MaximumEitrPerQuality;
        [Min(0f)] public float RegenerationModifier = 0.15f;

        public float GetMaximumEitr(int quality)
        {
            return Mathf.Max(0f, MaximumEitr + Mathf.Max(0, quality - 1) * MaximumEitrPerQuality);
        }
    }
}

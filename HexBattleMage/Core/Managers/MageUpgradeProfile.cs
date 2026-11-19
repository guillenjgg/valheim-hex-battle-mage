using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class MageUpgradeProfile : MonoBehaviour
    {
        [Min(1)] public int RequiredStationLevel = 1;
        [Tooltip("Use each recipe resource's Amount Per Quality as a fixed cost for every upgrade.")]
        public bool FixedResourceCosts;
    }
}

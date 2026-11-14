using System;

namespace HexBattleMage.Core
{
    internal static class ProjectileDamageFalloff
    {
        internal static float CalculateMultiplier(float distance, float fullDamageRange, float falloffDistance, float minimumMultiplier)
        {
            if (distance <= fullDamageRange)
            {
                return 1f;
            }

            float progress = Math.Min((distance - fullDamageRange) / falloffDistance, 1f);

            return Math.Max(minimumMultiplier, 1f - progress * (1f - minimumMultiplier));
        }
    }
}

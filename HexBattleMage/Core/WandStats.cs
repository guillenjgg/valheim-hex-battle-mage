using HexBattleMage.Assets;

namespace HexBattleMage.Core
{
    internal static class WandStats
    {
        internal const string CompendiumTopic = "$hexbattlemage_wandstats_title";

        private static Player _player;
        private static string _prefabName;
        private static float _damage;
        private static float _areaRadius;

        internal static void RecordImpact(Player player, string prefabName, float damage, float areaRadius)
        {
            _player = player;
            _prefabName = prefabName;
            _damage = damage;
            _areaRadius = areaRadius;
        }

        internal static string GetText()
        {
            if (_player == Player.m_localPlayer && _prefabName != null)
            {
                return FormatImpact(_prefabName, _damage, _areaRadius);
            }

            return ModLocalization.Format("$hexbattlemage_wandstats_empty", AssetBundleHelper.HexBattleMageWand.name);
        }

        internal static string FormatImpact(string prefabName, float damage, float areaRadius)
        {
            if (areaRadius > 0f)
            {
                return ModLocalization.Format("$hexbattlemage_wandstats_area", prefabName, damage, areaRadius);
            }

            return ModLocalization.Format("$hexbattlemage_wandstats_direct", prefabName, damage);
        }
    }
}

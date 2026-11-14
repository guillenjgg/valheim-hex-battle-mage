using HexBattleMage.Assets;

namespace HexBattleMage.Core
{
    internal static class WandStats
    {
        internal const string CompendiumTopic = "HexBattleMage - Wand Stats";

        private static Player _player;
        private static string _latestImpactText;

        internal static void RecordImpact(Player player, string prefabName, float damage, float areaRadius)
        {
            _player = player;
            _latestImpactText = FormatImpact(prefabName, damage, areaRadius);
        }

        internal static string GetText()
        {
            if (_player == Player.m_localPlayer && _latestImpactText != null)
            {
                return _latestImpactText;
            }

            return $"Prefab: {AssetBundleHelper.HexBeechWand.name}\nImpact damage: Not recorded\nAoE damage: Not recorded\n\nFire the wand to record an impact.";
        }

        internal static string FormatImpact(string prefabName, float damage, float areaRadius)
        {
            if (areaRadius > 0f)
            {
                return $"Prefab: {prefabName}\nImpact damage: 0 (area hit)\nAoE damage: {damage:0.##} per target ({areaRadius:0.##} m radius)\n\nLatest impact, after range falloff and before target defenses. Area damage replaces direct impact damage.";
            }

            return $"Prefab: {prefabName}\nImpact damage: {damage:0.##}\nAoE damage: 0\n\nLatest impact, after range falloff and before target defenses.";
        }
    }
}

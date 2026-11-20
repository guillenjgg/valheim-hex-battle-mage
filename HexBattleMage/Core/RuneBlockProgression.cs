using HexBattleMage.Core.Managers;

namespace HexBattleMage.Core
{
    internal static class RuneBlockProgression
    {
        private const string NotificationKeyPrefix = "HexBattleMage.RuneBlockLevel.";

        internal static int GetLevel(HexBattleMageRuneBlockProfile profile)
        {
            int level = profile.BaseLevel;
            var zone = ZoneSystem.instance;

            if (zone == null)
            {
                return level;
            }

            for (int index = 0; index < profile.BossDefeatKeys.Length; index++)
            {
                if (zone.GetGlobalKey(profile.BossDefeatKeys[index]))
                {
                    level = profile.BaseLevel + index + 1;
                }
            }

            return level;
        }

        internal static bool RecordLevel(Player player, long worldId, int level, int baseLevel)
        {
            string key = NotificationKeyPrefix + worldId;
            int previous = baseLevel;

            bool saved = player.m_customData.TryGetValue(key, out string value);

            if (saved)
            {
                int.TryParse(value, out previous);
            }

            if (saved && level == previous)
            {
                return false;
            }

            player.m_customData[key] = level.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return level > previous && level > baseLevel;
        }
    }
}

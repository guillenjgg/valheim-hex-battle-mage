using BepInEx.Logging;

namespace HexBattleMage.Core
{
    internal static class DebugLog
    {
        internal static void LogInfo(string message)
        {
            Write(LogLevel.Info, message);
        }

        internal static void LogWarning(string message)
        {
            Write(LogLevel.Warning, message);
        }

        internal static void LogError(string message)
        {
            Write(LogLevel.Error, message);
        }

        private static void Write(LogLevel level, string message)
        {
            if (DevConfig.EnableDebugLogging == null)
            {
                return;
            }

            if (!DevConfig.EnableDebugLogging.Value)
            {
                return;
            }

            Plugin.Log.Log(level, message);
        }
    }
}

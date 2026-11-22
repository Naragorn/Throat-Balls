using UnityEngine;

namespace ThroatBalls
{
    /// <summary>Writes to the game log (Player.log) with a fixed prefix so lines are easy to grep.</summary>
    internal static class ModLog
    {
        public const string Prefix = "[ThroatBalls] ";

        public static void Info(string message) => Debug.Log(Prefix + message);

        public static void Warn(string message) => Debug.LogWarning(Prefix + message);

        public static void Error(string message) => Debug.LogError(Prefix + message);

        /// <summary>Only logs when "Debug Logging" is on in the Mod Menu.</summary>
        public static void Verbose(string message)
        {
            if (ModOptions.DebugLogging)
            {
                Info(message);
            }
        }
    }
}

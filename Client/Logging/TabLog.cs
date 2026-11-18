using System;
using System.Collections.Generic;

namespace ImprovedTabUI.Logging
{
    internal static class TabLog
    {
        private static readonly HashSet<string> SeenWarnings = new HashSet<string>(StringComparer.Ordinal);

        internal static void Warn(string message)
        {
            Plugin.Log?.LogWarning(message);
        }

        internal static void WarnOnce(string key, string message)
        {
            if (Plugin.Log == null || key == null || !SeenWarnings.Add(key))
            {
                return;
            }

            Plugin.Log.LogWarning(message);
        }

        internal static void Error(string message, Exception ex)
        {
            Plugin.Log?.LogError(message + ": " + ex);
        }
    }
}

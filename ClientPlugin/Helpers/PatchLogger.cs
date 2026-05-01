using Keen.VRage.Library.Diagnostics;
using System;

namespace ClientPlugin.Helpers
{
    internal static class PatchLogger
    {
        /// <summary>
        /// If tracking or logging is disabled, log a single notification and return true
        /// to indicate that the caller should skip further processing until reenabled
        /// </summary>
        /// <param name="pluginName"></param>
        /// <param name="patchName"></param>
        /// <param name="feature"></param>
        /// <param name="condition"></param>
        /// <param name="disabledMessageDisplayed"></param>
        /// <returns></returns>
        public static bool IsDisabled(string pluginName, string patchName, string feature, bool condition, ref bool disabledMessageDisplayed)
        {
            if (condition)
            {
                disabledMessageDisplayed = false;
                return false;
            }

            if (!disabledMessageDisplayed)
            {
                Log.Default.WriteLine($"[{pluginName}] [{patchName}] {feature} disabled - no further logging until reenabled.");
                disabledMessageDisplayed = true;
            }
            return true;
        }
    }
}
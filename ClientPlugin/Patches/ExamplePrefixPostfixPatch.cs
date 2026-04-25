using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Keen.Game2.Simulation.GameSystems.Ownership;
using Keen.Game2.Simulation.GameSystems.Player;
using Keen.VRage.Library.Diagnostics;

namespace ClientPlugin.Patches;

// ReSharper disable once UnusedType.Global
[HarmonyPatch(typeof(GameModeSessionComponent))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class GameModeSessionComponentPatch
{
    private static Config Config => Config.Current;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameModeSessionComponent.IsCreative), typeof(IdentityId))]
    public static bool IsCreativePrefix(IdentityId identity)
    {
        // Use the config to enable patches corresponding to your plugin's features
        if (!Config.Enabled)
            return true;

        // Your logic to run before or instead the original method implementation.
        // You cannot and should not attempt to call the original method here.
        Log.Default.WriteLine($"[{Plugin.Name}] IsCreative check for identity: {identity}");

        // Return false to replace the original method, make sure any return value and out arguments are handled.
        // Return true to call the original method.
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(GameModeSessionComponent.IsCreative), typeof(IdentityId))]
    public static void IsCreativePostfix(bool __result, IdentityId identity)
    {
        // Use the config to enable patches corresponding to your plugin's features
        if (!Config.Enabled)
            return;

        // Your logic to run after the original method implementation.
        Log.Default.WriteLine($"[{Plugin.Name}] IsCreative result for identity {identity}: {__result}");
    }
}

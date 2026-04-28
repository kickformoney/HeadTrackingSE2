using HarmonyLib;
using Keen.Game2.Client.WorldObjects.Character;
using Keen.Game2.Simulation.WorldObjects.Movement;
using Keen.VRage.Library.Diagnostics;

namespace ClientPlugin.Helpers
{
    //[HarmonyPatch(typeof(EntityMovementExtensions), "UpdateControlData")]
    //internal class Patch_LogMovementInputs
    //{
    //    private static void Postfix(in MovementInputs inputs)
    //    {
    //        Log.Default.WriteLine($"[{Plugin.Name}] Inputs: Pitch={inputs.Pitch:F6} Yaw={inputs.Yaw:F6}");
    //    }
    //}

    //[HarmonyPatch(typeof(CharacterMovementInputHandlerComponent), "UpdateMovement")]
    //internal class Patch_LogUpdateMovement
    //{
    //    private static void Prefix(CharacterMovementInputHandlerComponent __instance)
    //    {
    //        Log.Default.WriteLine($"[{Plugin.Name}] UpdateMovement: Yaw={__instance._basicMovementInputs.Yaw:F4} Pitch={__instance._basicMovementInputs.Pitch:F4}");
    //    }
    //}
}
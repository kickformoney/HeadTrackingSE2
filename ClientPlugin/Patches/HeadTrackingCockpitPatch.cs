using ClientPlugin.Helpers;
using HarmonyLib;
using Keen.Game2.Client.GameSystems.CameraSystems.Modes;
using Keen.Game2.Simulation.WorldObjects.Movement;
using Keen.VRage.DCS.Components;
using Keen.VRage.Library.Diagnostics;
using Keen.VRage.Library.Mathematics;
using System;
using System.Reflection;

namespace ClientPlugin.Patches;

/// <summary>
/// Patches FirstPersonCameraWithInputComponent.UpdateRelativeTransform to inject
/// head tracking data from OpenTrack into the cockpit freelook camera
///
/// How it works:
///   UpdateRelativeTransform is the static ECS job that converts RotationData
///   (yaw/pitch in degrees) into the RelativeTransform quaternion applied to the
///   camera each frame. By writing OpenTrack values into RotationData.Rotation
///   in a Prefix patch, the engine's own clamping and quaternion conversion runs
///   against the supplied values
///
/// Coordinate mapping:
///   RotationData.Rotation.X = yaw   (engine negates this: 0f - Rotation.X)
///   RotationData.Rotation.Y = pitch (engine uses this directly)
///   RotationData.Rotation.Z = roll  (zeroed by UpdateRelativeTransform, ignored)
///
///   Values are inverted in the engine, so use -yaw and -pitch
/// </summary>
[HarmonyPatch(typeof(FirstPersonCameraWithInputComponent), "UpdateRelativeTransform")]
internal class HeadTrackingCockpitPatch
{
    #region Fields

    public static bool logToFile = false;
    private static int logCounter = 0;
    private static bool loggingDisabledMessageShown = false;
    private static int noDataLogCounter = 0;

    #endregion Fields

    #region Methods

    public static void Register(Harmony harmony)
    {
        try
        {
            // Manually patch UpdateRelativeTransform
            var targetMethod = typeof(FirstPersonCameraWithInputComponent)
                .GetMethod("UpdateRelativeTransform",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (targetMethod != null)
            {
                var prefix = typeof(HeadTrackingCockpitPatch)
                    .GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefix));
                Log.Default.WriteLine($"[{Plugin.Name}] UpdateRelativeTransform patched manually.");
            }

            // Patch OnAddedToScene to cache topLevelParent
            var onAdded = typeof(FirstPersonCameraWithInputComponent)
                .GetMethod("OnAddedToScene",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (onAdded != null)
            {
                var postfix = typeof(CacheCockpitParentPatch)
                    .GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(onAdded, postfix: new HarmonyMethod(postfix));
                Log.Default.WriteLine($"[{Plugin.Name}] OnAddedToScene patched manually.");
            }

            // Patch OnBeforeRemovedFromScene to clear topLevelParent
            var onRemoved = typeof(FirstPersonCameraWithInputComponent)
                .GetMethod("OnBeforeRemovedFromScene",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (onRemoved != null)
            {
                var postfix = typeof(ClearCockpitParentPatch)
                    .GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(onRemoved, postfix: new HarmonyMethod(postfix));
                Log.Default.WriteLine($"[{Plugin.Name}] OnBeforeRemovedFromScene patched manually.");
            }
        }
        catch (Exception e)
        {
            Log.Default.WriteLine($"[{Plugin.Name}] Failed to patch HeadTrackingCockpitPatch: {e}");
        }
    }

    internal static Entity? topLevelParent = null;

    [HarmonyPatch(typeof(FirstPersonCameraWithInputComponent), "OnAddedToScene")]
    internal class CacheCockpitParentPatch
    {
        private static void Postfix(FirstPersonCameraWithInputComponent __instance)
        {
            HeadTrackingCockpitPatch.topLevelParent = __instance._topLevelParent;
            Log.Default.WriteLine($"[{Plugin.Name}] Cached cockpit top level parent.");
        }
    }

    [HarmonyPatch(typeof(FirstPersonCameraWithInputComponent), "OnBeforeRemovedFromScene")]
    internal class ClearCockpitParentPatch
    {
        private static void Postfix()
        {
            HeadTrackingCockpitPatch.topLevelParent = null;
            Log.Default.WriteLine($"[{Plugin.Name}] Cleared cockpit top level parent.");
        }
    }

    private static void Prefix(ref FirstPersonCameraWithInputComponent.RotationData rotationData)
    {
        //Log.Default.WriteLine($"[{Plugin.Name}] Cockpit Prefix fired. logCounter={logCounter} CockpitLogging={Config.Current.CockpitLogging} LogToFile={LogToFile}");

        logToFile = Config.Current.CockpitLogging && (logCounter++ % 180) == 0; // 180 frames - approximately 3 seconds

        if (!Config.Current.EnableCockpitTracking)
        {
            if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] Cockpit Tracking Disabled - Skipping additional logging");
            return;
        }

        // Show the "Logging disabled" message only once when logging is turned off
        if (!Config.Current.CockpitLogging)
        {
            if (!loggingDisabledMessageShown)
            {
                Log.Default.WriteLine($"[{Plugin.Name}] Cockpit logging disabled - no further log entries will be written for this patch until logging is enabled");
                loggingDisabledMessageShown = true;
            }
        }
        else // reset flag if logging is enabled
        {
            loggingDisabledMessageShown = false;
        }

        if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] Patch firing. Current RotationData.Rotation: {rotationData.Rotation}");
        if (!OpenTrackReader.TryGetPose(out float yaw, out float pitch))
        {
            int frames = logToFile ? 180 : 3600;

            if (noDataLogCounter++ % frames == 0) // 3600 frames - approximately once per minute
            {
                Log.Default.WriteLine($"[{Plugin.Name}] OpenTrack: no data");
            }
            return;
        }

        if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] OpenTrack raw: yaw={yaw:F2}, pitch={pitch:F2}");

        rotationData.Rotation = new Vector3(-yaw, -pitch, 0f);

        if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] Written rotation: {rotationData.Rotation}");
    }

    #endregion Methods
}
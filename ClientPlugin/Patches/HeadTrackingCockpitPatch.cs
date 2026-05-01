using ClientPlugin.Helpers;
using HarmonyLib;
using Keen.Game2.Client.GameSystems.CameraSystems.Modes;
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

    private const string patchName = "Cockpit";

    private static int logCounter = 0;
    private static bool loggingDisabledMessageShown = false;
    private static bool logToFile = false;
    private static int noDataLogCounter = 0;
    private static bool trackingDisabledMessageShown = false;

    #endregion Fields

    #region Methods

    internal static Entity? topLevelParent = null;

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
                Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] UpdateRelativeTransform patched manually.");
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
                Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] OnAddedToScene patched manually.");
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
                Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] OnBeforeRemovedFromScene patched manually.");
            }
        }
        catch (Exception e)
        {
            Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Failed to patch HeadTrackingCockpitPatch: {e}");
        }
    }

    private static void Prefix(ref FirstPersonCameraWithInputComponent.RotationData rotationData)
    {
        //Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Cockpit Prefix fired. logCounter={logCounter} CockpitLogging={Config.Current.CockpitLogging} LogToFile={LogToFile}");

        logToFile = Config.Current.CockpitLogging && (logCounter++ % 180) == 0; // 180 frames - approximately 3 seconds

        if (PatchLogger.IsDisabled(Plugin.Name, patchName, "Tracking", Config.Current.EnableCockpitTracking, ref trackingDisabledMessageShown)) return;
        PatchLogger.IsDisabled(Plugin.Name, patchName, "Logging", Config.Current.CockpitLogging, ref loggingDisabledMessageShown);

        if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Patch firing. Current RotationData.Rotation: {rotationData.Rotation}");
        if (!OpenTrackReader.TryGetPose(out float yaw, out float pitch))
        {
            int frames = logToFile ? 180 : 3600;

            if (noDataLogCounter++ % frames == 0) // 3600 frames - approximately once per minute
            {
                Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] FreeTrack: no data");
            }
            return;
        }

        if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] FreeTrack raw: yaw={yaw:F2}, pitch={pitch:F2}");

        rotationData.Rotation = new Vector3(-yaw, -pitch, 0f);

        if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Written rotation: {rotationData.Rotation}");
    }

    [HarmonyPatch(typeof(FirstPersonCameraWithInputComponent), "OnAddedToScene")]
    internal class CacheCockpitParentPatch
    {
        #region Methods

        private static void Postfix(FirstPersonCameraWithInputComponent __instance)
        {
            HeadTrackingCockpitPatch.topLevelParent = __instance._topLevelParent;
            Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Cached cockpit top level parent.");
        }

        #endregion Methods
    }

    [HarmonyPatch(typeof(FirstPersonCameraWithInputComponent), "OnBeforeRemovedFromScene")]
    internal class ClearCockpitParentPatch
    {
        #region Methods

        private static void Postfix()
        {
            HeadTrackingCockpitPatch.topLevelParent = null;
            Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Cleared cockpit top level parent.");
        }

        #endregion Methods
    }

    #endregion Methods
}
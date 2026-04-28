using ClientPlugin.Helpers;
using HarmonyLib;
using Keen.Game2.Client.GameSystems.CameraSystems.Modes;
using Keen.VRage.Library.Diagnostics;
using Keen.VRage.Library.Mathematics;

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

    private static bool logToFile = false;
    private static int logCounter = 0;
    private static int noDataLogCounter = 0;

    #endregion Fields

    // Enable to add debugging info to %Appdata%\SpaceEngineers2\Temp\Logs

    #region Methods

    private static void Prefix(ref FirstPersonCameraWithInputComponent.RotationData rotationData)
    {
        bool enableLogging = logToFile && (logCounter++ % 180) == 0; // 180 frames - approximately 3 seconds

        if (!Config.Current.CockpitEnabled)
        {
            if (enableLogging) Log.Default.WriteLine($"[{Plugin.Name}] Cockpit Rotation Disabled - Skipping additional logging");
            return;
        }

        if (enableLogging) Log.Default.WriteLine($"[{Plugin.Name}] Patch firing. Current RotationData.Rotation: {rotationData.Rotation}");

        if (!OpenTrackReader.TryGetPose(out float yaw, out float pitch))
        {
            int frames = enableLogging ? 180 : 18000;

            if (noDataLogCounter++ % frames == 0) // 18000 frames - approximately five minutes
            {
                Log.Default.WriteLine($"[{Plugin.Name}] OpenTrack: no data.");
            }
            return;
        }

        if (enableLogging) Log.Default.WriteLine($"[{Plugin.Name}] OpenTrack raw: yaw={yaw:F2}, pitch={pitch:F2}");

        rotationData.Rotation = new Vector3(-yaw, -pitch, 0f);

        if (enableLogging) Log.Default.WriteLine($"[{Plugin.Name}] Written rotation: {rotationData.Rotation}");
    }

    #endregion Methods
}
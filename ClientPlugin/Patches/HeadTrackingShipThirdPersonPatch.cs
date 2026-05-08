using ClientPlugin.Helpers;
using HarmonyLib;
using Keen.Game2.Client.GameSystems.CameraSystems.Modes;
using Keen.Game2.Simulation.WorldObjects.Movement;
using Keen.VRage.Library.Diagnostics;
using Keen.VRage.Library.Mathematics;
using System;
using System.Linq;
using System.Reflection;
using static Keen.Game2.Client.GameSystems.CameraSystems.Modes.ThirdPersonCameraComponent;

namespace ClientPlugin.Patches
{
    //[HarmonyPatch(typeof(ThirdPersonCameraComponent), "UpdateRelativeTransform")]
    internal class HeadTrackingShipThirdPersonPatch
    {
        #region Fields

        private const string patchName = "Ship External";

        private static ThirdPersonCameraComponent? instance;
        private static int logCounter = 0;
        private static bool loggingDisabledMessageShown = false;
        private static bool logToFile = false;
        private static int noDataLogCounter = 0;
        private static bool trackingDisabledMessageShown = false;

        #endregion Fields

        #region Methods

        public static void Register(Harmony harmony)
        {
            try
            {
                // Patch OnAddedToScene to ensure LookOffsetData is present
                var onAddedToScene = typeof(ThirdPersonCameraComponent)
                    .GetMethod("OnAddedToScene",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (onAddedToScene != null)
                {
                    var onAddedToScenePrefix = typeof(HeadTrackingShipThirdPersonPatch)
                        .GetMethod("OnAddedToScenePrefix", BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(onAddedToScene, prefix: new HarmonyMethod(onAddedToScenePrefix));
                    Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] ThirdPersonCameraComponent.OnAddedToScene patched manually");
                }
                else
                {
                    Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] ThirdPersonCameraComponent.OnAddedToScene not found");
                }

                var onRemovedFromScene = typeof(ThirdPersonCameraComponent)
                    .GetMethod("OnBeforeRemovedFromScene",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (onRemovedFromScene != null)
                {
                    var onRemovedPostfix = typeof(HeadTrackingShipThirdPersonPatch)
                        .GetMethod("OnBeforeRemovedFromScenePostfix", BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(onRemovedFromScene, postfix: new HarmonyMethod(onRemovedPostfix));
                    Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] ThirdPersonCameraComponent.OnBeforeRemovedFromScene patched manually");
                }
                else
                {
                    Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] ThirdPersonCameraComponent.OnBeforeRemovedFromScenePostfix not found");
                }

                // Patch UpdateRelativeTransform — target the 3-parameter overload with optional distanceOverride
                var updateRelativeTransform = typeof(ThirdPersonCameraComponent)
                    .GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == "UpdateRelativeTransform" &&
                                         m.GetParameters().Length == 3 &&
                                         m.GetParameters()[2].IsOptional);

                if (updateRelativeTransform != null)
                {
                    var prefix = typeof(HeadTrackingShipThirdPersonPatch)
                        .GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);

                    var postfix = typeof(HeadTrackingShipThirdPersonPatch)
                        .GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);

                    harmony.Patch(updateRelativeTransform,
                        prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                        postfix: postfix != null ? new HarmonyMethod(postfix) : null);

                    Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] ThirdPersonCameraComponent.UpdateRelativeTransform patched manually. Prefix={prefix != null} Postfix={postfix != null}");
                }
                else
                {
                    Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] ThirdPersonCameraComponent.UpdateRelativeTransform not found");
                }
            }
            catch (Exception e)
            {
                Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Failed to patch HeadTrackingShipThirdPersonPatch: {e}");
            }
        }

        private static void OnAddedToScenePrefix(ThirdPersonCameraComponent __instance)
        {
            // using a private instance field to store the reference, to avoid issues when calling it up in Prefix
            instance = __instance;

            if (!__instance._topLevelParent.Data.Has<LookOffsetData>())
            {
                __instance._topLevelParent.Data.Set(new LookOffsetData
                {
                    PitchOffset = 0f,
                    YawOffset = 0f,
                    MinPitchLookAngle = MathHelper.ToRadians(-90f),
                    MaxPitchLookAngle = MathHelper.ToRadians(90f)
                });

                Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Added LookOffsetData before OnAddedToScene");
            }
        }

        private static void OnBeforeRemovedFromScenePostfix()
        {
            instance = null;
            Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Cleared instance");
        }

        private static void Prefix(ThirdPersonCameraComponent __instance, ref ThirdPersonCameraData cameraData)
        {
            logToFile = Config.Current.ExternalViewLogging && (logCounter++ % 180) == 0;

            if (PatchLogger.IsDisabled(Plugin.Name, patchName, "Tracking", Config.Current.EnableExternalTracking, ref trackingDisabledMessageShown)) return;
            PatchLogger.IsDisabled(Plugin.Name, patchName, "Logging", Config.Current.ExternalViewLogging, ref loggingDisabledMessageShown);

            if (instance == null || instance._topLevelParent == null)
            {
                if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Instance not ready - skipping");
                return;
            }

            if (cameraData.InFirstPerson)
            {
                if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] In first person mode - skipping");
                return;
            }

            if (!Config.Current.EnableExternalTracking)
            {
                if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Tracking disabled - skipping");
                return;
            }

            if (!OpenTrackReader.TryGetPose(out float yaw, out float pitch))
            {
                if (noDataLogCounter++ % 18000 == 0) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] No OpenTrack data.");
                return;
            }

            if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Patch firing. Current LookOffset before: Yaw={(instance._topLevelParent.Data.TryGet<LookOffsetData>(out var before) ? before.YawOffset : 0):F4)} Pitch={before.PitchOffset:F4}");
            if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] OpenTrack raw: yaw={yaw:F2} pitch={pitch:F2}");

            // Inject into LookOffsetData so UpdateLookOffsetRotation will apply it to the camera
            var data = instance._topLevelParent.Data;

            if (!data.TryGet<LookOffsetData>(out _))
            {
                if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] LookOffsetData missing");
                return;
            }

            ref LookOffsetData lookOffset = ref data.GetWritePtr<LookOffsetData>();

            yaw = Config.Current.InvertYaw ? -yaw : yaw;
            pitch = Config.Current.InvertPitch ? -pitch : pitch;

            lookOffset.YawOffset = MathHelper.ToRadians(yaw);
            lookOffset.PitchOffset = MathHelper.ToRadians(pitch);

            float yawRadians = MathHelper.ToRadians(yaw);
            float pitchRadians = MathHelper.ToRadians(pitch);

            if (!float.IsFinite(yawRadians) || !float.IsFinite(pitchRadians))
            {
                Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Invalid radians");
                return;
            }

            if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Written: Yaw={lookOffset.YawOffset:F4} Pitch={lookOffset.PitchOffset:F4}");
        }

        #endregion Methods
    }
}
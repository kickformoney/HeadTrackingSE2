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

        public static bool logToFile = false;
        private static ThirdPersonCameraComponent instance = new();
        private static int logCounter = 0;
        private static bool loggingDisabledMessageShown = false;
        private static int noDataLogCounter = 0;

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
                    Log.Default.WriteLine($"[{Plugin.Name}] ThirdPersonCameraComponent.OnAddedToScene patched manually.");
                }
                else
                {
                    Log.Default.WriteLine($"[{Plugin.Name}] ThirdPersonCameraComponent.OnAddedToScene not found.");
                }

                var onRemovedFromScene = typeof(ThirdPersonCameraComponent)
                    .GetMethod("OnBeforeRemovedFromScene",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (onRemovedFromScene != null)
                {
                    var onRemovedPostfix = typeof(HeadTrackingShipThirdPersonPatch)
                        .GetMethod("OnBeforeRemovedFromScenePostfix", BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(onRemovedFromScene, postfix: new HarmonyMethod(onRemovedPostfix));
                    Log.Default.WriteLine($"[{Plugin.Name}] ThirdPersonCameraComponent.OnBeforeRemovedFromScene patched manually.");
                }
                else
                {
                    Log.Default.WriteLine($"[{Plugin.Name}] ThirdPersonCameraComponent.OnBeforeRemovedFromScenePostfix not found.");
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

                    Log.Default.WriteLine($"[{Plugin.Name}] ThirdPersonCameraComponent.UpdateRelativeTransform patched manually. Prefix={prefix != null} Postfix={postfix != null}");
                }
                else
                {
                    Log.Default.WriteLine($"[{Plugin.Name}] ThirdPersonCameraComponent.UpdateRelativeTransform not found.");
                }
            }
            catch (Exception e)
            {
                Log.Default.WriteLine($"[{Plugin.Name}] Failed to patch HeadTrackingShipThirdPersonPatch: {e}");
            }
        }

        private static void OnAddedToScenePrefix(ThirdPersonCameraComponent __instance)
        {
            // use a private instance field to store the reference to avoid issues when calling it up in Prefix
            instance = __instance;

            if (!__instance._topLevelParent.Data.Has<LookOffsetData>())
            {
                __instance._topLevelParent.Data.Set(new LookOffsetData());
                Log.Default.WriteLine($"[{Plugin.Name}] Third Person: added LookOffsetData before OnAddedToScene.");
            }
        }

        private static void Prefix(ThirdPersonCameraComponent __instance, ref ThirdPersonCameraData cameraData)
        {
            logToFile = Config.Current.OnFootLogging && (logCounter++ % 180) == 0;

            // Guard against calls during initialization before the component is fully set up
            if (instance == null || instance._topLevelParent == null) return;

            if (cameraData.InFirstPerson) return;
            if (!Config.Current.EnableExternalTracking) return;
            if (!OpenTrackReader.TryGetPose(out float yaw, out float pitch)) return;

            // Pre-set LookOffsetData with our values so UpdateLookOffsetRotation reads them
            var data = instance._topLevelParent.Data;
            if (!data.TryGet<LookOffsetData>(out _))
                data.Set(new LookOffsetData());

            ref LookOffsetData lookOffset = ref data.GetWritePtr<LookOffsetData>();

            yaw = Config.Current.InvertYaw ? -yaw : yaw;
            pitch = Config.Current.InvertPitch ? -pitch : pitch;

            lookOffset.YawOffset = MathHelper.ToRadians(yaw);
            lookOffset.PitchOffset = MathHelper.ToRadians(pitch);
        }

        private static void OnBeforeRemovedFromScenePostfix()
        {
            instance = null;
            Log.Default.WriteLine($"[{Plugin.Name}] Third Person: cleared instance.");
        }

        #endregion Methods
    }
}
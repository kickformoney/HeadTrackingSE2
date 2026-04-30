using ClientPlugin.Helpers;
using HarmonyLib;
using Keen.Game2.Client.WorldObjects.Character;
using Keen.Game2.Simulation.WorldObjects.Characters;
using Keen.VRage.Library.Diagnostics;
using Keen.VRage.Library.Mathematics;

namespace ClientPlugin.Patches
{
    internal class HeadTrackingOnFootPatch
    {
        #region Fields

        private static float lastPitch;
        private static float lastYaw;
        private static int logCounter = 0;
        private static bool loggingDisabledMessageShown = false;
        private static bool logToFile = false;
        private static int noDataLogCounter = 0;

        #endregion Fields

        #region Properties

        public static CharacterMovementInputHandlerComponent? Instance { get; private set; }

        #endregion Properties

        #region Methods

        public static void Update()
        {
            logToFile = Config.Current.OnFootLogging && (logCounter++ % 180) == 0;

            if (!Config.Current.EnableOnFootTracking)
            {
                if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] On-Foot Tracking Disabled - Skipping additional logging");
                return;
            }

            // Show the "Logging disabled" message only once when logging is turned off
            if (!Config.Current.OnFootLogging)
            {
                if (!loggingDisabledMessageShown)
                {
                    Log.Default.WriteLine($"[{Plugin.Name}] On-Foot logging disabled - no further log entries will be written for this patch until logging is enabled");
                    loggingDisabledMessageShown = true;
                }
            }
            else // reset flag if logging is enabled
            {
                loggingDisabledMessageShown = false;
            }

            var character = Instance;

            if (character == null)
            {
                if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] On-Foot: no character instance");
                return;
            }

            // Skip if seated in a cockpit or chair
            if (character._characterInput._movementComponent.CurrentMovementState == CharacterMovementState.Sitting)
            {
                if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] On-Foot: character is seated, skipping.");
                return;
            }

            var movementState = character._characterInput._movementComponent.CurrentMovementState;
            if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] On-Foot: MovementState={movementState}");

            if (OpenTrackReader.TryGetPose(out float yaw, out float pitch))
            {
                float yawDelta = yaw - lastYaw;
                float pitchDelta = pitch - lastPitch;
                lastYaw = yaw;
                lastPitch = pitch;

                float scale = Config.Current.OnFootSensitivity;

                float scaledYaw = yawDelta * scale;
                float scaledPitch = pitchDelta * scale;

                if (logToFile)
                {
                    Log.Default.WriteLine($"[{Plugin.Name}] On-Foot raw:     yaw={yaw:F2} pitch={pitch:F2}");
                    Log.Default.WriteLine($"[{Plugin.Name}] On-Foot delta:   yawDelta={yawDelta:F4} pitchDelta={pitchDelta:F4}");
                    Log.Default.WriteLine($"[{Plugin.Name}] On-Foot scaled:  scaledYaw={scaledYaw:F4} scaledPitch={scaledPitch:F4} scale={scale}");
                    Log.Default.WriteLine($"[{Plugin.Name}] On-Foot mouse before: {character._mouse}");
                }

                character._mouse.X -= scaledYaw;
                character._mouse.Y += scaledPitch;
                character.UpdateMovement();
                character._mouse = Vector2.Zero;

                if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] On-Foot mouse after reset: {character._mouse}");
            }
            else
            {
                int frames = logToFile ? 180 : 3600;
                if (noDataLogCounter++ % frames == 0)
                    Log.Default.WriteLine($"[{Plugin.Name}] On-Foot: no head tracking data");
            }
        }

        #endregion Methods

        #region Classes

        [HarmonyPatch(typeof(CharacterMovementInputHandlerComponent), "OnAddedToScene")]
        internal class CacheCharacterInputPatch
        {
            #region Methods

            private static void Postfix(CharacterMovementInputHandlerComponent __instance)
            {
                Instance = __instance;
                Log.Default.WriteLine($"[{Plugin.Name}] Cached CharacterMovementInputHandlerComponent");
            }

            #endregion Methods
        }

        [HarmonyPatch(typeof(CharacterMovementInputHandlerComponent), "OnBeforeRemovedFromScene")]
        internal class Patch_ClearCharacterInput
        {
            #region Methods

            private static void Postfix()
            {
                HeadTrackingOnFootPatch.Instance = null;
                Log.Default.WriteLine($"[{Plugin.Name}] Cleared CharacterMovementInputHandlerComponent");
            }

            #endregion Methods
        }

        #endregion Classes
    }
}
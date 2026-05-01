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

        private const string patchName = "On-Foot";

        private static bool characterSeatedMessageShown = false;
        private static float lastPitch;
        private static float lastYaw;
        private static int logCounter = 0;
        private static bool loggingDisabledMessageShown = false;
        private static bool logToFile = false;
        private static int noDataLogCounter = 0;
        private static bool trackingDisabledMessageShown = false;

        #endregion Fields

        #region Properties

        public static CharacterMovementInputHandlerComponent? Instance { get; private set; }

        #endregion Properties

        #region Methods

        public static void Update()
        {
            logToFile = Config.Current.OnFootLogging && (logCounter++ % 180) == 0;

            if (PatchLogger.IsDisabled(Plugin.Name, patchName, "Tracking", Config.Current.EnableOnFootTracking, ref trackingDisabledMessageShown)) return;
            PatchLogger.IsDisabled(Plugin.Name, patchName, "Logging", Config.Current.OnFootLogging, ref loggingDisabledMessageShown);

            var character = Instance;

            if (character == null)
            {
                if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] No character instance");
                return;
            }

            // Skip if seated in a cockpit or chair
            bool isSeated = character._characterInput._movementComponent.CurrentMovementState == CharacterMovementState.Sitting;

            if (isSeated)
            {
                if (!characterSeatedMessageShown)
                {
                    if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Character is seated, skipping.");
                    characterSeatedMessageShown = true;
                }
                return;
            }

            if (characterSeatedMessageShown)
            {
                if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Character is no longer seated, resuming head tracking.");
                characterSeatedMessageShown = false;
            }

            var movementState = character._characterInput._movementComponent.CurrentMovementState;
            if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] MovementState={movementState}");

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
                    Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Raw:     yaw={yaw:F2} pitch={pitch:F2}");
                    Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Delta:   yawDelta={yawDelta:F4} pitchDelta={pitchDelta:F4}");
                    Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Scaled:  scaledYaw={scaledYaw:F4} scaledPitch={scaledPitch:F4} scale={scale}");
                    Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Mouse before: {character._mouse}");
                }

                character._mouse.X -= scaledYaw;
                character._mouse.Y += scaledPitch;
                character.UpdateMovement();
                character._mouse = Vector2.Zero;

                if (logToFile) Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Mouse after reset: {character._mouse}");
            }
            else
            {
                int frames = logToFile ? 180 : 3600;
                if (noDataLogCounter++ % frames == 0)
                    Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] No head tracking data");
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
                Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Cached CharacterMovementInputHandlerComponent");
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
                Log.Default.WriteLine($"[{Plugin.Name}] [{patchName}] Cleared CharacterMovementInputHandlerComponent");
            }

            #endregion Methods
        }

        #endregion Classes
    }
}
using ClientPlugin.Helpers;
using ClientPlugin.Settings.Tools;
using HarmonyLib;
using Keen.Game2.Client.UI.InGame;
using Keen.VRage.Core.Game.Components;
using Keen.VRage.Core.Game.Systems;
using Keen.VRage.Core.Input;
using Keen.VRage.Input;
using Keen.VRage.Library.Diagnostics;
using Keen.VRage.Library.Mathematics;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ClientPlugin.Patches;

[HarmonyPatch(typeof(Session))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class KeybindPatch
{
    #region Fields

    private static bool cockpitDecreaseLast;
    private static bool cockpitIncreaseLast;
    private static bool cockpitToggleLast;
    private static bool decreasePressedLast;
    private static bool increasePressedLast;
    private static float lastPitch;
    private static float lastYaw;
    private static bool onFootDecreaseLast;
    private static bool onFootIncreaseLast;
    private static bool onFootToggleLast;
    private static bool thirdPersonDecreaseLast;
    private static bool thirdPersonIncreaseLast;
    private static bool thirdPersonToggleLast;
    private static bool togglePressedLast;

    #endregion Fields

    #region Enums

    private enum OperationEnum
    {
        Decrease,
        Increase,
    };

    #endregion Enums

    #region Methods

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Session.Update), typeof(bool))]
    public static void UpdatePostfix(Session __instance)
    {
        try
        {
            // On-foot head tracking — runs every frame
            var character = HeadTrackingOnFootPatch.Instance;

            // Always update OpenTrack values, regardless of session type
            if (character != null && Config.Current.OnFootEnabled)
            {
                if (OpenTrackReader.TryGetPose(out float yaw, out float pitch))
                {
                    float yawDelta = yaw - lastYaw;
                    float pitchDelta = pitch - lastPitch;
                    lastYaw = yaw;
                    lastPitch = pitch;

                    float scale = Config.Current.OnFootSensitivity; //* 0.35f;

                    float scaledYaw = yawDelta * scale;
                    float scaledPitch = pitchDelta * scale;

                    character._mouse.X -= scaledYaw;
                    character._mouse.Y += scaledPitch;
                    character.UpdateMovement();

                    character._mouse = Vector2.Zero;
                }
                else
                {
                    Log.Default.WriteLine($"[{Plugin.Name}] On-Foot: no OpenTrack data.");
                }
            }

            if (__instance.TryGet<SessionInGameUISessionComponent>() == null)
                return;

            var keyboard = __instance.TryGet<IInputManager>()?.Keyboard;
            if (keyboard == null) return;

            // Global toggle
            bool togglePressed = IsPressed(keyboard, Config.Current.GlobalToggleTracking);
            if (togglePressed && !togglePressedLast)
                Config.Current.EnableTracking = !Config.Current.EnableTracking;
            togglePressedLast = togglePressed;

            // Global sensitivity decrease - affects all tracking
            bool globalDecrease = IsPressed(keyboard, Config.Current.GlobalSensitivityDecrease);
            if (globalDecrease && !decreasePressedLast)
            {
                Config.Current.GlobalTrackingSensitivityMultiplier -= Config.Current.GlobalSensitivityStep;
                Config.Current.CockpitSensitivity = AdjustSensitivity(Config.Current.CockpitSensitivity, OperationEnum.Decrease, Config.Current.GlobalSensitivityStep);
                Config.Current.OnFootSensitivity = AdjustSensitivity(Config.Current.OnFootSensitivity, OperationEnum.Decrease, (Config.Current.GlobalSensitivityStep * 0.2f));
                //Config.Current.ThirdPersonSensitivity = AdjustSensitivity(Config.Current.ThirdPersonSensitivity, OperationEnum.Decrease, Config.Current.GlobalSensitivityStep);
            }
            decreasePressedLast = globalDecrease;

            // Global sensitivity increase - affects all tracking
            bool globalIncrease = IsPressed(keyboard, Config.Current.GlobalSensitivityIncrease);
            if (globalIncrease && !increasePressedLast)
            {
                Config.Current.GlobalTrackingSensitivityMultiplier += Config.Current.GlobalSensitivityStep;
                Config.Current.CockpitSensitivity = AdjustSensitivity(Config.Current.CockpitSensitivity, OperationEnum.Increase, Config.Current.GlobalSensitivityStep);
                Config.Current.OnFootSensitivity = AdjustSensitivity(Config.Current.OnFootSensitivity, OperationEnum.Increase, (Config.Current.GlobalSensitivityStep * 0.2f));
                //Config.Current.ThirdPersonSensitivity = AdjustSensitivity(Config.Current.ThirdPersonSensitivity, OperationEnum.Increase, Config.Current.GlobalSensitivityStep);
            }
            increasePressedLast = globalIncrease;

            // Mode-specific toggles
            bool cockpitTogglePressed = IsPressed(keyboard, Config.Current.CockpitToggle);
            if (cockpitTogglePressed && !cockpitToggleLast)
                Config.Current.CockpitEnabled = !Config.Current.CockpitEnabled;
            cockpitToggleLast = cockpitTogglePressed;

            bool onFootTogglePressed = IsPressed(keyboard, Config.Current.OnFootToggle);
            if (onFootTogglePressed && !onFootToggleLast)
                Config.Current.OnFootEnabled = !Config.Current.OnFootEnabled;
            onFootToggleLast = onFootTogglePressed;

            // Cockpit
            bool cockpitDecrease = IsPressed(keyboard, Config.Current.CockpitSensitivityDecrease);
            if (cockpitDecrease && !cockpitDecreaseLast)
                Config.Current.CockpitSensitivity = AdjustSensitivity(Config.Current.CockpitSensitivity, OperationEnum.Decrease, Config.Current.GlobalSensitivityStep);
            cockpitDecreaseLast = cockpitDecrease;

            bool cockpitIncrease = IsPressed(keyboard, Config.Current.CockpitSensitivityIncrease);
            if (cockpitIncrease && !cockpitIncreaseLast)
                Config.Current.CockpitSensitivity = AdjustSensitivity(Config.Current.CockpitSensitivity, OperationEnum.Increase, Config.Current.GlobalSensitivityStep, Config.CockpitSensitivityMax);
            cockpitIncreaseLast = cockpitIncrease;

            // First person - on foot
            bool onFootDecrease = IsPressed(keyboard, Config.Current.OnFootSensitivityDecrease);
            if (onFootDecrease && !onFootDecreaseLast)
                Config.Current.OnFootSensitivity = AdjustSensitivity(Config.Current.OnFootSensitivity, OperationEnum.Decrease, Config.Current.GlobalSensitivityStep);
            onFootDecreaseLast = onFootDecrease;

            bool onFootIncrease = IsPressed(keyboard, Config.Current.OnFootSensitivityIncrease);
            if (onFootIncrease && !onFootIncreaseLast)
                Config.Current.OnFootSensitivity = AdjustSensitivity(Config.Current.OnFootSensitivity, OperationEnum.Increase, Config.Current.GlobalSensitivityStep * 0.2f, Config.OnFootSensitivityMax);
            onFootIncreaseLast = onFootIncrease;

            // Reserved for potential future use
            //// Third person
            //bool thirdPersonTogglePressed = IsPressed(keyboard, Config.Current.ThirdPersonToggle);
            //if (thirdPersonTogglePressed && !thirdPersonToggleLast)
            //    Config.Current.ThirdPersonEnabled = !Config.Current.ThirdPersonEnabled;
            //thirdPersonToggleLast = thirdPersonTogglePressed;

            //bool thirdPersonIncrease = IsPressed(keyboard, Config.Current.ThirdPersonSensitivityIncrease);
            //if (thirdPersonIncrease && !thirdPersonIncreaseLast)
            //    Config.Current.ThirdPersonSensitivity = AdjustSensitivity(Config.Current.ThirdPersonSensitivity, true, Config.Current.GlobalSensitivityStep);
            //thirdPersonIncreaseLast = thirdPersonIncrease;

            //bool thirdPersonDecrease = IsPressed(keyboard, Config.Current.ThirdPersonSensitivityDecrease);
            //if (thirdPersonDecrease && !thirdPersonDecreaseLast)
            //    Config.Current.ThirdPersonSensitivity = AdjustSensitivity(Config.Current.ThirdPersonSensitivity, false, Config.Current.GlobalSensitivityStep);
            //thirdPersonDecreaseLast = thirdPersonDecrease;
            //}
        }
        catch (Exception e)
        {
            Log.Default.WriteLine($"[{Plugin.Name}] KeybindPatch failed: {e}");
        }
    }

    private static float AdjustSensitivity(float sensitivity, OperationEnum operation, float step, float max = 100f)
    {
        return operation == OperationEnum.Increase ? Math.Min(sensitivity + step, max) : Math.Max(sensitivity - step, 0f);
    }

    private static bool IsPressed(IInputDevice keyboard, Binding binding)
    {
        return binding.IsBound &&
            new DigitalInput(binding.Vk, GenericDeviceClass.Keyboard).IsActive(keyboard) &&
            KeyboardInputs.Control.IsActive(keyboard) == binding.Ctrl &&
            KeyboardInputs.Alt.IsActive(keyboard) == binding.Alt &&
            KeyboardInputs.Shift.IsActive(keyboard) == binding.Shift;
    }

    #endregion Methods
}
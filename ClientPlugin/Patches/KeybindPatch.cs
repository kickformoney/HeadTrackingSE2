using ClientPlugin.Settings.Tools;
using HarmonyLib;
using Keen.Game2.Client.UI.InGame;
using Keen.Game2.Simulation.WorldObjects.Movement;
using Keen.VRage.Core.Game.Components;
using Keen.VRage.Core.Game.Systems;
using Keen.VRage.Core.Input;
using Keen.VRage.Input;
using Keen.VRage.Library.Diagnostics;
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
    [HarmonyPatch(typeof(Session), nameof(Session.Update), typeof(bool))]
    public static void UpdatePostfix(Session __instance)
    {
        try
        {
            // Client session only — filters out the server session
            if (__instance.TryGet<SessionInGameUISessionComponent>() == null) return;

            HeadTrackingOnFootPatch.Update();

            // Actively remove LookOffsetData every frame when cockpit camera is active
            // to prevent third person patch from reading stale values
            if (HeadTrackingCockpitPatch.topLevelParent != null)
            {
                HeadTrackingCockpitPatch.topLevelParent.Data.TryRemove<LookOffsetData>();
            }

            if (__instance.TryGet<SessionInGameUISessionComponent>() == null) return;

            var keyboard = __instance.TryGet<IInputManager>()?.Keyboard;
            if (keyboard == null) return;

            // Global toggle
            if (KeyPressed(keyboard, Config.Current.GlobalToggleTracking, ref togglePressedLast))
                Config.Current.EnableTracking = !Config.Current.EnableTracking;

            // Global sensitivity - affects all tracking
            if (KeyPressed(keyboard, Config.Current.GlobalSensitivityDecrease, ref decreasePressedLast))
            {
                Config.Current.CockpitSensitivity = AdjustSensitivity(Config.Current.CockpitSensitivity, OperationEnum.Decrease, Config.Current.GlobalSensitivityStep);
                Config.Current.OnFootSensitivity = AdjustSensitivity(Config.Current.OnFootSensitivity, OperationEnum.Decrease, Config.Current.GlobalSensitivityStep * 0.2f);
                Config.Current.ThirdPersonSensitivity = AdjustSensitivity(Config.Current.ThirdPersonSensitivity, OperationEnum.Decrease, Config.Current.GlobalSensitivityStep);
            }

            if (KeyPressed(keyboard, Config.Current.GlobalSensitivityIncrease, ref increasePressedLast))
            {
                Config.Current.CockpitSensitivity = AdjustSensitivity(Config.Current.CockpitSensitivity, OperationEnum.Increase, Config.Current.GlobalSensitivityStep);
                Config.Current.OnFootSensitivity = AdjustSensitivity(Config.Current.OnFootSensitivity, OperationEnum.Increase, Config.Current.GlobalSensitivityStep * 0.2f);
                Config.Current.ThirdPersonSensitivity = AdjustSensitivity(Config.Current.ThirdPersonSensitivity, OperationEnum.Increase, Config.Current.GlobalSensitivityStep);
            }

            // Mode-specific toggles
            if (KeyPressed(keyboard, Config.Current.ToggleCockpit, ref cockpitToggleLast))
                Config.Current.EnableCockpitTracking = !Config.Current.EnableCockpitTracking;

            if (KeyPressed(keyboard, Config.Current.ToggleOnFoot, ref onFootToggleLast))
                Config.Current.EnableOnFootTracking = !Config.Current.EnableOnFootTracking;

            if (KeyPressed(keyboard, Config.Current.ToggleExternalCamera, ref thirdPersonToggleLast))
                Config.Current.EnableExternalTracking = !Config.Current.EnableExternalTracking;

            // Cockpit sensitivity
            if (KeyPressed(keyboard, Config.Current.CockpitSensitivityDecrease, ref cockpitDecreaseLast))
                Config.Current.CockpitSensitivity = AdjustSensitivity(Config.Current.CockpitSensitivity, OperationEnum.Decrease, Config.Current.GlobalSensitivityStep);

            if (KeyPressed(keyboard, Config.Current.CockpitSensitivityIncrease, ref cockpitIncreaseLast))
                Config.Current.CockpitSensitivity = AdjustSensitivity(Config.Current.CockpitSensitivity, OperationEnum.Increase, Config.Current.GlobalSensitivityStep, Config.CockpitSensitivityMax);

            // On-foot sensitivity
            if (KeyPressed(keyboard, Config.Current.OnFootSensitivityDecrease, ref onFootDecreaseLast))
                Config.Current.OnFootSensitivity = AdjustSensitivity(Config.Current.OnFootSensitivity, OperationEnum.Decrease, Config.Current.GlobalSensitivityStep);

            if (KeyPressed(keyboard, Config.Current.OnFootSensitivityIncrease, ref onFootIncreaseLast))
                Config.Current.OnFootSensitivity = AdjustSensitivity(Config.Current.OnFootSensitivity, OperationEnum.Increase, Config.Current.GlobalSensitivityStep * 0.2f, Config.OnFootSensitivityMax);

            // External cam sensitivity
            if (KeyPressed(keyboard, Config.Current.ThirdPersonSensitivityDecrease, ref thirdPersonDecreaseLast))
                Config.Current.ThirdPersonSensitivity = AdjustSensitivity(Config.Current.ThirdPersonSensitivity, OperationEnum.Decrease, Config.Current.GlobalSensitivityStep);

            if (KeyPressed(keyboard, Config.Current.ThirdPersonSensitivityIncrease, ref thirdPersonIncreaseLast))
                Config.Current.ThirdPersonSensitivity = AdjustSensitivity(Config.Current.ThirdPersonSensitivity, OperationEnum.Increase, Config.Current.GlobalSensitivityStep, Config.ThirdPersonSensitivityMax);
        }
        catch (Exception ex)
        {
            Log.Default.WriteLine($"[{Plugin.Name}] KeybindPatch failed: {ex}");
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

    /// <summary>
    /// Returns true only on the first frame a key binding is pressed, not while held
    /// Updates the last pressed state - if just pressed this frame, returns true
    /// </summary>
    private static bool KeyPressed(IInputDevice keyboard, Binding binding, ref bool last)
    {
        bool pressed = IsPressed(keyboard, binding);
        bool justPressed = pressed && !last;
        last = pressed;
        return justPressed;
    }

    #endregion Methods
}
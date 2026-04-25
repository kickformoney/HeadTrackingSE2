using ClientPlugin;
using ClientPlugin.Settings.Tools;
using HarmonyLib;
using Keen.Game2.Client.UI.InGame;
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

    private static bool _decreasePressedLast;
    private static bool _increasePressedLast;
    private static bool _togglePressedLast;

    #endregion Fields

    #region Methods

    public static void AdjustTrackingSensitivity(bool isIncrease, float incByVal)
    {
        if (isIncrease)
            Config.Current.Multiplier = Math.Min(Config.Current.Multiplier + incByVal, 100f);
        else
            Config.Current.Multiplier = Math.Max(Config.Current.Multiplier - incByVal, 0f);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Session.Update), typeof(bool))]
    public static void UpdatePostfix(Session __instance)
    {
        try
        {
            // Client session only
            if (__instance.TryGet<SessionInGameUISessionComponent>() == null)
                return;

            //if (!Config.Current.Enabled)
            //    return;

            var keyboard = __instance.TryGet<IInputManager>()?.Keyboard;
            if (keyboard == null)
                return;

            bool togglePressed = IsPressed(keyboard, Config.Current.Toggle);
            bool increasePressed = IsPressed(keyboard, Config.Current.IncreaseSensitivity);
            bool decreasePressed = IsPressed(keyboard, Config.Current.DecreaseSensitivity);

            if (togglePressed && !_togglePressedLast)
                Config.Current.Enabled = !Config.Current.Enabled;
            _togglePressedLast = togglePressed;

            if (increasePressed && !_increasePressedLast)
                AdjustTrackingSensitivity(true, Config.Current.SensitivityStep);
            _increasePressedLast = increasePressed;

            if (decreasePressed && !_decreasePressedLast)
                AdjustTrackingSensitivity(false, Config.Current.SensitivityStep);
            _decreasePressedLast = decreasePressed;
        }
        catch (Exception e)
        {
            Log.Default.WriteLine($"[{Plugin.Name}] KeybindPatch failed: {e}");
        }
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
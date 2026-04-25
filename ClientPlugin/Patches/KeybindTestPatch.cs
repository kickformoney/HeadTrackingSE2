using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Keen.Game2.Client.UI.InGame;
using Keen.Game2.Client.UI.Shared.SystemNotification;
using Keen.VRage.Core.Game.Components;
using Keen.VRage.Core.Game.Systems;
using Keen.VRage.Core.Input;
using Keen.VRage.Input;
using Keen.VRage.Library.Diagnostics;

namespace ClientPlugin.Patches;

// Example patch: shows an in-game toast notification whenever the configured
// Keybind is pressed. Delete this class once you no longer need the example.
//
// Session.Update runs every frame for both the client and server sessions. We
// filter to the client session by probing for SessionInGameUISessionComponent,
// which only exists on the client side.
//
// Scope: the keybind is only observed while a world is loaded, because
// Session.Update does not tick in the main menu and DisplayNotification is
// suppressed while the in-game (Esc) menu is open. That matches the most
// common use case — gameplay hotkeys — but if you need a keybind that works
// in menus too, hook an earlier frame-update instead (e.g. a patch on the
// client engine's input or UI component).
//
// The game does have a first-class input-action system (InputContext /
// InputActionDefinition / ActionInputProcessor), but it is content-driven:
// the ActionControlMapping is loaded from XML and SetMapping() replaces it
// wholesale, so injecting a plugin-defined action is intrusive. Polling the
// keyboard device each tick is the pragmatic idiomatic approach for a plugin.
// ReSharper disable once UnusedType.Global
[HarmonyPatch(typeof(Session))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class KeybindTestPatch
{
    // Edge detection so we fire once per press, not every frame the key is held.
    private static bool wasPressed;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Session.Update), typeof(bool))]
    public static void UpdatePostfix(Session __instance)
    {
        try
        {
            var inGameUI = __instance.TryGet<SessionInGameUISessionComponent>();
            if (inGameUI == null)
                return;

            var config = Config.Current;
            if (!config.Enabled)
                return;

            var binding = config.Keybind;
            if (!binding.IsBound)
            {
                wasPressed = false;
                return;
            }

            var keyboard = __instance.TryGet<IInputManager>()?.Keyboard;
            if (keyboard == null)
                return;

            var mainKey = new DigitalInput(binding.Vk, GenericDeviceClass.Keyboard);
            var pressed =
                mainKey.IsActive(keyboard) &&
                KeyboardInputs.Control.IsActive(keyboard) == binding.Ctrl &&
                KeyboardInputs.Alt.IsActive(keyboard) == binding.Alt &&
                KeyboardInputs.Shift.IsActive(keyboard) == binding.Shift;

            if (pressed && !wasPressed)
            {
                var notification = new NotificationViewModel(
                    title: Plugin.Name,
                    content: $"Keybind pressed: {binding}",
                    timeout: TimeSpan.FromSeconds(3));
                inGameUI.DisplayNotification(notification);
                Log.Default.WriteLine($"[{Plugin.Name}] Keybind pressed: {binding}");
            }

            wasPressed = pressed;
        }
        catch (Exception e)
        {
            Log.Default.WriteLine(LogSeverity.Error, $"[{Plugin.Name}] KeybindTestPatch failed: {e}");
        }
    }
}

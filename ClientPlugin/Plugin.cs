using ClientPlugin.Patches;
using ClientPlugin.Settings;
using ClientPlugin.Tools;
using HarmonyLib;
using Keen.Game2.Game.Plugins;
using Keen.VRage.Library.Diagnostics;
using System;
using System.Linq;
using System.Reflection;

namespace ClientPlugin;

public class Plugin : IPlugin
{
    public const string Name = "HeadTrackingSE2";
    public static Plugin Instance;

    // The data directory will be provided by a proper SDK in the future.
    // This static function is currently injected by Pulsar, which will
    // remain compatible, even after the SDK's release.
#pragma warning disable CS0649 // This field is assigned by Pulsar
    private static Func<string, string, string> GetConfigPath;
#pragma warning restore CS0649
    public string DataDir { get; private set; } = GetConfigPath(Name, null);

    public Plugin()
    {
        Instance = this;
        _ = Config.Current;

        Log.Default.WriteLine($"[{Name}] Loaded plugin");

#if DEBUG
        Harmony.DEBUG = true;
#endif
        var harmony = new Harmony(Name);

        harmony.PatchAll(Assembly.GetExecutingAssembly());
        HeadTrackingCockpitPatch.Register(harmony);
        HeadTrackingShipThirdPersonPatch.Register(harmony);

        // Log patched methods for debugging
        var patchedMethods = harmony.GetPatchedMethods().ToList();

        Log.Default.WriteLine($"[{Plugin.Name}] Patched methods count: {patchedMethods.Count}");

        foreach (var m in patchedMethods)
        {
            Log.Default.WriteLine($"[{Plugin.Name}] Patched: {m.DeclaringType?.Name}.{m.Name}");
        }

        // Log active configuration
        Log.Default.WriteLine($"[{Name}] Configuration loaded:");
        Log.Default.WriteLine($"[{Name}]   Cockpit tracking:        {(Config.Current.EnableCockpitTracking ? "Enabled" : "Disabled")} | Sensitivity: {Config.Current.CockpitSensitivity} | Logging: {(Config.Current.CockpitLogging ? "Enabled" : "Disabled")}");
        Log.Default.WriteLine($"[{Name}]   On-foot tracking:        {(Config.Current.EnableOnFootTracking ? "Enabled" : "Disabled")} | Sensitivity: {Config.Current.OnFootSensitivity}  | Logging: {(Config.Current.OnFootLogging ? "Enabled" : "Disabled")}");
        Log.Default.WriteLine($"[{Name}]   Third-person tracking:   {(Config.Current.EnableExternalTracking ? "Enabled" : "Disabled")} | Sensitivity: {Config.Current.ThirdPersonSensitivity} | Logging: {(Config.Current.ExternalViewLogging ? "Enabled" : "Disabled")}");
        Log.Default.WriteLine($"[{Name}]   Global sensitivity step: {Config.Current.GlobalSensitivityStep}");
    }

    // Invoked by Pulsar via reflection when the user clicks the plugin's config button.
    public void OpenConfigDialog()
    {
        try
        {
            var sharedUi = GameAccess.GetSharedUI();
            if (sharedUi == null)
            {
                Log.Default.WriteLine(LogSeverity.Warning, $"[{Name}] SharedUIComponent not available");
                return;
            }

            var generator = new SettingsGenerator();
            var viewModel = new SettingsScreenViewModel(
                generator.Title,
                panel => generator.PopulateContent(panel),
                () => ConfigStorage.Save(Config.Current));

            sharedUi.CreateScreen<SettingsScreen>(viewModel, showCursor: true);
        }
        catch (Exception e)
        {
            Log.Default.WriteLine(LogSeverity.Error, $"[{Name}] OpenConfigDialog failed: {e}");
        }
    }
}
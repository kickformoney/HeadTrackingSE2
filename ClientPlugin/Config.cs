using ClientPlugin.Settings.Elements;
using ClientPlugin.Settings.Tools;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace ClientPlugin;

public class Config : INotifyPropertyChanged
{
    #region Fields

    public const float CockpitSensitivityMax = 100f;
    public const float OnFootSensitivityMax = 20f;
    public const float ThirdPersonSensitivityMax = 100f;
    public static readonly Config Default = new Config();
    public static Config Current = ConfigStorage.Load();
    private bool enableTracking = true;

    #endregion Fields

    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    #region Properties

    [XmlIgnore]
    public readonly string Title = "Head Tracking Configuration";

    [Separator("Global Settings")]
    [Checkbox(description: "Enable/Disable Plugin")]
    public bool EnableTracking
    {
        get => enableTracking;
        set => SetField(ref enableTracking, value);
    }

    [Keybind(description: "Decrease Global Sensitivity - Affects overall tracking sensitivity")]
    public Binding GlobalSensitivityDecrease { get; set; } = new Binding();

    [Keybind(description: "Increase Global Sensitivity - Affects overall tracking sensitivity")]
    public Binding GlobalSensitivityIncrease { get; set; } = new Binding();

    [Slider(0.25f, 5f, 0.25f, SliderAttribute.SliderType.Float, description: "Sensitivity Adjustment Increment - Default: 0.25")]
    public float GlobalSensitivityStep { get; set; } = 1.0f;

    [Keybind(description: "Toggle Tracking")]
    public Binding GlobalToggleTracking { get; set; } = new Binding();

    [Slider(0f, 100f, 1f, SliderAttribute.SliderType.Integer, description: "Global Sensitivity Multiplier - Default: 35")]
    public float GlobalTrackingSensitivityMultiplier { get; set; } = 35f;

    //[Checkbox(description: "Use Individual Hotkeys")]
    //public bool UseIndividualHotkeys { get; set; } = false;

    #region Cockpit

    [Separator("Cockpit")]
    [Checkbox(description: "Enable Cockpit Head Tracking")]
    public bool EnableCockpitTracking { get; set; } = true;

    [Slider(0f, CockpitSensitivityMax, 1f, SliderAttribute.SliderType.Integer, description: "Cockpit Sensitivity - Default: 35")]
    public float CockpitSensitivity { get; set; } = 35f;

    [Keybind(description: "Decrease Sensitivity")]
    public Binding CockpitSensitivityDecrease { get; set; } = new Binding();

    [Keybind(description: "Increase Sensitivity")]
    public Binding CockpitSensitivityIncrease { get; set; } = new Binding();

    [Keybind(description: "Toggle Cockpit Head Tracking")]
    public Binding ToggleCockpit { get; set; } = new Binding();

    #endregion Cockpit

    #region On Foot - First Person

    [Separator("On Foot")]
    [Checkbox(description: "Enable On-Foot Head Tracking")]
    public bool EnableOnFootTracking { get; set; } = false;

    [Slider(0f, OnFootSensitivityMax, 0.25f, SliderAttribute.SliderType.Integer, description: "On-Foot Sensitivity - Default: 10")]
    public float OnFootSensitivity { get; set; } = 10f;

    [Keybind(description: "Decrease Sensitivity")]
    public Binding OnFootSensitivityDecrease { get; set; } = new Binding();

    [Keybind(description: "Increase Sensitivity")]
    public Binding OnFootSensitivityIncrease { get; set; } = new Binding();

    [Keybind(description: "Toggle On-Foot Head Tracking")]
    public Binding ToggleOnFoot { get; set; } = new Binding();

    #endregion On Foot - First Person

    #region Ship - External Camera

    [Separator("Ship External Camera*")]
    [TextBlock(description: "*Note: Alt+mouse camera movement disabled while active")]
    public string TrackingNote { get; set; }

    [Checkbox(description: "Enable External Camera Head Tracking")]
    public bool EnableExternalTracking { get; set; } = false;

    [Slider(0f, ThirdPersonSensitivityMax, 1f, SliderAttribute.SliderType.Integer, description: "External View Sensitivity - Default: 50")]
    public float ThirdPersonSensitivity { get; set; } = 50f;

    [Keybind(description: "Decrease Sensitivity")]
    public Binding ThirdPersonSensitivityDecrease { get; set; } = new Binding();

    [Keybind(description: "Increase Sensitivity")]
    public Binding ThirdPersonSensitivityIncrease { get; set; } = new Binding();

    [Checkbox(description: "Invert Pitch (Y-axis) in the third person external view")]
    public bool InvertPitch { get; set; } = false;

    [Checkbox(description: "Invert Yaw (X-axis) in the third person external view")]
    public bool InvertYaw { get; set; } = false;

    [Keybind(description: "Toggle External View Head Tracking")]
    public Binding ToggleExternalCamera { get; set; } = new Binding();

    #endregion Ship - External Camera

    #region Debugging Section

    [Separator("Debugging")]
    [Checkbox(description: @"Enable if requested - Logs to %AppData%\SpaceEngineers2\Temp\Logs")]
    public bool OnFootLogging { get; set; } = false;

    [Checkbox(description: @"Enable if requested - logs to %AppData%\SpaceEngineers2\Temp\Logs")]
    public bool CockpitLogging { get; set; } = false;

    [Checkbox(description: @"Enable if requested - Logs to %AppData%\SpaceEngineers2\Temp\Logs")]
    public bool ExternalViewLogging { get; set; } = false;

    #endregion Debugging Section

    #endregion Properties

    #region Methods

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion Methods
}
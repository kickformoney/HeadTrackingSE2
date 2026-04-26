using ClientPlugin.Settings.Elements;
using ClientPlugin.Settings.Tools;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace ClientPlugin;

//public enum ProtocolEnum
//{
//    FaceTrackNoIR,
//    FreeTrack,
//}

public class Config : INotifyPropertyChanged
{
    #region Fields

    public static readonly Config Default = new Config();
    public static Config Current = ConfigStorage.Load();

    [XmlIgnore]
    public readonly string Title = "Head Tracking Configuration";

    private bool enabled = true;
    private float trackingSensitivity = 35f;
    private float sensitivityStep = 1f;

    //private ProtocolEnum protocolSelection = ProtocolEnum.FreeTrack;

    private Binding toggleKey = new Binding();
    private Binding increaseSensitivity = new Binding();
    private Binding decreaseSensitivity = new Binding();

    #endregion Fields

    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    #region Properties

    [Separator("Head Tracking Settings")]
    [Checkbox(description: "Enable/Disable Plugin")]
    public bool Enabled
    {
        get => enabled;
        set => SetField(ref enabled, value);
    }

    //[Dropdown(description: "Tracking Protocol")]
    //public ProtocolEnum ProtocolSelection
    //{
    //    get => protocolSelection;
    //    set => SetField(ref protocolSelection, value);
    //}

    [Slider(0f, 100f, 1f, SliderAttribute.SliderType.Integer, description: "Tracking Sensitivity")]
    public float TrackingSensitivity
    {
        get => trackingSensitivity;
        set => SetField(ref trackingSensitivity, value);
    }

    [Separator("Keybinds")]
    [Keybind(description: "Toggle Tracking")]
    public Binding Toggle
    {
        get => toggleKey;
        set => SetField(ref toggleKey, value);
    }

    [Slider(0.25f, 5f, 0.25f, SliderAttribute.SliderType.Float, description: "Sensitivity Adjustment Increment")]
    public float SensitivityStep
    {
        get => sensitivityStep;
        set => SetField(ref sensitivityStep, value);
    }

    [Keybind(description: "Increase Tracking Sensitivity")]
    public Binding IncreaseSensitivity
    {
        get => increaseSensitivity;
        set => SetField(ref increaseSensitivity, value);
    }

    [Keybind(description: "Decrease Tracking Sensitivity")]
    public Binding DecreaseSensitivity
    {
        get => decreaseSensitivity;
        set => SetField(ref decreaseSensitivity, value);
    }

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
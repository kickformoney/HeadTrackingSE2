using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Avalonia.Media;
using ClientPlugin.Settings.Elements;
using ClientPlugin.Settings.Tools;

namespace ClientPlugin;

public enum ExampleEnum
{
    FirstAlpha,
    SecondBeta,
    ThirdGamma,
    AndTheDelta,
    Epsilon,
}

public class Config : INotifyPropertyChanged
{
    #region Options

    private bool enabled = true;
    private bool toggle = true;
    private int integer = 2;
    private float number = 0.1f;
    private string text = "Default Text";
    private ExampleEnum dropdown = ExampleEnum.FirstAlpha;
    private uint color = 0xFF00FFFFu;           // ARGB packed, cyan
    private uint colorWithAlpha = 0x80CC9933u;  // ARGB packed, 50% alpha orange-ish
    private Binding keybind = new Binding();

    #endregion

    #region User interface

    [XmlIgnore]
    public readonly string Title = "Config Demo";

    [Separator("Some settings")]

    [Checkbox(description: "Enable or disable the plugin's features")]
    public bool Enabled
    {
        get => enabled;
        set => SetField(ref enabled, value);
    }

    [Checkbox(description: "Checkbox Tooltip")]
    public bool Toggle
    {
        get => toggle;
        set => SetField(ref toggle, value);
    }

    [Slider(-1f, 10f, 1f, SliderAttribute.SliderType.Integer, description: "Integer Slider Tooltip")]
    public int Integer
    {
        get => integer;
        set => SetField(ref integer, value);
    }

    [Slider(-5f, 4.5f, 0.5f, SliderAttribute.SliderType.Float, description: "Float Slider Tooltip")]
    public float Number
    {
        get => number;
        set => SetField(ref number, value);
    }

    [Textbox(description: "Textbox Tooltip")]
    public string Text
    {
        get => text;
        set => SetField(ref text, value);
    }

    [Dropdown(description: "Dropdown Tooltip")]
    public ExampleEnum Dropdown
    {
        get => dropdown;
        set => SetField(ref dropdown, value);
    }

    [Separator("More settings")]

    [XmlIgnore]
    [Color(description: "RGB color")]
    public Color Color
    {
        get => Avalonia.Media.Color.FromUInt32(color | 0xFF000000u);
        set => SetField(ref color, value.ToUInt32() | 0xFF000000u);
    }

    [XmlIgnore]
    [Color(hasAlpha: true, description: "RGBA color")]
    public Color ColorWithAlpha
    {
        get => Avalonia.Media.Color.FromUInt32(colorWithAlpha);
        set => SetField(ref colorWithAlpha, value.ToUInt32());
    }

    [Keybind(description: "Keybind Tooltip - Unbind by right clicking the button")]
    public Binding Keybind
    {
        get => keybind;
        set => SetField(ref keybind, value);
    }

    [Button(description: "Button Tooltip")]
    public void Button()
    {
        // TODO: Put your custom button action here.
    }

    #endregion

    #region Serialization-only properties

    public uint ColorPacked
    {
        get => color;
        set => color = value;
    }

    public uint ColorWithAlphaPacked
    {
        get => colorWithAlpha;
        set => colorWithAlpha = value;
    }

    #endregion

    #region Property change notification boilerplate

    public static readonly Config Default = new Config();
    public static Config Current = ConfigStorage.Load();

    public event PropertyChangedEventHandler PropertyChanged;

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

    #endregion
}

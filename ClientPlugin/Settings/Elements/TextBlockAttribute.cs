using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace ClientPlugin.Settings.Elements;

[AttributeUsage(AttributeTargets.Property)]
internal class TextBlockAttribute : Attribute, IElement
{
    public readonly string Label;
    public readonly string Description;

    public TextBlockAttribute(string label = null, string description = null)
    {
        Label = label;
        Description = description;
    }

    public Control BuildRow(string name, Func<object> getter, Action<object> setter)
    {
        return RowBuilder.NewRowNoLabel(Description ?? name);
    }

    public List<Type> SupportedTypes { get; } = new() { typeof(string) };
}
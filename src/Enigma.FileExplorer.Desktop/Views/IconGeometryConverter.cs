using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// Turns a bound Phosphor icon into the geometry an Enigma card's <c>IconData</c> takes; the
/// <c>{ei:IconGeometry}</c> markup extension can only name a fixed icon.
/// </summary>
public sealed class IconGeometryConverter : IValueConverter
{
    /// <summary>
    /// The shared instance, for <c>{x:Static}</c>.
    /// </summary>
    public static IconGeometryConverter Instance { get; } = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is PhosphorIcon icon ? PhosphorIconSet.Instance.GetGlyph(icon, PhosphorWeight.Regular).ToGeometry() : null;

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("An icon cannot be read back from its geometry.");
}

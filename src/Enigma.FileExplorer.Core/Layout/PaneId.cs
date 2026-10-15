using System.Globalization;

namespace Enigma.FileExplorer.Core.Layout;

/// <summary>
/// Identifies a pane within a <see cref="LayoutTree"/>.
/// </summary>
/// <param name="Value">The identifier's value, unique within its tree.</param>
public readonly record struct PaneId(int Value)
{
    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

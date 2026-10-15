namespace Enigma.FileExplorer.Core.Layout;

/// <summary>
/// A layout as plain data, for persisting it: either a pane, or a split with both sides.
/// </summary>
/// <param name="Pane">The pane's id, for a leaf.</param>
/// <param name="Axis">How the split divides its space, for a split.</param>
/// <param name="Ratio">The first side's share, for a split.</param>
/// <param name="First">The left or top side, for a split.</param>
/// <param name="Second">The right or bottom side, for a split.</param>
public sealed record LayoutSnapshot(
    int? Pane = null,
    SplitAxis? Axis = null,
    double? Ratio = null,
    LayoutSnapshot? First = null,
    LayoutSnapshot? Second = null);

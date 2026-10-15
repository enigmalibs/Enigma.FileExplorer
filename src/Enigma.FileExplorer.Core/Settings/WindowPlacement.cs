namespace Enigma.FileExplorer.Core.Settings;

/// <summary>
/// Where the main window was: its normal (not maximized) bounds, and whether it was maximized.
/// </summary>
public sealed record WindowPlacement
{
    /// <summary>
    /// The left edge, in screen pixels.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// The top edge, in screen pixels.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// The width, in device-independent pixels.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// The height, in device-independent pixels.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Whether the window was maximized; the bounds are then those it returns to.
    /// </summary>
    public bool IsMaximized { get; set; }
}

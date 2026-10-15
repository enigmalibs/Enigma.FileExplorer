namespace Enigma.FileExplorer.Core.Layout;

/// <summary>
/// Where a pane sits in the window, as fractions of the whole (0 to 1 on both axes).
/// </summary>
/// <param name="X">The left edge.</param>
/// <param name="Y">The top edge.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct LayoutRect(double X, double Y, double Width, double Height)
{
    /// <summary>The right edge.</summary>
    public double Right => X + Width;

    /// <summary>The bottom edge.</summary>
    public double Bottom => Y + Height;

    /// <summary>The horizontal centre.</summary>
    public double CenterX => X + (Width / 2);

    /// <summary>The vertical centre.</summary>
    public double CenterY => Y + (Height / 2);
}

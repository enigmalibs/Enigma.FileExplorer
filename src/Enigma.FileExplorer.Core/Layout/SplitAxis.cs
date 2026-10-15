namespace Enigma.FileExplorer.Core.Layout;

/// <summary>
/// How a split divides its space.
/// </summary>
public enum SplitAxis
{
    /// <summary>
    /// Side by side, divided by a vertical bar: vim's <c>:vsplit</c>.
    /// </summary>
    Columns,

    /// <summary>
    /// One above the other, divided by a horizontal bar: vim's <c>:split</c>.
    /// </summary>
    Rows,
}

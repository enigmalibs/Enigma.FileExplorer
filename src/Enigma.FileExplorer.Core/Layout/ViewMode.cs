namespace Enigma.FileExplorer.Core.Layout;

/// <summary>
/// How a pane presents its entries.
/// </summary>
public enum ViewMode
{
    /// <summary>
    /// One row per entry with name, date, type and size columns.
    /// </summary>
    Details,

    /// <summary>
    /// A grid of medium tiles.
    /// </summary>
    MediumIcons,

    /// <summary>
    /// A grid of large tiles.
    /// </summary>
    LargeIcons,
}

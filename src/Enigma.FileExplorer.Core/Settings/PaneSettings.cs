using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;

namespace Enigma.FileExplorer.Core.Settings;

/// <summary>
/// How a pane shows its entries: a preference kept between runs, unlike where the pane is.
/// </summary>
public sealed record PaneSettings
{
    /// <summary>
    /// How it shows its entries.
    /// </summary>
    public ViewMode ViewMode { get; set; }

    /// <summary>
    /// What it sorts by.
    /// </summary>
    public SortKey SortKey { get; set; }

    /// <summary>
    /// Which way it sorts.
    /// </summary>
    public SortDirection SortDirection { get; set; }
}

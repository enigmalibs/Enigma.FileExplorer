namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// Whether a menu entry shows a state next to its title.
/// </summary>
public enum MenuToggle
{
    /// <summary>
    /// A plain entry.
    /// </summary>
    None,

    /// <summary>
    /// An on/off switch, shown with a check mark.
    /// </summary>
    Check,

    /// <summary>
    /// One choice among its siblings, shown with a dot.
    /// </summary>
    Radio,
}

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// What a mouse click on an entry means, decoupled from the platform's modifier keys.
/// </summary>
public enum ClickModifiers
{
    /// <summary>
    /// A plain click: move the cursor there and clear the marks.
    /// </summary>
    None,

    /// <summary>
    /// Ctrl (Cmd on macOS): toggle the entry's mark.
    /// </summary>
    Toggle,

    /// <summary>
    /// Shift: mark the range from the anchor to the entry.
    /// </summary>
    Range,
}

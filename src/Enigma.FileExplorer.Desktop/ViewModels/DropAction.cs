namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// What a drop of files does.
/// </summary>
public enum DropAction
{
    /// <summary>
    /// Nothing: the entries are already where they were dropped.
    /// </summary>
    None,

    /// <summary>
    /// Ask with the drop menu (Copy here, Move here, Cancel).
    /// </summary>
    Ask,

    /// <summary>
    /// Copy, without asking (Ctrl held).
    /// </summary>
    Copy,

    /// <summary>
    /// Move, without asking (Shift held).
    /// </summary>
    Move,
}

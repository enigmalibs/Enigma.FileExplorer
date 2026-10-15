namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// What the command line is being used for.
/// </summary>
public enum CommandLineMode
{
    /// <summary>
    /// Hidden.
    /// </summary>
    Closed,

    /// <summary>
    /// An ex command after <c>:</c>.
    /// </summary>
    Command,

    /// <summary>
    /// A quick-jump pattern after <c>/</c>.
    /// </summary>
    Jump,

    /// <summary>
    /// An answer to a prompt.
    /// </summary>
    Prompt,
}

namespace Enigma.FileExplorer.Core.Commands;

/// <summary>
/// What an ex command accepts after its name.
/// </summary>
public enum ExArgumentKind
{
    /// <summary>
    /// Nothing: trailing text is an error.
    /// </summary>
    None,

    /// <summary>
    /// Optional free text.
    /// </summary>
    Optional,

    /// <summary>
    /// Required free text.
    /// </summary>
    Required,

    /// <summary>
    /// An optional path, completed against the file system.
    /// </summary>
    Path,
}

namespace Enigma.FileExplorer.Core.Search;

/// <summary>
/// What a search looks at.
/// </summary>
public enum SearchKind
{
    /// <summary>
    /// Entry names (files and folders).
    /// </summary>
    Name,

    /// <summary>
    /// The text inside files.
    /// </summary>
    Content,
}

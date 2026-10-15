namespace Enigma.FileExplorer.Core.FileSystem;

/// <summary>
/// What a directory entry is.
/// </summary>
public enum EntryKind
{
    /// <summary>
    /// A directory, including a link or junction that points to one.
    /// </summary>
    Directory,

    /// <summary>
    /// Anything that is not a directory.
    /// </summary>
    File,
}

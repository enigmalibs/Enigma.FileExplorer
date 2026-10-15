namespace Enigma.FileExplorer.Core.FileSystem;

/// <summary>
/// What a listing is sorted by.
/// </summary>
public enum SortKey
{
    /// <summary>
    /// The entry's name, in natural order.
    /// </summary>
    Name,

    /// <summary>
    /// When the entry was last written.
    /// </summary>
    Modified,

    /// <summary>
    /// The size of a file.
    /// </summary>
    Size,

    /// <summary>
    /// The entry's type, as <see cref="FileTypes.Describe"/> names it.
    /// </summary>
    Type,
}

namespace Enigma.FileExplorer.Core.FileSystem;

/// <summary>
/// How a listing is sorted.
/// </summary>
/// <param name="Key">What to sort by.</param>
/// <param name="Direction">Which way.</param>
public readonly record struct EntrySort(SortKey Key, SortDirection Direction)
{
    /// <summary>
    /// By name, ascending: what a directory shows until the user chooses otherwise.
    /// </summary>
    public static EntrySort Default { get; } = new(SortKey.Name, SortDirection.Ascending);

    /// <summary>
    /// A key in the direction people want first: names and types A to Z, dates newest first, sizes
    /// largest first.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The sort.</returns>
    public static EntrySort For(SortKey key)
        => new(key, key is SortKey.Modified or SortKey.Size ? SortDirection.Descending : SortDirection.Ascending);

    /// <summary>
    /// The same key in the other direction.
    /// </summary>
    /// <returns>The reversed sort.</returns>
    public EntrySort Reversed()
        => this with { Direction = Direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending };
}

using System;
using System.Collections.Generic;

namespace Enigma.FileExplorer.Core.FileSystem;

/// <summary>
/// Sorts listings: directories always first, then by the chosen key, ties broken by name.
/// </summary>
public static class EntrySorter
{
    /// <summary>
    /// Creates the comparer for a sort.
    /// </summary>
    /// <param name="sort">The sort.</param>
    /// <returns>The comparer.</returns>
    public static IComparer<FileSystemEntry> CreateComparer(EntrySort sort) => new EntryComparer(sort);

    /// <summary>
    /// Sorts entries into a new list.
    /// </summary>
    /// <param name="entries">The entries.</param>
    /// <param name="sort">The sort.</param>
    /// <returns>The sorted entries.</returns>
    public static List<FileSystemEntry> Sort(IEnumerable<FileSystemEntry> entries, EntrySort sort)
    {
        ArgumentNullException.ThrowIfNull(entries);

        List<FileSystemEntry> sorted = [.. entries];
        sorted.Sort(CreateComparer(sort));
        return sorted;
    }

    private sealed class EntryComparer(EntrySort sort) : IComparer<FileSystemEntry>
    {
        public int Compare(FileSystemEntry? x, FileSystemEntry? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            // Directories lead whichever way the key runs: reversing a sort must not bury them.
            if (x.IsDirectory != y.IsDirectory)
            {
                return x.IsDirectory ? -1 : 1;
            }

            int byKey = sort.Key switch
            {
                SortKey.Modified => x.LastWriteTime.CompareTo(y.LastWriteTime),
                SortKey.Size => x.Size.CompareTo(y.Size),
                SortKey.Type => NaturalStringComparer.Instance.Compare(FileTypes.Describe(x), FileTypes.Describe(y)),
                _ => NaturalStringComparer.Instance.Compare(x.Name, y.Name),
            };

            if (byKey != 0)
            {
                return sort.Direction == SortDirection.Descending ? -byKey : byKey;
            }

            // Ties read alphabetically whatever the direction, so equal sizes or dates stay findable.
            return NaturalStringComparer.Instance.Compare(x.Name, y.Name);
        }
    }
}

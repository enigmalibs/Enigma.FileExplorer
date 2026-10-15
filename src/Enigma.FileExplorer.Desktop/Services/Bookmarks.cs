using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enigma.FileExplorer.Core.FileSystem;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// A bookmarked folder.
/// </summary>
/// <param name="Name">What it is listed as: the folder's name, or the whole path for a volume root.</param>
/// <param name="Path">The folder.</param>
public sealed record Bookmark(string Name, string Path);

/// <summary>
/// The user's bookmarked folders, in the order they were added. The session saves them whenever they change.
/// </summary>
public sealed class Bookmarks
{
    private readonly List<Bookmark> _items = [];

    /// <summary>
    /// Raised after a bookmark was added or removed (not when the saved ones are loaded).
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// The bookmarks, oldest first.
    /// </summary>
    public IReadOnlyList<Bookmark> Items => _items;

    /// <summary>
    /// The name a folder is bookmarked under: its own name, or the whole path for a volume root.
    /// </summary>
    /// <param name="path">The folder.</param>
    /// <returns>The name.</returns>
    public static string NameOf(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string normalized = PathUtilities.Normalize(path);
        string name = Path.GetFileName(normalized);
        return name.Length > 0 ? name : normalized;
    }

    /// <summary>
    /// Whether a folder is bookmarked.
    /// </summary>
    /// <param name="path">The folder.</param>
    /// <returns><see langword="true"/> when it is.</returns>
    public bool Contains(string path) => _items.Any(item => PathUtilities.AreSame(item.Path, path));

    /// <summary>
    /// Bookmarks a folder, last in the list.
    /// </summary>
    /// <param name="path">The folder.</param>
    /// <returns>The new bookmark, or <see langword="null"/> when the folder already is one.</returns>
    public Bookmark? Add(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Contains(path))
        {
            return null;
        }

        Bookmark bookmark = new(NameOf(path), PathUtilities.Normalize(path));
        _items.Add(bookmark);
        Changed?.Invoke(this, EventArgs.Empty);
        return bookmark;
    }

    /// <summary>
    /// Removes a bookmark.
    /// </summary>
    /// <param name="bookmark">The bookmark.</param>
    /// <returns><see langword="false"/> when it was not in the list.</returns>
    public bool Remove(Bookmark bookmark)
    {
        ArgumentNullException.ThrowIfNull(bookmark);

        if (!_items.Remove(bookmark))
        {
            return false;
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>
    /// Replaces the list with the saved bookmarks, at start. Blank and repeated folders are skipped.
    /// </summary>
    /// <param name="saved">The saved bookmarks.</param>
    public void Load(IEnumerable<Bookmark> saved)
    {
        ArgumentNullException.ThrowIfNull(saved);

        _items.Clear();

        foreach (Bookmark bookmark in saved)
        {
            if (!string.IsNullOrWhiteSpace(bookmark.Path) && !Contains(bookmark.Path))
            {
                _items.Add(bookmark.Name.Length > 0 ? bookmark : bookmark with { Name = NameOf(bookmark.Path) });
            }
        }
    }
}

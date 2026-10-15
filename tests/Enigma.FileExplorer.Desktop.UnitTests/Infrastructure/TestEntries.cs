using System;
using System.IO;
using Enigma.FileExplorer.Core.FileSystem;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Builds entries and paths for the in-memory file system.
/// </summary>
public static class TestEntries
{
    /// <summary>
    /// A root under which test paths live; it never exists on disk.
    /// </summary>
    public static string Root { get; } = Path.Combine(Path.GetPathRoot(Path.GetTempPath())!, "enigma-fx-fake");

    /// <summary>
    /// A path under <see cref="Root"/>.
    /// </summary>
    /// <param name="parts">The path's parts.</param>
    /// <returns>The absolute path.</returns>
    public static string PathOf(params string[] parts) => Path.Combine([Root, .. parts]);

    /// <summary>
    /// A file entry.
    /// </summary>
    /// <param name="directory">The directory it lives in.</param>
    /// <param name="name">Its name.</param>
    /// <param name="size">Its size.</param>
    /// <param name="hidden">Whether it is hidden; dot-files are hidden regardless.</param>
    /// <returns>The entry.</returns>
    public static FileSystemEntry File(string directory, string name, long size = 0, bool hidden = false)
        => new(name, Path.Combine(directory, name), EntryKind.File, size, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local), hidden || name.StartsWith('.'), IsSymbolicLink: false);

    /// <summary>
    /// A directory entry.
    /// </summary>
    /// <param name="directory">The directory it lives in.</param>
    /// <param name="name">Its name.</param>
    /// <param name="hidden">Whether it is hidden; dot-directories are hidden regardless.</param>
    /// <returns>The entry.</returns>
    public static FileSystemEntry Directory(string directory, string name, bool hidden = false)
        => new(name, Path.Combine(directory, name), EntryKind.Directory, 0, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local), hidden || name.StartsWith('.'), IsSymbolicLink: false);
}

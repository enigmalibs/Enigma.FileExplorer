using System;

namespace Enigma.FileExplorer.Core.FileSystem;

/// <summary>
/// Names an entry's type for people: what the Type column shows and what sorting by type uses, so the two
/// always agree.
/// </summary>
public static class FileTypes
{
    /// <summary>
    /// The description of a directory.
    /// </summary>
    public const string Folder = "Folder";

    /// <summary>
    /// The description of a file without an extension.
    /// </summary>
    public const string File = "File";

    /// <summary>
    /// Describes an entry: <c>Folder</c>, <c>PNG File</c>, or <c>File</c> for a file without an extension.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <returns>The description.</returns>
    public static string Describe(FileSystemEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.IsDirectory)
        {
            return Folder;
        }

        string extension = entry.Extension;
        return extension.Length > 1 ? $"{extension[1..].ToUpperInvariant()} {File}" : File;
    }
}

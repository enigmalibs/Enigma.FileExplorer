using System.Collections.Generic;

namespace Enigma.FileExplorer.Core.FileSystem;

/// <summary>
/// The entries of one directory, in the order the file system returned them.
/// </summary>
/// <param name="Path">The directory's normalized absolute path.</param>
/// <param name="Entries">Every entry, hidden ones included; filtering and sorting are the caller's choice.</param>
public sealed record DirectoryListing(string Path, IReadOnlyList<FileSystemEntry> Entries);

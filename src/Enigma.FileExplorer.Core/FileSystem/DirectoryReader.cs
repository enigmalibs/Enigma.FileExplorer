using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Threading;
using System.Threading.Tasks;
using IoEntry = System.IO.Enumeration.FileSystemEntry;

namespace Enigma.FileExplorer.Core.FileSystem;

/// <summary>
/// Reads directories from the local file system.
/// </summary>
public sealed class DirectoryReader : IDirectoryReader
{
    /// <inheritdoc />
    public Task<DirectoryListing> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // A large or remote directory can take seconds to enumerate; the caller is usually the UI thread.
        return Task.Run(() => Read(path, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Reads a directory synchronously.
    /// </summary>
    /// <param name="path">The directory to read.</param>
    /// <param name="cancellationToken">Cancels the read between entries.</param>
    /// <returns>The listing.</returns>
    internal static DirectoryListing Read(string path, CancellationToken cancellationToken)
    {
        string directory = PathUtilities.Normalize(path);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"The directory '{directory}' does not exist.");
        }

        EnumerationOptions options = new()
        {
            // Not recursive, so this only concerns the directory itself: an unreadable directory must
            // be reported, not shown as empty.
            IgnoreInaccessible = false,
            AttributesToSkip = 0,
            RecurseSubdirectories = false,
            ReturnSpecialDirectories = false,
        };

        FileSystemEnumerable<FileSystemEntry?> enumerable = new(directory, ToEntry, options);
        List<FileSystemEntry> entries = [];

        foreach (FileSystemEntry? entry in enumerable)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return new DirectoryListing(directory, entries);
    }

    private static FileSystemEntry? ToEntry(ref IoEntry entry)
    {
        try
        {
            string name = entry.FileName.ToString();
            FileAttributes attributes = entry.Attributes;
            bool isDirectory = entry.IsDirectory;

            return new FileSystemEntry(
                name,
                entry.ToFullPath(),
                isDirectory ? EntryKind.Directory : EntryKind.File,
                isDirectory ? 0 : entry.Length,
                entry.LastWriteTimeUtc.LocalDateTime,
                HiddenRules.IsHidden(name, attributes),
                (attributes & FileAttributes.ReparsePoint) != 0);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The entry vanished or became unreadable between the directory read and its stat. It is
            // not part of the directory any more, so leaving it out is the truthful listing.
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Operations;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A trash over the in-memory file system: trashed entries vanish from their directory.
/// </summary>
/// <param name="reader">The in-memory file system.</param>
public sealed class FakeTrash(FakeDirectoryReader reader) : ITrash
{
    /// <summary>
    /// Paths that have no trash (as on a network drive).
    /// </summary>
    public HashSet<string> NoTrash { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Paths whose trashing fails.
    /// </summary>
    public HashSet<string> FailOn { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Every path trashed, in order.
    /// </summary>
    public List<string> Trashed { get; } = [];

    /// <inheritdoc />
    public bool CanTrash(string path) => !NoTrash.Contains(path);

    /// <inheritdoc />
    public void MoveToTrash(string path)
    {
        if (FailOn.Contains(path))
        {
            throw new FileOperationException($"'{Path.GetFileName(path)}' could not be moved to the trash");
        }

        FileSystemEntry entry = reader.Find(path) ?? throw new FileOperationException($"'{Path.GetFileName(path)}' does not exist");
        reader.EntriesOf(Path.GetDirectoryName(entry.FullPath)!)!.Remove(entry);
        Trashed.Add(path);
    }
}

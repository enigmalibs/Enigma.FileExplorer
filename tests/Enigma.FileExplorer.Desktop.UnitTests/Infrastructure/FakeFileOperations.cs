using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Operations;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// File operations on the in-memory file system, with the real name rules, and failures on demand.
/// </summary>
/// <param name="reader">The in-memory file system.</param>
public sealed class FakeFileOperations(FakeDirectoryReader reader) : IFileOperations
{
    /// <summary>
    /// Paths whose deletion fails.
    /// </summary>
    public HashSet<string> FailOn { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Every path deleted for good, in order.
    /// </summary>
    public List<string> Deleted { get; } = [];

    /// <inheritdoc />
    public string CreateDirectory(string parent, string name) => Create(parent, name, EntryKind.Directory);

    /// <inheritdoc />
    public string CreateFile(string parent, string name) => Create(parent, name, EntryKind.File);

    /// <inheritdoc />
    public string Rename(string path, string newName)
    {
        if (NameValidator.Validate(newName) is { } invalid)
        {
            throw new FileOperationException(invalid);
        }

        FileSystemEntry entry = reader.Find(path) ?? throw new FileOperationException($"'{Path.GetFileName(path)}' does not exist");
        string parent = Path.GetDirectoryName(entry.FullPath)!;
        string target = Path.Combine(parent, newName);

        if (reader.Find(target) is not null)
        {
            throw new FileOperationException($"'{newName}' already exists");
        }

        List<FileSystemEntry> entries = reader.EntriesOf(parent)!;
        entries.Remove(entry);
        entries.Add(entry with { Name = newName, FullPath = target });
        return target;
    }

    /// <inheritdoc />
    public void DeletePermanently(string path, CancellationToken cancellationToken = default)
    {
        if (FailOn.Contains(path))
        {
            throw new FileOperationException($"'{Path.GetFileName(path)}' is in use");
        }

        FileSystemEntry entry = reader.Find(path) ?? throw new FileOperationException($"'{Path.GetFileName(path)}' does not exist");
        reader.EntriesOf(Path.GetDirectoryName(entry.FullPath)!)!.Remove(entry);
        Deleted.Add(path);
    }

    private string Create(string parent, string name, EntryKind kind)
    {
        if (NameValidator.Validate(name) is { } invalid)
        {
            throw new FileOperationException(invalid);
        }

        string path = Path.Combine(parent, name);

        if (reader.Find(path) is not null)
        {
            throw new FileOperationException($"'{name}' already exists");
        }

        FileSystemEntry entry = kind == EntryKind.Directory ? TestEntries.Directory(parent, name) : TestEntries.File(parent, name);
        reader.EntriesOf(parent)!.Add(entry);

        if (kind == EntryKind.Directory)
        {
            reader.Add(path);
        }

        return path;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.FileSystem;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// An in-memory file system for panes: directories are declared with their entries, failures with
/// their exception, and a read can be held open to test what happens while it is in flight.
/// </summary>
public sealed class FakeDirectoryReader : IDirectoryReader
{
    private readonly Dictionary<string, List<FileSystemEntry>> _directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Exception> _failures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TaskCompletionSource> _gates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Every path read, in order.
    /// </summary>
    public List<string> Reads { get; } = [];

    /// <summary>
    /// Declares a directory and its entries, replacing any earlier declaration.
    /// </summary>
    /// <param name="path">The directory.</param>
    /// <param name="entries">Its entries.</param>
    /// <returns>The normalized directory path.</returns>
    public string Add(string path, params FileSystemEntry[] entries)
    {
        string normalized = PathUtilities.Normalize(path);
        _directories[normalized] = [.. entries];
        _failures.Remove(normalized);
        return normalized;
    }

    /// <summary>
    /// The entries of a declared directory, live: changes show on the next read.
    /// </summary>
    /// <param name="path">The directory.</param>
    /// <returns>Its entries, or <see langword="null"/> when it is not declared.</returns>
    public List<FileSystemEntry>? EntriesOf(string path)
        => _directories.TryGetValue(PathUtilities.Normalize(path), out List<FileSystemEntry>? entries) ? entries : null;

    /// <summary>
    /// Finds an entry by its full path.
    /// </summary>
    /// <param name="path">The entry's path.</param>
    /// <returns>The entry, or <see langword="null"/>.</returns>
    public FileSystemEntry? Find(string path)
    {
        string normalized = PathUtilities.Normalize(path);
        return EntriesOf(Path.GetDirectoryName(normalized) ?? normalized)?.Find(entry => string.Equals(entry.FullPath, normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Makes reading a directory fail.
    /// </summary>
    /// <param name="path">The directory.</param>
    /// <param name="exception">What the read throws.</param>
    public void Fail(string path, Exception exception) => _failures[PathUtilities.Normalize(path)] = exception;

    /// <summary>
    /// Removes a directory, so reading it reports it missing.
    /// </summary>
    /// <param name="path">The directory.</param>
    public void Remove(string path) => _directories.Remove(PathUtilities.Normalize(path));

    /// <summary>
    /// Holds reads of a directory until the returned gate is opened.
    /// </summary>
    /// <param name="path">The directory.</param>
    /// <returns>The gate; <c>SetResult()</c> lets the read finish.</returns>
    public TaskCompletionSource Hold(string path)
    {
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _gates[PathUtilities.Normalize(path)] = gate;
        return gate;
    }

    /// <inheritdoc />
    public async Task<DirectoryListing> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        string normalized = PathUtilities.Normalize(path);
        Reads.Add(normalized);

        if (_gates.Remove(normalized, out TaskCompletionSource? gate))
        {
            await gate.Task.WaitAsync(cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (_failures.TryGetValue(normalized, out Exception? failure))
        {
            throw failure;
        }

        if (!_directories.TryGetValue(normalized, out List<FileSystemEntry>? entries))
        {
            throw new DirectoryNotFoundException($"The directory '{normalized}' does not exist.");
        }

        return new DirectoryListing(normalized, [.. entries]);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Operations;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A transfer engine over the in-memory file system: copies add entries to the target, moves also remove
/// them from their source. A test can hold a run open or make it fail.
/// </summary>
/// <param name="reader">The in-memory file system.</param>
public sealed class FakeTransferEngine(FakeDirectoryReader reader) : ITransferEngine
{
    /// <summary>
    /// Every request run, in order.
    /// </summary>
    public List<TransferRequest> Requests { get; } = [];

    /// <summary>
    /// When set, runs wait for it before finishing (and honour cancellation meanwhile).
    /// </summary>
    public TaskCompletionSource? Gate { get; set; }

    /// <summary>
    /// When set, every run reports these errors.
    /// </summary>
    public List<TransferError> Errors { get; } = [];

    /// <summary>
    /// When set, runs throw this before doing anything.
    /// </summary>
    public FileOperationException? Refuse { get; set; }

    /// <inheritdoc />
    public async Task<TransferResult> RunAsync(TransferRequest request, IConflictResolver resolver, IProgress<TransferProgress>? progress, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);

        if (Refuse is { } refusal)
        {
            throw refusal;
        }

        if (Gate is { } gate)
        {
            try
            {
                await gate.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return new TransferResult([], [], 0, Cancelled: true);
            }
        }

        List<string> created = [];

        foreach (string source in request.Sources)
        {
            FileSystemEntry? entry = reader.Find(source);

            if (entry is null)
            {
                continue;
            }

            string destination = Path.Combine(request.TargetDirectory, entry.Name);
            reader.EntriesOf(request.TargetDirectory)?.Add(entry with { FullPath = destination });
            created.Add(destination);

            if (request.Mode == TransferMode.Move)
            {
                reader.EntriesOf(Path.GetDirectoryName(source)!)?.Remove(entry);
            }
        }

        progress?.Report(new TransferProgress(created.Count, created.Count, 0, 0, null));
        return new TransferResult(created, [.. Errors], 0, Cancelled: false);
    }
}

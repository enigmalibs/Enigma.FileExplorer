using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.FileSystem;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// Copies and moves trees on the local file system, with progress, cancellation, conflict decisions and
/// per-item error collection.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>A move within one volume is a rename; across volumes it is a copy followed by deleting the source,
/// and the source is deleted only when its copy succeeded completely.</item>
/// <item>A directory transferred onto an existing directory is merged: conflicts are decided per file.</item>
/// <item>A copy into the directory the source already lives in gets a free name (<c>name (2).ext</c>); a
/// move there is nothing to do.</item>
/// <item>Links are recreated as links, never followed.</item>
/// </list>
/// </remarks>
public sealed class TransferEngine : ITransferEngine
{
    private const int BufferSize = 1 << 20;

    private readonly IFileOperations _operations;
    private readonly TimeSpan _reportInterval;

    /// <summary>
    /// Creates the engine.
    /// </summary>
    /// <param name="operations">Deletes what a move or an overwrite replaces.</param>
    public TransferEngine(IFileOperations operations)
        : this(operations, TimeSpan.FromMilliseconds(50))
    {
    }

    /// <summary>
    /// Creates the engine with a chosen progress throttle.
    /// </summary>
    /// <param name="operations">Deletes what a move or an overwrite replaces.</param>
    /// <param name="reportInterval">The least time between two progress reports; tests use zero.</param>
    internal TransferEngine(IFileOperations operations, TimeSpan reportInterval)
    {
        ArgumentNullException.ThrowIfNull(operations);

        _operations = operations;
        _reportInterval = reportInterval;
    }

    /// <inheritdoc />
    public Task<TransferResult> RunAsync(TransferRequest request, IConflictResolver resolver, IProgress<TransferProgress>? progress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(resolver);

        if (!Directory.Exists(request.TargetDirectory))
        {
            throw new FileOperationException($"'{request.TargetDirectory}' does not exist");
        }

        string target = PathUtilities.Normalize(request.TargetDirectory);
        string verb = request.Mode == TransferMode.Copy ? "copy" : "move";

        foreach (string source in request.Sources)
        {
            if (Directory.Exists(source) && !IsLink(source) && PathUtilities.IsSameOrDescendant(target, source))
            {
                throw new FileOperationException($"Cannot {verb} '{Path.GetFileName(source)}' into itself");
            }
        }

        Run run = new(_operations, _reportInterval, request, target, resolver, progress, cancellationToken);

        // The whole transfer runs on the thread pool: the caller is the UI.
        return Task.Run(run.ExecuteAsync, CancellationToken.None);
    }

    private static bool IsLink(string path) => new FileInfo(path).LinkTarget is not null;

    private sealed class Run(
        IFileOperations operations,
        TimeSpan reportInterval,
        TransferRequest request,
        string target,
        IConflictResolver resolver,
        IProgress<TransferProgress>? progress,
        CancellationToken cancellationToken)
    {
        private readonly List<string> _created = [];
        private readonly List<TransferError> _errors = [];
        private readonly Stopwatch _sinceReport = Stopwatch.StartNew();
        private readonly Lazy<string[]> _mounts = new(MountPoints);
        private ConflictChoice? _applyToAll;
        private int _totalItems;
        private long _totalBytes;
        private int _completedItems;
        private long _completedBytes;
        private int _skipped;
        private string? _current;

        public async Task<TransferResult> ExecuteAsync()
        {
            bool cancelled = false;
            List<string> sources = [];

            foreach (string path in request.Sources)
            {
                string source = PathUtilities.Normalize(path);

                if (Exists(source))
                {
                    sources.Add(source);
                    (int items, long bytes) = Measure(source);
                    _totalItems += items;
                    _totalBytes += bytes;
                }
                else
                {
                    _errors.Add(new TransferError(source, $"'{Path.GetFileName(source)}' does not exist"));
                }
            }

            Report(force: true);

            try
            {
                foreach (string source in sources)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (await TransferTopAsync(source) is { } created)
                    {
                        _created.Add(created);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
            }
            catch (TransferCancelledException)
            {
                cancelled = true;
            }

            Report(force: true);
            return new TransferResult(_created, _errors, _skipped, cancelled);
        }

        private static bool Exists(string path) => UniqueNameGenerator.Exists(path);

        private static bool IsRealDirectory(string path) => Directory.Exists(path) && !IsLink(path);

        private static string[] MountPoints()
        {
            try
            {
                return [.. DriveInfo.GetDrives().Select(drive => drive.RootDirectory.FullName).OrderByDescending(root => root.Length)];
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return [];
            }
        }

        private static (int Items, long Bytes) Measure(string path)
        {
            if (IsLink(path))
            {
                return (1, 0);
            }

            if (!Directory.Exists(path))
            {
                return (1, new FileInfo(path).Length);
            }

            int items = 0;
            long bytes = 0;

            try
            {
                foreach (string child in Directory.EnumerateFileSystemEntries(path, "*", new EnumerationOptions { AttributesToSkip = 0, IgnoreInaccessible = true }))
                {
                    (int childItems, long childBytes) = Measure(child);
                    items += childItems;
                    bytes += childBytes;
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // An unreadable subtree is measured as empty; its transfer will report the failure.
            }

            return (items, bytes);
        }

        private async Task<string?> TransferTopAsync(string source)
        {
            string name = Path.GetFileName(source);
            string? parent = Path.GetDirectoryName(source);
            bool sameDirectory = (parent is not null) && PathUtilities.AreSame(parent, target);

            if (sameDirectory && (request.Mode == TransferMode.Move))
            {
                // Moving something to where it already is changes nothing.
                Done(source);
                return null;
            }

            string destination = sameDirectory
                ? Path.Combine(target, UniqueNameGenerator.FirstFree(target, name, IsRealDirectory(source), Exists))
                : Path.Combine(target, name);

            return await TransferAsync(source, destination);
        }

        private async Task<string?> TransferAsync(string source, string destination)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool sourceIsDirectory = IsRealDirectory(source);

            if (Exists(destination))
            {
                if (sourceIsDirectory && IsRealDirectory(destination))
                {
                    await MergeAsync(source, destination);
                    return destination;
                }

                switch (await DecideAsync(source, destination))
                {
                    case ConflictChoice.Skip:
                        _skipped++;
                        Done(source);
                        return null;

                    case ConflictChoice.KeepBoth:
                        string directory = Path.GetDirectoryName(destination)!;
                        destination = Path.Combine(directory, UniqueNameGenerator.FirstFree(directory, Path.GetFileName(destination), sourceIsDirectory, Exists));
                        break;

                    case ConflictChoice.Cancel:
                        throw new TransferCancelledException();

                    default:
                        if (!TryRun(source, () => operations.DeletePermanently(destination, cancellationToken)))
                        {
                            Done(source);
                            return null;
                        }

                        break;
                }
            }

            if ((request.Mode == TransferMode.Move) && SameVolume(source, destination))
            {
                // Measured first: once renamed, the source is gone.
                (int Items, long Bytes) moved = Measure(source);

                switch (TryRename(source, destination))
                {
                    case true:
                        Done(moved);
                        return destination;

                    case null:
                        Done(moved);
                        return null;
                }
            }

            bool copied = await CopyAsync(source, destination);

            if (copied && (request.Mode == TransferMode.Move))
            {
                // Only a complete copy may cost the source.
                TryRun(source, () => operations.DeletePermanently(source, cancellationToken));
            }

            return copied ? destination : null;
        }

        private async Task MergeAsync(string source, string destination)
        {
            string[] children;

            try
            {
                children = Directory.GetFileSystemEntries(source);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Fail(source, exception);
                return;
            }

            foreach (string child in children)
            {
                await TransferAsync(child, Path.Combine(destination, Path.GetFileName(child)));
            }

            if (request.Mode == TransferMode.Move)
            {
                try
                {
                    // Empty once everything moved; what was skipped or failed keeps it alive, and that is right.
                    Directory.Delete(source, recursive: false);
                }
                catch (IOException)
                {
                }
            }
        }

        private async Task<ConflictChoice> DecideAsync(string source, string destination)
        {
            if (_applyToAll is { } decided)
            {
                return decided;
            }

            FileInfo sourceInfo = new(source);
            FileInfo targetInfo = new(destination);
            bool targetIsDirectory = Directory.Exists(destination);

            ConflictInfo conflict = new(
                source,
                destination,
                IsRealDirectory(source) ? 0 : sourceInfo.Length,
                sourceInfo.LastWriteTime,
                targetIsDirectory ? 0 : targetInfo.Length,
                targetInfo.LastWriteTime,
                targetIsDirectory);

            ConflictResolution resolution = await resolver.ResolveAsync(conflict, cancellationToken);

            if (resolution.ApplyToAll)
            {
                _applyToAll = resolution.Choice;
            }

            return resolution.Choice;
        }

        private async Task<bool> CopyAsync(string source, string destination)
        {
            if (IsLink(source))
            {
                bool linked = TryRun(source, () =>
                {
                    string linkTarget = new FileInfo(source).LinkTarget!;

                    if (Directory.Exists(source))
                    {
                        Directory.CreateSymbolicLink(destination, linkTarget);
                    }
                    else
                    {
                        File.CreateSymbolicLink(destination, linkTarget);
                    }
                });

                Done(source);
                return linked;
            }

            return IsRealDirectory(source)
                ? await CopyDirectoryAsync(source, destination)
                : await CopyFileAsync(source, destination);
        }

        private async Task<bool> CopyDirectoryAsync(string source, string destination)
        {
            string[] children;

            try
            {
                children = Directory.GetFileSystemEntries(source);
                Directory.CreateDirectory(destination);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Fail(source, exception);
                Done(source);
                return false;
            }

            bool complete = true;

            foreach (string child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                complete &= await CopyAsync(child, Path.Combine(destination, Path.GetFileName(child)));
            }

            TryRun(source, () => Directory.SetLastWriteTimeUtc(destination, Directory.GetLastWriteTimeUtc(source)));
            return complete;
        }

        private async Task<bool> CopyFileAsync(string source, string destination)
        {
            _current = Path.GetFileName(source);
            bool created = false;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

            try
            {
                await using (FileStream input = new(source, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
                await using (FileStream output = new(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
                {
                    created = true;
                    int read;

                    while ((read = await input.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken)) > 0)
                    {
                        await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                        _completedBytes += read;
                        Report(force: false);
                    }
                }

                File.SetLastWriteTimeUtc(destination, File.GetLastWriteTimeUtc(source));
                File.SetAttributes(destination, File.GetAttributes(source) & ~FileAttributes.Directory);
                _completedItems++;
                Report(force: false);
                return true;
            }
            catch (OperationCanceledException)
            {
                // A half-written file is worse than none.
                if (created)
                {
                    TryDelete(destination);
                }

                throw;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                if (created)
                {
                    TryDelete(destination);
                }

                Fail(source, exception);
                _completedItems++;
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Renames across the tree in one step.
        /// </summary>
        /// <returns><see langword="true"/> when renamed, <see langword="false"/> when only a copy can do it, <see langword="null"/> when it failed (recorded).</returns>
        private bool? TryRename(string source, string destination)
        {
            try
            {
                if (Directory.Exists(source))
                {
                    Directory.Move(source, destination);
                }
                else
                {
                    File.Move(source, destination);
                }

                return true;
            }
            catch (IOException)
            {
                // Mount points that look alike can still be different devices: copying is the fallback.
                return false;
            }
            catch (UnauthorizedAccessException exception)
            {
                Fail(source, exception);
                return null;
            }
        }

        private bool SameVolume(string first, string second)
        {
            if (OperatingSystem.IsWindows())
            {
                return string.Equals(Path.GetPathRoot(first), Path.GetPathRoot(second), StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(MountOf(first), MountOf(second), StringComparison.Ordinal);
        }

        private string MountOf(string path)
            => _mounts.Value.FirstOrDefault(root => path.StartsWith(root, StringComparison.Ordinal)) ?? "/";

        private bool TryRun(string path, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Fail(path, exception);
                return false;
            }
        }

        private void Fail(string path, Exception exception)
            => _errors.Add(new TransferError(path, $"{Path.GetFileName(path)}: {exception.Message}"));

        private void Done(string path) => Done(Measure(path));

        private void Done((int Items, long Bytes) measured)
        {
            _completedItems += measured.Items;
            _completedBytes += measured.Bytes;
            Report(force: false);
        }

        private void Report(bool force)
        {
            if ((progress is null) || (!force && (_sinceReport.Elapsed < reportInterval)))
            {
                return;
            }

            _sinceReport.Restart();
            progress.Report(new TransferProgress(
                Math.Min(_completedItems, _totalItems),
                _totalItems,
                Math.Min(_completedBytes, _totalBytes),
                _totalBytes,
                _current));
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Leaving a partial file behind is reported through the item's error already.
            }
        }
    }

    /// <summary>
    /// Thrown inside a run when the user answers a conflict with Cancel.
    /// </summary>
    private sealed class TransferCancelledException : Exception
    {
    }
}

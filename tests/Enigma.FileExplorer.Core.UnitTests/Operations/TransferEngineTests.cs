using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Operations;

public sealed class TransferEngineTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly TransferEngine _engine = new(new FileOperations());
    private readonly ScriptedResolver _resolver = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task Copy_AFile_KeepsContentAndTimestamp()
    {
        string source = _temp.CreateFile("src/a.txt", "hello");
        DateTime written = new(2021, 3, 4, 5, 6, 7, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(source, written);
        string target = _temp.CreateDirectory("dst");

        TransferResult result = await RunAsync(TransferMode.Copy, target, source);

        Assert.True(result.Succeeded);
        Assert.Equal([Path.Combine(target, "a.txt")], result.Created);
        Assert.Equal("hello", File.ReadAllText(Path.Combine(target, "a.txt")));
        Assert.Equal(written, File.GetLastWriteTimeUtc(Path.Combine(target, "a.txt")));
        Assert.True(File.Exists(source));
    }

    [Fact]
    public async Task Copy_ATree()
    {
        _temp.CreateFile("src/tree/a.txt", "a");
        _temp.CreateFile("src/tree/sub/b.txt", "b");
        _temp.CreateDirectory("src/tree/empty");
        string target = _temp.CreateDirectory("dst");

        TransferResult result = await RunAsync(TransferMode.Copy, target, _temp.Combine("src/tree"));

        Assert.True(result.Succeeded);
        Assert.Equal("b", File.ReadAllText(Path.Combine(target, "tree", "sub", "b.txt")));
        Assert.True(Directory.Exists(Path.Combine(target, "tree", "empty")));
        Assert.True(Directory.Exists(_temp.Combine("src/tree")));
    }

    [Fact]
    public async Task Move_AFileAndATree()
    {
        string file = _temp.CreateFile("src/a.txt", "a");
        _temp.CreateFile("src/tree/b.txt", "b");
        string target = _temp.CreateDirectory("dst");

        TransferResult result = await RunAsync(TransferMode.Move, target, file, _temp.Combine("src/tree"));

        Assert.True(result.Succeeded);
        Assert.False(File.Exists(file));
        Assert.False(Directory.Exists(_temp.Combine("src/tree")));
        Assert.Equal("b", File.ReadAllText(Path.Combine(target, "tree", "b.txt")));
    }

    [Fact]
    public async Task Copy_IntoItsOwnDirectory_GetsAFreeName()
    {
        string source = _temp.CreateFile("src/report.pdf", "1");

        TransferResult result = await RunAsync(TransferMode.Copy, _temp.Combine("src"), source);

        Assert.Equal([_temp.Combine("src/report (2).pdf")], result.Created);
        Assert.Empty(_resolver.Asked);
    }

    [Fact]
    public async Task Move_IntoItsOwnDirectory_DoesNothing()
    {
        string source = _temp.CreateFile("src/report.pdf", "1");

        TransferResult result = await RunAsync(TransferMode.Move, _temp.Combine("src"), source);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Created);
        Assert.True(File.Exists(source));
    }

    [Theory]
    [InlineData(TransferMode.Copy)]
    [InlineData(TransferMode.Move)]
    public async Task AFolderIntoItself_IsRefusedBeforeAnythingHappens(TransferMode mode)
    {
        _temp.CreateFile("tree/a.txt");
        string inner = _temp.CreateDirectory("tree/inner");

        await Assert.ThrowsAsync<FileOperationException>(() => RunAsync(mode, inner, _temp.Combine("tree")));
        await Assert.ThrowsAsync<FileOperationException>(() => RunAsync(mode, _temp.Combine("tree"), _temp.Combine("tree")));
        Assert.Empty(Directory.GetFileSystemEntries(inner));
    }

    [Fact]
    public async Task AMissingTarget_IsRefused()
        => await Assert.ThrowsAsync<FileOperationException>(() => RunAsync(TransferMode.Copy, _temp.Combine("nowhere"), _temp.CreateFile("a.txt")));

    [Fact]
    public async Task Conflict_Skip_LeavesBoth()
    {
        string source = _temp.CreateFile("src/a.txt", "new");
        string existing = _temp.CreateFile("dst/a.txt", "old");
        _resolver.Answers.Enqueue(new ConflictResolution(ConflictChoice.Skip));

        TransferResult result = await RunAsync(TransferMode.Move, _temp.Combine("dst"), source);

        Assert.Equal(1, result.Skipped);
        Assert.Equal("old", File.ReadAllText(existing));
        Assert.True(File.Exists(source));
        ConflictInfo asked = Assert.Single(_resolver.Asked);
        Assert.Equal(existing, asked.TargetPath);
        Assert.Equal(3, asked.SourceSize);
    }

    [Fact]
    public async Task Conflict_Overwrite_Replaces()
    {
        string source = _temp.CreateFile("src/a.txt", "new");
        string existing = _temp.CreateFile("dst/a.txt", "old");
        _resolver.Answers.Enqueue(new ConflictResolution(ConflictChoice.Overwrite));

        TransferResult result = await RunAsync(TransferMode.Copy, _temp.Combine("dst"), source);

        Assert.True(result.Succeeded);
        Assert.Equal("new", File.ReadAllText(existing));
    }

    [Fact]
    public async Task Conflict_KeepBoth_UsesAFreeName()
    {
        string source = _temp.CreateFile("src/a.txt", "new");
        _temp.CreateFile("dst/a.txt", "old");
        _resolver.Answers.Enqueue(new ConflictResolution(ConflictChoice.KeepBoth));

        TransferResult result = await RunAsync(TransferMode.Copy, _temp.Combine("dst"), source);

        Assert.Equal([_temp.Combine("dst/a (2).txt")], result.Created);
        Assert.Equal("new", File.ReadAllText(_temp.Combine("dst/a (2).txt")));
        Assert.Equal("old", File.ReadAllText(_temp.Combine("dst/a.txt")));
    }

    [Fact]
    public async Task Conflict_Cancel_StopsEverything()
    {
        string first = _temp.CreateFile("src/a.txt", "a");
        string second = _temp.CreateFile("src/b.txt", "b");
        _temp.CreateFile("dst/a.txt", "old");
        _resolver.Answers.Enqueue(new ConflictResolution(ConflictChoice.Cancel));

        TransferResult result = await RunAsync(TransferMode.Copy, _temp.Combine("dst"), first, second);

        Assert.True(result.Cancelled);
        Assert.False(File.Exists(_temp.Combine("dst/b.txt")));
    }

    [Fact]
    public async Task Conflict_ApplyToAll_AsksOnce()
    {
        string first = _temp.CreateFile("src/a.txt", "a");
        string second = _temp.CreateFile("src/b.txt", "b");
        _temp.CreateFile("dst/a.txt", "old");
        _temp.CreateFile("dst/b.txt", "old");
        _resolver.Answers.Enqueue(new ConflictResolution(ConflictChoice.Overwrite, ApplyToAll: true));

        TransferResult result = await RunAsync(TransferMode.Copy, _temp.Combine("dst"), first, second);

        Assert.True(result.Succeeded);
        Assert.Single(_resolver.Asked);
        Assert.Equal("b", File.ReadAllText(_temp.Combine("dst/b.txt")));
    }

    [Fact]
    public async Task ADirectoryOntoADirectory_Merges_AskingOnlyForCollidingFiles()
    {
        _temp.CreateFile("src/docs/new.txt", "new");
        _temp.CreateFile("src/docs/same.txt", "mine");
        _temp.CreateFile("dst/docs/same.txt", "theirs");
        _temp.CreateFile("dst/docs/other.txt", "other");
        _resolver.Answers.Enqueue(new ConflictResolution(ConflictChoice.Skip));

        TransferResult result = await RunAsync(TransferMode.Copy, _temp.Combine("dst"), _temp.Combine("src/docs"));

        Assert.Equal([_temp.Combine("dst/docs/same.txt")], _resolver.Asked.Select(conflict => conflict.TargetPath));
        Assert.Equal("new", File.ReadAllText(_temp.Combine("dst/docs/new.txt")));
        Assert.Equal("theirs", File.ReadAllText(_temp.Combine("dst/docs/same.txt")));
        Assert.True(File.Exists(_temp.Combine("dst/docs/other.txt")));
        Assert.Equal(1, result.Skipped);
    }

    [Fact]
    public async Task AMoveMerge_RemovesTheSourceOnlyWhenEverythingMoved()
    {
        _temp.CreateFile("src/docs/new.txt", "new");
        _temp.CreateFile("src/docs/same.txt", "mine");
        _temp.CreateFile("dst/docs/same.txt", "theirs");
        _resolver.Answers.Enqueue(new ConflictResolution(ConflictChoice.Skip));

        await RunAsync(TransferMode.Move, _temp.Combine("dst"), _temp.Combine("src/docs"));

        Assert.True(File.Exists(_temp.Combine("dst/docs/new.txt")));
        Assert.False(File.Exists(_temp.Combine("src/docs/new.txt")));
        Assert.True(File.Exists(_temp.Combine("src/docs/same.txt")));
    }

    [Fact]
    public async Task AMissingSource_IsAnErrorAndTheRestStillGoes()
    {
        string present = _temp.CreateFile("src/here.txt", "x");
        string target = _temp.CreateDirectory("dst");

        TransferResult result = await RunAsync(TransferMode.Copy, target, _temp.Combine("src/gone.txt"), present);

        TransferError error = Assert.Single(result.Errors);
        Assert.Contains("gone.txt", error.Message, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(target, "here.txt")));
    }

    [Fact]
    public async Task AnUnreadableFile_IsAnErrorAndLeavesNoPartialCopy()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Exclusive file locks are a Windows behaviour.");

        string locked = _temp.CreateFile("src/locked.txt", "secret");
        string free = _temp.CreateFile("src/free.txt", "free");
        string target = _temp.CreateDirectory("dst");

        using (new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            TransferResult result = await RunAsync(TransferMode.Copy, target, locked, free);

            Assert.Single(result.Errors);
            Assert.False(File.Exists(Path.Combine(target, "locked.txt")));
            Assert.True(File.Exists(Path.Combine(target, "free.txt")));
        }
    }

    [Fact]
    public async Task Cancellation_StopsAndLeavesNoHalfWrittenFile()
    {
        string big = _temp.CreateFile("src/big.bin");
        File.WriteAllBytes(big, new byte[8 * 1024 * 1024]);
        string target = _temp.CreateDirectory("dst");
        using CancellationTokenSource cancellation = new();
        SynchronousProgress progress = new(report =>
        {
            if (report.CompletedBytes > 0)
            {
                cancellation.Cancel();
            }
        });

        TransferEngine unthrottled = new(new FileOperations(), TimeSpan.Zero);

        TransferResult result = await unthrottled.RunAsync(new TransferRequest([big], target, TransferMode.Copy), _resolver, progress, cancellation.Token);

        Assert.True(result.Cancelled);
        Assert.False(File.Exists(Path.Combine(target, "big.bin")));
    }

    [Fact]
    public async Task Progress_IsMonotonicAndEndsComplete()
    {
        _temp.CreateFile("src/tree/a.txt", new string('a', 1000));
        _temp.CreateFile("src/tree/b.txt", new string('b', 2000));
        List<TransferProgress> reports = [];
        SynchronousProgress progress = new(reports.Add);

        await _engine.RunAsync(new TransferRequest([_temp.Combine("src/tree")], _temp.CreateDirectory("dst"), TransferMode.Copy), _resolver, progress, TestContext.Current.CancellationToken);

        Assert.Equal(2, reports[^1].TotalItems);
        Assert.Equal(3000, reports[^1].TotalBytes);
        Assert.Equal(1, reports[^1].Fraction);
        Assert.True(reports.Zip(reports.Skip(1)).All(pair => pair.Second.CompletedBytes >= pair.First.CompletedBytes));
    }

    private Task<TransferResult> RunAsync(TransferMode mode, string target, params string[] sources)
        => _engine.RunAsync(new TransferRequest(sources, target, mode), _resolver, null, TestContext.Current.CancellationToken);

    private sealed class ScriptedResolver : IConflictResolver
    {
        public Queue<ConflictResolution> Answers { get; } = new();

        public List<ConflictInfo> Asked { get; } = [];

        public Task<ConflictResolution> ResolveAsync(ConflictInfo conflict, CancellationToken cancellationToken)
        {
            Asked.Add(conflict);
            return Task.FromResult(Answers.Count > 0 ? Answers.Dequeue() : new ConflictResolution(ConflictChoice.Skip));
        }
    }

    private sealed class SynchronousProgress(Action<TransferProgress> report) : IProgress<TransferProgress>
    {
        public void Report(TransferProgress value) => report(value);
    }
}

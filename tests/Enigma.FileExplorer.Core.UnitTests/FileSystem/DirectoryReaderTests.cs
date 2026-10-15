using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.FileSystem;

public sealed class DirectoryReaderTests
{
    private readonly DirectoryReader _reader = new();

    [Fact]
    public async Task ReadAsync_ListsFilesAndDirectoriesWithTheirKindAndSize()
    {
        using TempDirectory temp = new();
        temp.CreateFile("notes.txt", "hello");
        temp.CreateDirectory("docs");

        DirectoryListing listing = await _reader.ReadAsync(temp.Path, TestContext.Current.CancellationToken);

        Assert.Equal(temp.Path, listing.Path);
        Assert.Equal(2, listing.Entries.Count);

        FileSystemEntry file = Assert.Single(listing.Entries, e => e.Name == "notes.txt");
        Assert.Equal(EntryKind.File, file.Kind);
        Assert.Equal(5, file.Size);
        Assert.Equal(temp.Combine("notes.txt"), file.FullPath);
        Assert.False(file.IsHidden);

        FileSystemEntry directory = Assert.Single(listing.Entries, e => e.Name == "docs");
        Assert.Equal(EntryKind.Directory, directory.Kind);
        Assert.Equal(0, directory.Size);
    }

    [Fact]
    public async Task ReadAsync_ReportsDotFilesAsHidden()
    {
        using TempDirectory temp = new();
        temp.CreateFile(".env");
        temp.CreateDirectory(".git");

        DirectoryListing listing = await _reader.ReadAsync(temp.Path, TestContext.Current.CancellationToken);

        Assert.All(listing.Entries, entry => Assert.True(entry.IsHidden));
    }

    [Fact]
    public async Task ReadAsync_ReportsTheHiddenAttributeAsHidden()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The Hidden attribute is a Windows concept.");

        using TempDirectory temp = new();
        string path = temp.CreateFile("desktop.ini");
        File.SetAttributes(path, FileAttributes.Hidden);

        DirectoryListing listing = await _reader.ReadAsync(temp.Path, TestContext.Current.CancellationToken);

        Assert.True(Assert.Single(listing.Entries).IsHidden);
    }

    [Fact]
    public async Task ReadAsync_RecordsTheLastWriteTime()
    {
        using TempDirectory temp = new();
        string path = temp.CreateFile("old.txt");
        DateTime written = new(2020, 5, 17, 10, 30, 0, DateTimeKind.Local);
        File.SetLastWriteTime(path, written);

        DirectoryListing listing = await _reader.ReadAsync(temp.Path, TestContext.Current.CancellationToken);

        Assert.Equal(written, Assert.Single(listing.Entries).LastWriteTime);
    }

    [Fact]
    public async Task ReadAsync_NormalizesATrailingSeparator()
    {
        using TempDirectory temp = new();

        DirectoryListing listing = await _reader.ReadAsync(temp.Path + Path.DirectorySeparatorChar, TestContext.Current.CancellationToken);

        Assert.Equal(temp.Path, listing.Path);
    }

    [Fact]
    public async Task ReadAsync_ReturnsAnEmptyListingForAnEmptyDirectory()
    {
        using TempDirectory temp = new();

        DirectoryListing listing = await _reader.ReadAsync(temp.Path, TestContext.Current.CancellationToken);

        Assert.Empty(listing.Entries);
    }

    [Fact]
    public async Task ReadAsync_ThrowsForAMissingDirectory()
    {
        using TempDirectory temp = new();

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _reader.ReadAsync(temp.Combine("missing"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadAsync_HonoursCancellation()
    {
        using TempDirectory temp = new();
        temp.CreateFile("a.txt");
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _reader.ReadAsync(temp.Path, cancellation.Token));
    }

    [Fact]
    public void Read_StopsBetweenEntriesWhenCancelled()
    {
        using TempDirectory temp = new();

        foreach (int i in Enumerable.Range(0, 5))
        {
            temp.CreateFile($"file{i}.txt");
        }

        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => DirectoryReader.Read(temp.Path, cancellation.Token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ReadAsync_RejectsABlankPath(string path)
        => await Assert.ThrowsAsync<ArgumentException>(() => _reader.ReadAsync(path, TestContext.Current.CancellationToken));
}

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Operations;

public sealed class FileOperationsTests
{
    private readonly FileOperations _operations = new();

    [Fact]
    public void CreateDirectory_CreatesItAndReturnsItsPath()
    {
        using TempDirectory temp = new();

        string path = _operations.CreateDirectory(temp.Path, "New folder");

        Assert.Equal(temp.Combine("New folder"), path);
        Assert.True(Directory.Exists(path));
    }

    [Fact]
    public void CreateFile_CreatesAnEmptyFile()
    {
        using TempDirectory temp = new();

        string path = _operations.CreateFile(temp.Path, "notes.md");

        Assert.True(File.Exists(path));
        Assert.Equal(0, new FileInfo(path).Length);
    }

    [Fact]
    public void Create_RefusesATakenName()
    {
        using TempDirectory temp = new();
        temp.CreateFile("taken");

        FileOperationException file = Assert.Throws<FileOperationException>(() => _operations.CreateFile(temp.Path, "taken"));
        FileOperationException directory = Assert.Throws<FileOperationException>(() => _operations.CreateDirectory(temp.Path, "taken"));

        Assert.Equal("'taken' already exists", file.Message);
        Assert.Equal("'taken' already exists", directory.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("..")]
    [InlineData("a/b")]
    public void Create_RefusesAnInvalidName(string name)
    {
        using TempDirectory temp = new();

        Assert.Throws<FileOperationException>(() => _operations.CreateDirectory(temp.Path, name));
        Assert.Empty(Directory.EnumerateFileSystemEntries(temp.Path));
    }

    [Fact]
    public void Rename_AFile()
    {
        using TempDirectory temp = new();
        string source = temp.CreateFile("old.txt", "content");

        string renamed = _operations.Rename(source, "new.txt");

        Assert.Equal(temp.Combine("new.txt"), renamed);
        Assert.False(File.Exists(source));
        Assert.Equal("content", File.ReadAllText(renamed));
    }

    [Fact]
    public void Rename_ADirectory()
    {
        using TempDirectory temp = new();
        temp.CreateFile("old/inside.txt");

        string renamed = _operations.Rename(temp.Combine("old"), "new");

        Assert.True(File.Exists(Path.Combine(renamed, "inside.txt")));
    }

    [Fact]
    public void Rename_OnlyTheCase()
    {
        using TempDirectory temp = new();
        string source = temp.CreateFile("readme.md");
        temp.CreateDirectory("docs");

        _operations.Rename(source, "README.md");
        _operations.Rename(temp.Combine("docs"), "Docs");

        string[] names = [.. Directory.EnumerateFileSystemEntries(temp.Path).Select(Path.GetFileName).Order(StringComparer.Ordinal)!];
        Assert.Equal(["Docs", "README.md"], names);
    }

    [Fact]
    public void Rename_RefusesATakenName_AndKeepsBoth()
    {
        using TempDirectory temp = new();
        string source = temp.CreateFile("a.txt", "a");
        temp.CreateFile("b.txt", "b");

        FileOperationException error = Assert.Throws<FileOperationException>(() => _operations.Rename(source, "b.txt"));

        Assert.Equal("'b.txt' already exists", error.Message);
        Assert.Equal("a", File.ReadAllText(source));
        Assert.Equal("b", File.ReadAllText(temp.Combine("b.txt")));
    }

    [Fact]
    public void Rename_ToTheSameName_DoesNothing()
    {
        using TempDirectory temp = new();
        string source = temp.CreateFile("same.txt");

        Assert.Equal(source, _operations.Rename(source, "same.txt"));
    }

    [Fact]
    public void DeletePermanently_AFile_EvenReadOnly()
    {
        using TempDirectory temp = new();
        string path = temp.CreateFile("locked.txt");
        File.SetAttributes(path, FileAttributes.ReadOnly);

        _operations.DeletePermanently(path, TestContext.Current.CancellationToken);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void DeletePermanently_ATree()
    {
        using TempDirectory temp = new();
        temp.CreateFile("tree/a.txt");
        temp.CreateFile("tree/sub/b.txt");
        temp.CreateFile("tree/sub/deeper/c.txt");
        File.SetAttributes(temp.Combine("tree/sub/b.txt"), FileAttributes.ReadOnly);

        _operations.DeletePermanently(temp.Combine("tree"), TestContext.Current.CancellationToken);

        Assert.False(Directory.Exists(temp.Combine("tree")));
    }

    [Fact]
    public void DeletePermanently_ALinkLeavesItsTargetAlone()
    {
        using TempDirectory temp = new();
        temp.CreateFile("target/keep.txt");

        try
        {
            Directory.CreateSymbolicLink(temp.Combine("link"), temp.Combine("target"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Assert.Skip("Creating symbolic links needs a privilege on this machine.");
        }

        _operations.DeletePermanently(temp.Combine("link"), TestContext.Current.CancellationToken);

        Assert.False(Directory.Exists(temp.Combine("link")));
        Assert.True(File.Exists(temp.Combine("target/keep.txt")));
    }

    [Fact]
    public void DeletePermanently_HonoursCancellation()
    {
        using TempDirectory temp = new();
        temp.CreateFile("tree/a.txt");
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => _operations.DeletePermanently(temp.Combine("tree"), cancellation.Token));
        Assert.True(Directory.Exists(temp.Combine("tree")));
    }

    [Fact]
    public void DeletePermanently_AMissingEntry_IsReported()
    {
        using TempDirectory temp = new();

        Assert.Throws<FileOperationException>(() => _operations.DeletePermanently(temp.Combine("ghost"), TestContext.Current.CancellationToken));
    }
}

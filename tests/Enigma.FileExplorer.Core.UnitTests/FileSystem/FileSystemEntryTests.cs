using System;
using System.IO;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.FileSystem;

public sealed class FileSystemEntryTests
{
    [Theory]
    [InlineData("photo.png", ".png")]
    [InlineData("archive.tar.gz", ".gz")]
    [InlineData("Makefile", "")]
    [InlineData(".gitignore", "")]
    [InlineData("trailing.", ".")]
    public void Extension_OfAFile(string name, string expected)
        => Assert.Equal(expected, Entry(name, EntryKind.File).Extension);

    [Fact]
    public void Extension_OfADirectoryIsEmpty() => Assert.Equal(string.Empty, Entry("folder.d", EntryKind.Directory).Extension);

    [Theory]
    [InlineData("photo.png", EntryKind.File, "PNG File")]
    [InlineData("Makefile", EntryKind.File, "File")]
    [InlineData(".bashrc", EntryKind.File, "File")]
    [InlineData("trailing.", EntryKind.File, "File")]
    [InlineData("src", EntryKind.Directory, "Folder")]
    public void FileTypes_Describe(string name, EntryKind kind, string expected)
        => Assert.Equal(expected, FileTypes.Describe(Entry(name, kind)));

    [Fact]
    public void FromInfo_CapturesAFile()
    {
        using TempDirectory temp = new();
        string path = temp.CreateFile(".hidden.txt", "abc");

        FileSystemEntry entry = FileSystemEntry.FromInfo(new FileInfo(path));

        Assert.Equal(".hidden.txt", entry.Name);
        Assert.Equal(path, entry.FullPath);
        Assert.Equal(EntryKind.File, entry.Kind);
        Assert.Equal(3, entry.Size);
        Assert.True(entry.IsHidden);
        Assert.False(entry.IsSymbolicLink);
    }

    [Fact]
    public void FromInfo_CapturesADirectory()
    {
        using TempDirectory temp = new();
        string path = temp.CreateDirectory("sub");

        FileSystemEntry entry = FileSystemEntry.FromInfo(new DirectoryInfo(path));

        Assert.Equal(EntryKind.Directory, entry.Kind);
        Assert.True(entry.IsDirectory);
        Assert.Equal(0, entry.Size);
    }

    [Theory]
    [InlineData(".config", FileAttributes.Normal, true)]
    [InlineData("visible", FileAttributes.Normal, false)]
    [InlineData("visible", FileAttributes.Hidden, true)]
    [InlineData("visible", FileAttributes.Hidden | FileAttributes.System, true)]
    public void HiddenRules_IsHidden(string name, FileAttributes attributes, bool expected)
        => Assert.Equal(expected, HiddenRules.IsHidden(name, attributes));

    private static FileSystemEntry Entry(string name, EntryKind kind)
        => new(name, name, kind, 0, DateTime.MinValue, IsHidden: false, IsSymbolicLink: false);
}

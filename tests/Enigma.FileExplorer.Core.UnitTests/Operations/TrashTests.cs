using System;
using System.IO;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Operations;

public sealed class TrashTests
{
    private static readonly DateTimeOffset Deleted = new(2026, 10, 14, 9, 30, 15, TimeSpan.Zero);

    [Fact]
    public void Freedesktop_MovesTheFileAndWritesItsInfo()
    {
        using TempDirectory temp = new();
        string file = temp.CreateFile("docs/my report.pdf", "pdf");
        string trash = temp.Combine("Trash");
        FreedesktopTrash target = new(trash, new FixedClock(Deleted));

        target.MoveToTrash(file);

        Assert.False(File.Exists(file));
        Assert.Equal("pdf", File.ReadAllText(Path.Combine(trash, "files", "my report.pdf")));
        string info = File.ReadAllText(Path.Combine(trash, "info", "my report.pdf.trashinfo"));
        Assert.StartsWith("[Trash Info]\nPath=", info, StringComparison.Ordinal);
        Assert.Contains("my%20report.pdf\n", info, StringComparison.Ordinal);
        Assert.Contains($"DeletionDate={Deleted.LocalDateTime:yyyy-MM-ddTHH:mm:ss}\n", info, StringComparison.Ordinal);
    }

    [Fact]
    public void Freedesktop_GivesASameNamedEntryAFreeName()
    {
        using TempDirectory temp = new();
        string trash = temp.Combine("Trash");
        FreedesktopTrash target = new(trash, new FixedClock(Deleted));

        target.MoveToTrash(temp.CreateFile("a/notes.txt", "first"));
        target.MoveToTrash(temp.CreateFile("b/notes.txt", "second"));

        Assert.Equal("first", File.ReadAllText(Path.Combine(trash, "files", "notes.txt")));
        Assert.Equal("second", File.ReadAllText(Path.Combine(trash, "files", "notes (2).txt")));
        Assert.True(File.Exists(Path.Combine(trash, "info", "notes (2).txt.trashinfo")));
    }

    [Fact]
    public void Freedesktop_TrashesADirectory()
    {
        using TempDirectory temp = new();
        temp.CreateFile("project/src/main.c");
        string trash = temp.Combine("Trash");

        new FreedesktopTrash(trash, new FixedClock(Deleted)).MoveToTrash(temp.Combine("project"));

        Assert.False(Directory.Exists(temp.Combine("project")));
        Assert.True(File.Exists(Path.Combine(trash, "files", "project", "src", "main.c")));
        Assert.True(File.Exists(Path.Combine(trash, "info", "project.trashinfo")));
    }

    [Fact]
    public void Freedesktop_AMissingEntry_IsReportedAndLeavesNoInfo()
    {
        using TempDirectory temp = new();
        string trash = temp.Combine("Trash");

        Assert.Throws<FileOperationException>(() => new FreedesktopTrash(trash, new FixedClock(Deleted)).MoveToTrash(temp.Combine("ghost")));
        Assert.False(Directory.Exists(Path.Combine(trash, "info")) && (Directory.GetFiles(Path.Combine(trash, "info")).Length > 0));
    }

    [Fact]
    public void Freedesktop_EncodesThePathForTheInfoFile()
    {
        string content = FreedesktopTrash.InfoContent("/home/me/Été #1/a&b.txt", Deleted);

        Assert.Contains("Path=/home/me/%C3%89t%C3%A9%20%231/a%26b.txt\n", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Mac_MovesUnderAFreeName()
    {
        using TempDirectory temp = new();
        string trash = temp.Combine(".Trash");
        MacTrash target = new(trash);

        target.MoveToTrash(temp.CreateFile("x/photo.jpg", "1"));
        target.MoveToTrash(temp.CreateFile("y/photo.jpg", "2"));

        Assert.Equal("1", File.ReadAllText(Path.Combine(trash, "photo.jpg")));
        Assert.Equal("2", File.ReadAllText(Path.Combine(trash, "photo (2).jpg")));
    }

    [Fact]
    public void WindowsRecycleBin_OnlyOnFixedDrives()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows only.");

        if (OperatingSystem.IsWindows())
        {
            WindowsRecycleBin bin = new();

            Assert.True(bin.CanTrash(Path.GetTempPath()));
            Assert.False(bin.CanTrash(@"\\server\share\file.txt"));
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

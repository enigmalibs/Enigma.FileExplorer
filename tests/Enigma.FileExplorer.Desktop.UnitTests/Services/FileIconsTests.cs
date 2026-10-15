using System;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.Icons.Phosphor;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests.Services;

public sealed class FileIconsTests
{
    [Theory]
    [InlineData("notes.txt", PhosphorIcon.FileText)]
    [InlineData("README.md", PhosphorIcon.FileMd)]
    [InlineData("Program.CS", PhosphorIcon.FileCSharp)]
    [InlineData("photo.JPEG", PhosphorIcon.FileJpg)]
    [InlineData("song.flac", PhosphorIcon.FileAudio)]
    [InlineData("clip.mkv", PhosphorIcon.FileVideo)]
    [InlineData("backup.tar.gz", PhosphorIcon.FileArchive)]
    [InlineData("bundle.zip", PhosphorIcon.FileZip)]
    [InlineData("report.pdf", PhosphorIcon.FilePdf)]
    [InlineData("budget.xlsx", PhosphorIcon.FileXls)]
    [InlineData("setup.exe", PhosphorIcon.Package)]
    [InlineData("id.pem", PhosphorIcon.FileLock)]
    [InlineData("settings.json", PhosphorIcon.FileCode)]
    [InlineData("Makefile", PhosphorIcon.File)]
    [InlineData("data.unknownext", PhosphorIcon.File)]
    public void For_AFile_PicksTheFamilyGlyph(string name, PhosphorIcon expected)
        => Assert.Equal(expected, FileIcons.For(Entry(name, EntryKind.File, link: false)));

    [Fact]
    public void For_ADirectory_IsAFolder() => Assert.Equal(PhosphorIcon.Folder, FileIcons.For(Entry("src", EntryKind.Directory, link: false)));

    [Fact]
    public void For_ALinkedDirectory_IsADashedFolder()
        => Assert.Equal(PhosphorIcon.FolderDashed, FileIcons.For(Entry("OneDrive", EntryKind.Directory, link: true)));

    [Fact]
    public void For_EveryPlaceKind_HasAGlyphThatRenders()
    {
        foreach (PlaceKind kind in Enum.GetValues<PlaceKind>())
        {
            PhosphorIcon icon = FileIcons.For(kind);

            Assert.NotNull(PhosphorIconSet.Instance.GetGlyph(icon, PhosphorWeight.Regular));
        }
    }

    private static FileSystemEntry Entry(string name, EntryKind kind, bool link)
        => new(name, name, kind, 0, DateTime.MinValue, IsHidden: false, IsSymbolicLink: link);
}

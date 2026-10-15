using System;
using System.IO;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Settings;

public sealed class AtomicFileTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void WriteAllText_ReplacesTheOldContent_AsUtf8WithoutABom()
    {
        string path = _temp.CreateFile("settings.json", "old content that is longer than the new one");

        AtomicFile.WriteAllText(path, "näw");

        Assert.Equal("näw", File.ReadAllText(path));
        Assert.Equal([0x6E, 0xC3, 0xA4, 0x77], File.ReadAllBytes(path));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void WriteAllText_CreatesTheMissingDirectories()
    {
        string path = _temp.Combine(Path.Combine("a", "b", "settings.json"));

        AtomicFile.WriteAllText(path, "{}");

        Assert.Equal("{}", File.ReadAllText(path));
    }

    [Fact]
    public void AppPaths_PointIntoAFolderOfTheirOwn()
    {
        Assert.True(Path.IsPathRooted(AppPaths.SettingsFile));
        Assert.Equal("settings.json", Path.GetFileName(AppPaths.SettingsFile));
        Assert.Equal(AppPaths.ApplicationName, Path.GetFileName(AppPaths.SettingsDirectory));
    }
}

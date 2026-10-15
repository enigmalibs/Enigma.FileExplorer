using System.IO;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Core.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Settings;

/// <summary>
/// The settings file: a round trip, and every way a file can be missing, partial, foreign or broken.
/// </summary>
public sealed class JsonSettingsStoreTests : System.IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly JsonSettingsStore _store;

    public JsonSettingsStoreTests()
    {
        _store = new JsonSettingsStore(_temp.Combine(Path.Combine("config", "settings.json")), NullLogger<JsonSettingsStore>.Instance);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Load_WithoutAFile_GivesTheDefaults()
    {
        AppSettings settings = _store.Load();

        Assert.Equal(new AppSettings(), settings);
        Assert.Equal(ThemePreference.Dark, settings.Theme);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips_AndCreatesTheDirectory()
    {
        AppSettings saved = new()
        {
            Theme = ThemePreference.Light,
            ShowHidden = true,
            Window = new WindowPlacement { X = -1200, Y = 40, Width = 1024.5, Height = 700, IsMaximized = true },
            Pane = new PaneSettings { ViewMode = ViewMode.LargeIcons, SortKey = SortKey.Modified, SortDirection = SortDirection.Descending },
        };

        _store.Save(saved);
        AppSettings loaded = _store.Load();

        // Record equality compares the bookmark list by reference; its content has a test of its own.
        Assert.Equal(saved with { Bookmarks = loaded.Bookmarks }, loaded);
        Assert.Empty(loaded.Bookmarks);
    }

    [Fact]
    public void Bookmarks_RoundTrip_InTheirOrder()
    {
        _store.Save(new AppSettings
        {
            Bookmarks =
            [
                new BookmarkSettings { Name = "Work", Path = "/home/me/work" },
                new BookmarkSettings { Name = "Pictures", Path = "/home/me/Pictures" },
            ],
        });

        AppSettings loaded = _store.Load();

        Assert.Equal(
            [new BookmarkSettings { Name = "Work", Path = "/home/me/work" }, new BookmarkSettings { Name = "Pictures", Path = "/home/me/Pictures" }],
            loaded.Bookmarks);
        Assert.Contains("\"bookmarks\": [", File.ReadAllText(_store.Path), System.StringComparison.Ordinal);
    }

    [Fact]
    public void Save_WritesReadableJson_WithoutLeavingATemporaryFile()
    {
        _store.Save(new AppSettings { Theme = ThemePreference.System });

        string json = File.ReadAllText(_store.Path);
        Assert.Contains("\"theme\": \"System\"", json, System.StringComparison.Ordinal);
        Assert.Contains("\"schemaVersion\": 1", json, System.StringComparison.Ordinal);
        Assert.DoesNotContain("\"window\"", json, System.StringComparison.Ordinal);
        Assert.False(File.Exists(_store.Path + ".tmp"));
    }

    [Fact]
    public void Load_SkipsUnknownMembers_AndDefaultsMissingOnes()
    {
        Write("""
            {
              // A newer version's file, edited by hand.
              "theme": "light",
              "futureOption": { "anything": [1, 2] },
              "pane": { "sortKey": "Size", },
            }
            """);

        AppSettings settings = _store.Load();

        Assert.Equal(ThemePreference.Light, settings.Theme);
        Assert.False(settings.ShowHidden);
        Assert.Null(settings.Window);
        Assert.Equal(new PaneSettings { SortKey = SortKey.Size }, settings.Pane);
        Assert.False(File.Exists(_store.Path + JsonSettingsStore.BadSuffix));
    }

    [Fact]
    public void Load_AFileFromWhenTheWorkspaceWasSaved_StillLoads_WithoutIt()
    {
        // Earlier builds saved the split layout and every pane's directory; that member is skipped now.
        Write("""
            {
              "schemaVersion": 1,
              "theme": "Light",
              "workspace": {
                "layout": { "axis": "Columns", "ratio": 0.5, "first": { "pane": 0 }, "second": { "pane": 1 } },
                "panes": [ { "id": 0, "path": "/home/me", "viewMode": "Details" } ],
                "activePane": 1
              }
            }
            """);

        AppSettings settings = _store.Load();

        Assert.Equal(ThemePreference.Light, settings.Theme);
        Assert.Null(settings.Pane);
        Assert.False(File.Exists(_store.Path + JsonSettingsStore.BadSuffix));
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("""{ "theme": "purple" }""")]
    [InlineData("""{ "pane": { "viewMode": null } }""")]
    [InlineData("""[1, 2, 3]""")]
    public void Load_AnUnreadableFile_GivesTheDefaults_AndKeepsTheFileAsBad(string content)
    {
        Write(content);

        AppSettings settings = _store.Load();

        Assert.Equal(new AppSettings(), settings);
        Assert.Equal(content, File.ReadAllText(_store.Path + JsonSettingsStore.BadSuffix));
        Assert.Equal(content, File.ReadAllText(_store.Path));
    }

    [Fact]
    public void Load_TheJsonNull_GivesTheDefaults()
    {
        Write("null");

        Assert.Equal(new AppSettings(), _store.Load());
    }

    [Fact]
    public void Save_WhereItCannotWrite_DoesNotThrow()
    {
        // The settings path is a directory, which no file can replace.
        string directory = _temp.CreateDirectory("taken");
        JsonSettingsStore store = new(directory, NullLogger<JsonSettingsStore>.Instance);

        store.Save(new AppSettings());

        Assert.True(Directory.Exists(directory));
        Assert.False(File.Exists(directory + ".tmp"));
    }

    private void Write(string content)
    {
        _temp.CreateDirectory("config");
        File.WriteAllText(_store.Path, content);
    }
}

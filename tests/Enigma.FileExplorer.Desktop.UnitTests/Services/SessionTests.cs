using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Services;

/// <summary>
/// What the explorer remembers between runs: what is saved, what a restart brings back (a single pane at
/// home, shown the way the last one was), and when it saves.
/// </summary>
public sealed class SessionTests
{
    private static readonly string Home = PathOf("home");
    private static readonly string Docs = PathOf("home", "docs");
    private static readonly string Pictures = PathOf("home", "Pictures");

    private readonly FakeSettingsStore _store = new();
    private readonly ExplorerHarness _explorer = Explorer();

    [Fact]
    public void Load_AppliesTheThemeAndHiddenFiles_WithoutSaving()
    {
        _store.Stored = new AppSettings { Theme = ThemePreference.Light, ShowHidden = true };

        AppSettings settings = SessionOf(_explorer).Load();

        Assert.Same(_store.Stored, settings);
        Assert.Equal(ThemePreference.Light, _explorer.Theme.Preference);
        Assert.True(_explorer.Options.ShowHidden);
        Assert.Empty(_store.Saved);
    }

    [Fact]
    public async Task Save_CapturesTheActivePanesView_TheWindow_AndTheSwitches_ButNotWhereThePanesAre()
    {
        Session session = SessionOf(_explorer);
        session.Load();
        session.TrackWindow(() => new WindowPlacement { X = 10, Y = 20, Width = 800, Height = 600 });
        await TwoPanesAsync(_explorer);

        session.Save();

        AppSettings saved = Assert.Single(_store.Saved);
        Assert.Equal(new WindowPlacement { X = 10, Y = 20, Width = 800, Height = 600 }, saved.Window);
        Assert.Equal(ThemePreference.Dark, saved.Theme);
        Assert.Equal(new PaneSettings { ViewMode = ViewMode.LargeIcons, SortKey = SortKey.Size, SortDirection = SortDirection.Descending }, saved.Pane);
    }

    [Fact]
    public async Task ARestart_OpensASinglePaneAtHome_WithTheLastView()
    {
        Session first = SessionOf(_explorer);
        first.Load();
        await TwoPanesAsync(_explorer);
        first.Save();

        ExplorerHarness restarted = Explorer();
        AppSettings loaded = SessionOf(restarted).Load();
        await restarted.Window.StartAsync(view: loaded.Pane);

        PaneViewModel pane = Assert.Single(restarted.Workspace.Panes);
        Assert.Equal(Home, pane.CurrentPath);
        Assert.Equal(ViewMode.LargeIcons, pane.ViewMode);
        Assert.Equal(EntrySort.For(SortKey.Size), pane.Sort);
        Assert.Equal(new LayoutSnapshot(Pane: restarted.Workspace.ActivePaneId.Value), restarted.Workspace.Layout.ToSnapshot());
    }

    [Fact]
    public async Task ACommandLineDirectory_OpensInTheSinglePane()
    {
        _store.Stored = new AppSettings { Pane = new PaneSettings { ViewMode = ViewMode.MediumIcons } };
        AppSettings loaded = SessionOf(_explorer).Load();

        await _explorer.Window.StartAsync(Pictures, loaded.Pane);

        Assert.Equal(Pictures, Assert.Single(_explorer.Workspace.Panes).CurrentPath);
        Assert.Equal(ViewMode.MediumIcons, _explorer.Pane.ViewMode);
    }

    [Fact]
    public async Task AFirstStart_OpensASinglePaneAtHome_WithTheDefaultView()
    {
        AppSettings loaded = SessionOf(_explorer).Load();

        await _explorer.Window.StartAsync(view: loaded.Pane);

        Assert.Null(loaded.Pane);
        Assert.Equal(Home, Assert.Single(_explorer.Workspace.Panes).CurrentPath);
        Assert.Equal(ViewMode.Details, _explorer.Pane.ViewMode);
        Assert.Equal(EntrySort.Default, _explorer.Pane.Sort);
    }

    [Fact]
    public async Task ChangingTheThemeOrHiddenFiles_SavesAtOnce()
    {
        Session session = SessionOf(_explorer);
        session.Load();
        await _explorer.Window.StartAsync();

        await _explorer.RunAsync(CommandIds.Theme, "light");
        await _explorer.PressAsync("zh");

        Assert.Equal(2, _store.Saved.Count);
        Assert.Equal(ThemePreference.Light, _store.Saved[0].Theme);
        Assert.False(_store.Saved[0].ShowHidden);
        Assert.True(_store.Saved[1].ShowHidden);
        Assert.Equal(ViewMode.Details, _store.Saved[1].Pane?.ViewMode);
    }

    [Fact]
    public void Load_AppliesTheTrashQuestion_AndSwitchingItSavesAtOnce()
    {
        _store.Stored = new AppSettings { ConfirmTrash = false };
        Session session = SessionOf(_explorer);

        session.Load();
        Assert.False(_explorer.Options.ConfirmTrash);
        Assert.Empty(_store.Saved);

        _explorer.Ex("set confirmtrash");

        Assert.True(Assert.Single(_store.Saved).ConfirmTrash);
    }

    [Fact]
    public void Load_BringsTheBookmarksBack_AndAddingOneSavesAtOnce()
    {
        _store.Stored = new AppSettings { Bookmarks = [new BookmarkSettings { Name = "Work", Path = Docs }] };
        Session session = SessionOf(_explorer);

        session.Load();
        Assert.Equal([new Bookmark("Work", Docs)], _explorer.Bookmarks.Items);
        Assert.Empty(_store.Saved);

        _explorer.Bookmarks.Add(Pictures);

        AppSettings saved = Assert.Single(_store.Saved);
        Assert.Equal(["Work", "Pictures"], saved.Bookmarks.Select(bookmark => bookmark.Name));
        Assert.Equal([Docs, Pictures], saved.Bookmarks.Select(bookmark => bookmark.Path));
    }

    [Fact]
    public void DirectoryArgument_IsTheFirstArgument_WhenItIsAnExistingDirectory()
    {
        Assert.Null(Session.DirectoryArgument([], _ => true));
        Assert.Null(Session.DirectoryArgument([string.Empty], _ => true));
        Assert.Null(Session.DirectoryArgument([Docs], _ => false));
        Assert.Equal(Docs, Session.DirectoryArgument([Docs, "ignored"], path => path == Docs));

        // Relative to where the explorer was started.
        Assert.Equal(Path.GetFullPath("."), Session.DirectoryArgument(["."], _ => true));
    }

    private static ExplorerHarness Explorer()
    {
        ExplorerHarness explorer = new();
        explorer.Reader.Add(Home, Directory(Home, "docs"), Directory(Home, "Pictures"), File(Home, "a.txt"));
        explorer.Reader.Add(Docs, File(Docs, "x.md"));
        explorer.Reader.Add(Pictures, File(Pictures, "p.png"));
        return explorer;
    }

    private static async Task TwoPanesAsync(ExplorerHarness explorer)
    {
        await explorer.Window.StartAsync();
        await explorer.RunAsync(CommandIds.SplitVertical, Docs);
        explorer.Pane.ViewMode = ViewMode.LargeIcons;
        explorer.Pane.Sort = EntrySort.For(SortKey.Size);
    }

    private Session SessionOf(ExplorerHarness explorer)
        => new(_store, explorer.Workspace, explorer.Options, explorer.Theme, explorer.Bookmarks);
}

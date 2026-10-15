using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

public sealed class PaneViewModelTests
{
    private readonly PaneHarness _harness = new();
    private readonly string _home;
    private readonly string _docs;

    public PaneViewModelTests()
    {
        _home = _harness.Reader.Add(
            PathOf("home"),
            File(PathOf("home"), "zeta.txt", size: 10),
            Directory(PathOf("home"), "docs"),
            File(PathOf("home"), ".profile", size: 5),
            File(PathOf("home"), "alpha.txt", size: 20),
            Directory(PathOf("home"), "Music"));

        _docs = _harness.Reader.Add(
            PathOf("home", "docs"),
            File(PathOf("home", "docs"), "a.md"),
            File(PathOf("home", "docs"), "b.md"),
            File(PathOf("home", "docs"), "c.md"));

        _harness.Reader.Add(Root, Directory(Root, "home"));
    }

    private PaneViewModel Pane => _harness.Pane;

    [Fact]
    public async Task StartAsync_OpensHome_DirectoriesFirst_HiddenLeftOut()
    {
        await Pane.StartAsync();

        Assert.Equal(_home, Pane.CurrentPath);
        Assert.Equal(["docs", "Music", "alpha.txt", "zeta.txt"], Names());
        Assert.Equal(0, Pane.CursorIndex);
        Assert.True(Pane.Items[0].IsCursor);
        Assert.Null(Pane.PlaceholderText);
        Assert.False(Pane.IsLoading);
    }

    [Fact]
    public async Task StartAsync_FallsBackToTheNearestExistingDirectory()
    {
        using TempDirectory temp = new();
        _harness.Reader.Add(temp.Path);

        await Pane.StartAsync(Path.Combine(temp.Path, "gone", "deeper"));

        Assert.Equal(temp.Path, Pane.CurrentPath);
    }

    [Fact]
    public async Task NavigateAsync_ToAMissingDirectory_StaysAndReportsTheError()
    {
        await Pane.StartAsync();

        bool navigated = await Pane.NavigateAsync(PathOf("nowhere"));

        Assert.False(navigated);
        Assert.Equal(_home, Pane.CurrentPath);
        Assert.True(_harness.Status.Current?.IsError);
        Assert.Contains("does not exist", _harness.Status.Current?.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NavigateAsync_ToAnUnreadableDirectory_ReportsAccessDenied()
    {
        await Pane.StartAsync();
        _harness.Reader.Fail(_docs, new UnauthorizedAccessException());

        await Pane.NavigateAsync(_docs);

        Assert.Equal(_home, Pane.CurrentPath);
        Assert.Contains("denied", _harness.Status.Current?.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NavigateAsync_FailingFirstLoad_ShowsTheErrorAsPlaceholder()
    {
        _harness.Reader.Fail(_home, new IOException("disk on fire"));

        await Pane.NavigateAsync(_home);

        Assert.Equal(string.Empty, Pane.CurrentPath);
        Assert.Contains("disk on fire", Pane.PlaceholderText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NavigateAsync_EmptyDirectory_ShowsTheEmptyPlaceholder()
    {
        string empty = _harness.Reader.Add(PathOf("empty"));

        await Pane.NavigateAsync(empty);

        Assert.Empty(Pane.Items);
        Assert.Equal(-1, Pane.CursorIndex);
        Assert.Null(Pane.CursorItem);
        Assert.Equal("This folder is empty", Pane.PlaceholderText);
    }

    [Fact]
    public async Task NavigateAsync_FocusesTheNamedEntry()
    {
        await Pane.NavigateAsync(_home, "alpha.txt");

        Assert.Equal("alpha.txt", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task NavigateAsync_ANewerNavigationWins()
    {
        await Pane.StartAsync();
        TaskCompletionSource gate = _harness.Reader.Hold(_docs);

        Task<bool> slow = Pane.NavigateAsync(_docs);
        Assert.True(Pane.IsLoading);
        bool fast = await Pane.NavigateAsync(Root);
        gate.SetResult();

        Assert.False(await slow);
        Assert.True(fast);
        Assert.Equal(Root, Pane.CurrentPath);
        Assert.False(Pane.IsLoading);
    }

    [Fact]
    public async Task OpenAsync_ADirectory_NavigatesIntoIt()
    {
        await Pane.StartAsync();

        await Pane.OpenAsync(Item("docs"));

        Assert.Equal(_docs, Pane.CurrentPath);
    }

    [Fact]
    public async Task OpenAsync_AFile_HandsItToTheLauncher()
    {
        await Pane.StartAsync();

        await Pane.OpenAsync(Item("alpha.txt"));

        Assert.Equal([Path.Combine(_home, "alpha.txt")], _harness.Launcher.Opened);
        Assert.Equal(_home, Pane.CurrentPath);
    }

    [Fact]
    public async Task OpenAsync_ALaunchFailure_IsReported()
    {
        await Pane.StartAsync();
        _harness.Launcher.FailWith = "no application";

        await Pane.OpenAsync(Item("alpha.txt"));

        Assert.Equal("no application", _harness.Status.Current?.Text);
        Assert.True(_harness.Status.Current?.IsError);
    }

    [Fact]
    public async Task OpenCursorAsync_OpensTheCursorEntry()
    {
        await Pane.StartAsync();

        await Pane.OpenCursorAsync();

        Assert.Equal(_docs, Pane.CurrentPath);
    }

    [Fact]
    public async Task GoParentAsync_PutsTheCursorOnTheDirectoryLeft()
    {
        await Pane.NavigateAsync(_docs);

        await Pane.GoParentAsync();

        Assert.Equal(_home, Pane.CurrentPath);
        Assert.Equal("docs", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task GoParentAsync_AtTheRoot_DoesNothing()
    {
        string root = PathUtilities.GetVolumeRoot(Root);
        _harness.Reader.Add(root);
        await Pane.NavigateAsync(root);

        Assert.False(Pane.CanGoParent);
        await Pane.GoParentAsync();

        Assert.Equal(root, Pane.CurrentPath);
    }

    [Fact]
    public async Task BackAndForward_RestoreLocationsAndCursors()
    {
        await Pane.StartAsync();
        Pane.MoveCursorTo(2);
        await Pane.NavigateAsync(_docs);
        Pane.MoveCursorTo(1);

        Assert.True(Pane.CanGoBack);
        Assert.False(Pane.CanGoForward);

        await Pane.GoBackAsync();

        Assert.Equal(_home, Pane.CurrentPath);
        Assert.Equal("alpha.txt", Pane.CursorItem?.Name);
        Assert.True(Pane.CanGoForward);

        await Pane.GoForwardAsync();

        Assert.Equal(_docs, Pane.CurrentPath);
        Assert.Equal("b.md", Pane.CursorItem?.Name);
        Assert.False(Pane.CanGoForward);
    }

    [Fact]
    public async Task NavigateAsync_AfterGoingBack_ClearsForwardHistory()
    {
        await Pane.StartAsync();
        await Pane.NavigateAsync(_docs);
        await Pane.GoBackAsync();

        await Pane.NavigateAsync(Root);

        Assert.False(Pane.CanGoForward);
    }

    [Fact]
    public async Task NavigateAsync_ToTheSameDirectory_DoesNotGrowHistory()
    {
        await Pane.StartAsync();

        await Pane.NavigateAsync(_home);

        Assert.False(Pane.CanGoBack);
    }

    [Fact]
    public async Task GoBackAsync_ToAVanishedDirectory_DropsItFromHistory()
    {
        await Pane.StartAsync();
        await Pane.NavigateAsync(_docs);
        _harness.Reader.Remove(_home);

        await Pane.GoBackAsync();

        Assert.Equal(_docs, Pane.CurrentPath);
        Assert.False(Pane.CanGoBack);
        Assert.True(_harness.Status.Current?.IsError);
    }

    [Fact]
    public async Task MoveCursor_ClampsAtBothEnds()
    {
        await Pane.StartAsync();

        Pane.MoveCursor(-5);
        Assert.Equal(0, Pane.CursorIndex);

        Pane.MoveCursor(100);
        Assert.Equal(3, Pane.CursorIndex);
        Assert.Single(Pane.Items, item => item.IsCursor);
        Assert.True(Pane.Items[3].IsCursor);
    }

    [Fact]
    public async Task Click_Plain_MovesTheCursorAndClearsMarks()
    {
        await Pane.StartAsync();
        Pane.MarkAll();

        Pane.Click(Item("alpha.txt"), ClickModifiers.None);

        Assert.Equal("alpha.txt", Pane.CursorItem?.Name);
        Assert.Equal(0, Pane.MarkedCount);
        Assert.Equal(["alpha.txt"], Pane.GetTargets().Select(item => item.Name));
    }

    [Fact]
    public async Task Click_Toggle_MarksAndUnmarks()
    {
        await Pane.StartAsync();

        Pane.Click(Item("alpha.txt"), ClickModifiers.Toggle);
        Pane.Click(Item("zeta.txt"), ClickModifiers.Toggle);

        Assert.Equal(2, Pane.MarkedCount);
        Assert.Equal(30, Pane.MarkedSize);
        Assert.Equal(["alpha.txt", "zeta.txt"], Pane.GetTargets().Select(item => item.Name));

        Pane.Click(Item("alpha.txt"), ClickModifiers.Toggle);

        Assert.Equal(1, Pane.MarkedCount);
    }

    [Fact]
    public async Task Click_Range_MarksFromTheAnchor()
    {
        await Pane.StartAsync();
        Pane.Click(Item("Music"), ClickModifiers.None);

        Pane.Click(Item("zeta.txt"), ClickModifiers.Range);

        Assert.Equal(["Music", "alpha.txt", "zeta.txt"], Pane.GetTargets().Select(item => item.Name));
        Assert.Equal("zeta.txt", Pane.CursorItem?.Name);

        Pane.Click(Item("docs"), ClickModifiers.Range);

        Assert.Equal(["docs", "Music"], Pane.GetTargets().Select(item => item.Name));
    }

    [Fact]
    public async Task ContextClick_OnAnUnmarkedEntry_MakesItTheOnlyTarget()
    {
        await Pane.StartAsync();
        Pane.Click(Item("alpha.txt"), ClickModifiers.Toggle);
        Pane.Click(Item("docs"), ClickModifiers.Toggle);

        Pane.ContextClick(Item("zeta.txt"));

        Assert.Equal("zeta.txt", Pane.CursorItem?.Name);
        Assert.Equal(0, Pane.MarkedCount);
        Assert.Equal(["zeta.txt"], Pane.GetTargets().Select(item => item.Name));
    }

    [Fact]
    public async Task ContextClick_OnAMarkedEntry_KeepsTheMarks_AndLeavesVisualMode()
    {
        await Pane.StartAsync();
        Pane.StartVisual();
        Pane.MoveCursorTo(2);

        Pane.ContextClick(Item("docs"));

        Assert.False(Pane.IsVisualMode);
        Assert.Equal("docs", Pane.CursorItem?.Name);
        Assert.Equal(["docs", "Music", "alpha.txt"], Pane.GetTargets().Select(item => item.Name));
    }

    [Fact]
    public async Task RequestContextMenu_RaisesTheEvent()
    {
        await Pane.StartAsync();
        int raised = 0;
        Pane.ContextMenuRequested += (_, _) => raised++;

        Pane.RequestContextMenu();

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task MarkAllAndClearMarks()
    {
        await Pane.StartAsync();

        Pane.MarkAll();
        Assert.Equal(4, Pane.MarkedCount);

        Pane.ClearMarks();
        Assert.Equal(0, Pane.MarkedCount);
        Assert.Equal(0, Pane.MarkedSize);
    }

    [Fact]
    public async Task GetTargets_OnAnEmptyPane_IsEmpty()
    {
        await Pane.NavigateAsync(_harness.Reader.Add(PathOf("empty")));

        Assert.Empty(Pane.GetTargets());
    }

    [Fact]
    public async Task ShowHidden_ListsHiddenEntriesAndKeepsTheCursor()
    {
        await Pane.StartAsync();
        Pane.MoveCursorTo(3);

        Pane.ShowHidden = true;

        Assert.Equal(["docs", "Music", ".profile", "alpha.txt", "zeta.txt"], Names());
        Assert.Equal("zeta.txt", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task Sort_ReordersAndKeepsTheCursorAndMarks()
    {
        await Pane.StartAsync();
        Pane.Click(Item("alpha.txt"), ClickModifiers.Toggle);

        Pane.Sort = new EntrySort(SortKey.Size, SortDirection.Descending);

        Assert.Equal(["docs", "Music", "alpha.txt", "zeta.txt"], Names());
        Assert.Equal("alpha.txt", Pane.CursorItem?.Name);
        Assert.True(Item("alpha.txt").IsMarked);
    }

    [Fact]
    public async Task RefreshAsync_KeepsTheCursorAndMarksByName()
    {
        await Pane.StartAsync();
        Pane.Click(Item("zeta.txt"), ClickModifiers.Toggle);
        _harness.Reader.Add(
            _home,
            File(_home, "zeta.txt", size: 10),
            File(_home, "new.txt"),
            Directory(_home, "docs"));

        await Pane.RefreshAsync();

        Assert.Equal(["docs", "new.txt", "zeta.txt"], Names());
        Assert.Equal("zeta.txt", Pane.CursorItem?.Name);
        Assert.True(Item("zeta.txt").IsMarked);
        Assert.False(Pane.CanGoBack);
    }

    [Fact]
    public async Task RefreshAsync_WhenTheCursorEntryVanished_StaysAtTheSameIndex()
    {
        await Pane.StartAsync();
        Pane.MoveCursorTo(2);
        _harness.Reader.Add(_home, Directory(_home, "docs"), Directory(_home, "Music"), File(_home, "zeta.txt"));

        await Pane.RefreshAsync();

        Assert.Equal(2, Pane.CursorIndex);
        Assert.Equal("zeta.txt", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task Breadcrumbs_FollowTheCurrentPath()
    {
        await Pane.NavigateAsync(_docs);

        Assert.Equal(_docs, Pane.Breadcrumbs[^1].FullPath);
        Assert.Equal("docs", Pane.Breadcrumbs[^1].Name);
        Assert.Equal("docs", Pane.DisplayName);
        Assert.Equal(_home, Pane.Breadcrumbs[^2].FullPath);
    }

    [Fact]
    public async Task NavigateToSegmentCommand_Navigates()
    {
        await Pane.NavigateAsync(_docs);

        await Pane.NavigateToSegmentCommand.ExecuteAsync(Pane.Breadcrumbs[^2]);

        Assert.Equal(_home, Pane.CurrentPath);
    }

    [Fact]
    public async Task EditPath_ARelativePath_IsResolvedAgainstTheCurrentDirectory()
    {
        await Pane.StartAsync();
        Pane.BeginEditPath();
        Assert.True(Pane.IsEditingPath);
        Assert.Equal(_home, Pane.EditPathText);

        Pane.EditPathText = "docs";
        await Pane.CommitEditPathAsync();

        Assert.Equal(_docs, Pane.CurrentPath);
        Assert.False(Pane.IsEditingPath);
    }

    [Fact]
    public async Task EditPath_Tilde_IsHome()
    {
        await Pane.NavigateAsync(Root);
        Pane.BeginEditPath();

        Pane.EditPathText = "~/docs";
        await Pane.CommitEditPathAsync();

        Assert.Equal(_docs, Pane.CurrentPath);
    }

    [Fact]
    public async Task EditPath_AMissingPath_KeepsTheBoxOpen()
    {
        await Pane.StartAsync();
        Pane.BeginEditPath();

        Pane.EditPathText = "nowhere";
        await Pane.CommitEditPathAsync();

        Assert.True(Pane.IsEditingPath);
        Assert.Equal(_home, Pane.CurrentPath);
        Assert.True(_harness.Status.Current?.IsError);
    }

    [Fact]
    public async Task EditPath_AFile_OpensItsDirectoryOnIt()
    {
        using TempDirectory temp = new();
        string file = temp.CreateFile("report.pdf");
        _harness.Reader.Add(temp.Path, File(temp.Path, "a.txt"), File(temp.Path, "report.pdf"));
        await Pane.StartAsync();
        Pane.BeginEditPath();

        Pane.EditPathText = file;
        await Pane.CommitEditPathAsync();

        Assert.Equal(temp.Path, Pane.CurrentPath);
        Assert.Equal("report.pdf", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task CancelEditPath_LeavesTheBox()
    {
        await Pane.StartAsync();
        Pane.BeginEditPath();

        Pane.CancelEditPath();

        Assert.False(Pane.IsEditingPath);
    }

    private string[] Names() => [.. Pane.Items.Select(item => item.Name)];

    private EntryViewModel Item(string name) => Pane.Items.Single(item => item.Name == name);
}

using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

public sealed class WorkspaceViewModelTests
{
    private readonly ExplorerHarness _explorer = new();
    private readonly string _home;
    private readonly string _docs;

    public WorkspaceViewModelTests()
    {
        _home = _explorer.Reader.Add(PathOf("home"), Directory(PathOf("home"), "docs"), File(PathOf("home"), "a.txt"), File(PathOf("home"), ".rc"));
        _docs = _explorer.Reader.Add(PathOf("home", "docs"), File(PathOf("home", "docs"), "x.md"));
    }

    private WorkspaceViewModel Workspace => _explorer.Workspace;

    [Fact]
    public void ANewWorkspace_HasOneActiveUnframedPane()
    {
        PaneViewModel pane = Assert.Single(Workspace.Panes);

        Assert.Same(pane, Workspace.ActivePane);
        Assert.True(pane.IsActive);
        Assert.False(pane.IsFramed);
    }

    [Fact]
    public async Task Split_OpensTheSamePlaceTheSameWay_AndActivatesTheNewPane()
    {
        await Workspace.ActivePane.StartAsync();
        PaneViewModel source = Workspace.ActivePane;
        _explorer.Options.ShowHidden = true;
        source.Sort = new EntrySort(SortKey.Size, SortDirection.Descending);
        source.ViewMode = ViewMode.LargeIcons;
        source.MoveCursorTo(2);

        PaneViewModel created = await Workspace.SplitAsync(SplitAxis.Columns);

        Assert.Equal(2, Workspace.Panes.Count);
        Assert.Same(created, Workspace.ActivePane);
        Assert.Equal(_home, created.CurrentPath);
        Assert.Equal(source.CursorItem?.Name, created.CursorItem?.Name);
        Assert.True(created.ShowHidden);
        Assert.Equal(source.Sort, created.Sort);
        Assert.Equal(ViewMode.LargeIcons, created.ViewMode);
        Assert.True(created.IsFramed);
        Assert.False(source.IsActive);
        Assert.False(source.IsFramed);
    }

    [Fact]
    public async Task Split_WithAPath_OpensThere()
    {
        await Workspace.ActivePane.StartAsync();

        PaneViewModel created = await Workspace.SplitAsync(SplitAxis.Rows, _docs);

        Assert.Equal(_docs, created.CurrentPath);
        Assert.Equal(_home, Workspace.Panes[0].CurrentPath);
    }

    [Fact]
    public async Task Close_ActivatesTheSuccessorAndStopsTheClosedPane()
    {
        await Workspace.ActivePane.StartAsync();
        PaneViewModel first = Workspace.ActivePane;
        PaneViewModel second = await Workspace.SplitAsync(SplitAxis.Columns);
        int watching = _explorer.Watchers.Active.Count();

        Assert.True(Workspace.Close());

        Assert.Same(first, Workspace.ActivePane);
        Assert.Equal([first], Workspace.Panes);
        Assert.Null(Workspace.IdOf(second));
        Assert.Equal(watching - 1, _explorer.Watchers.Active.Count());
        Assert.False(first.IsFramed);
    }

    [Fact]
    public void Close_TheLastPane_IsRefused() => Assert.False(Workspace.Close());

    [Fact]
    public async Task Only_KeepsTheActivePane()
    {
        await Workspace.ActivePane.StartAsync();
        await Workspace.SplitAsync(SplitAxis.Columns);
        PaneViewModel kept = await Workspace.SplitAsync(SplitAxis.Rows);

        Workspace.Only();

        Assert.Equal([kept], Workspace.Panes);
        Assert.Same(kept, Workspace.ActivePane);
    }

    [Fact]
    public async Task Focus_MovesSpatially_AndNextWraps()
    {
        await Workspace.ActivePane.StartAsync();
        PaneViewModel left = Workspace.ActivePane;
        PaneViewModel right = await Workspace.SplitAsync(SplitAxis.Columns);

        Assert.True(Workspace.Focus(FocusDirection.Left));
        Assert.Same(left, Workspace.ActivePane);
        Assert.False(Workspace.Focus(FocusDirection.Left));

        Workspace.FocusNext();
        Assert.Same(right, Workspace.ActivePane);
        Workspace.FocusNext();
        Assert.Same(left, Workspace.ActivePane);
        Workspace.FocusPrevious();
        Assert.Same(right, Workspace.ActivePane);
    }

    [Fact]
    public async Task Activate_AnotherPane()
    {
        await Workspace.ActivePane.StartAsync();
        PaneViewModel left = Workspace.ActivePane;
        await Workspace.SplitAsync(SplitAxis.Columns);

        Workspace.Activate(left);

        Assert.Same(left, Workspace.ActivePane);
        Assert.True(left.IsFramed);
    }

    [Fact]
    public async Task TheToolbarCommands_ActOnTheirPane()
    {
        await Workspace.ActivePane.StartAsync();
        PaneViewModel left = Workspace.ActivePane;
        PaneViewModel right = await Workspace.SplitAsync(SplitAxis.Columns);

        await Workspace.SplitRowsCommand.ExecuteAsync(left);
        Assert.Equal(3, Workspace.Panes.Count);
        Assert.Equal(new LayoutRect(0, 0.5, 0.5, 0.5), Workspace.Layout.Geometry()[Workspace.ActivePaneId]);

        Workspace.CloseCommand.Execute(right);
        Assert.Equal(2, Workspace.Panes.Count);
        Assert.Null(Workspace.IdOf(right));
    }

    [Fact]
    public async Task TheTitle_FollowsTheActivePane()
    {
        await _explorer.Window.StartAsync();
        Assert.StartsWith("home", _explorer.Window.Title, System.StringComparison.Ordinal);

        await Workspace.SplitAsync(SplitAxis.Columns, _docs);

        Assert.StartsWith("docs", _explorer.Window.Title, System.StringComparison.Ordinal);
    }
}

using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Commands;

/// <summary>
/// The leader keys, the g-direction pane keys and the window ex commands, typed.
/// </summary>
public sealed class WindowCommandsTests
{
    private readonly ExplorerHarness _explorer = new();
    private readonly string _docs;

    public WindowCommandsTests()
    {
        _explorer.Reader.Add(PathOf("home"), Directory(PathOf("home"), "docs"), File(PathOf("home"), "a.txt"));
        _docs = _explorer.Reader.Add(PathOf("home", "docs"));
    }

    private WorkspaceViewModel Workspace => _explorer.Workspace;

    private async Task StartAsync() => await Workspace.ActivePane.StartAsync();

    [Fact]
    public async Task LeaderV_And_LeaderS_Split()
    {
        await StartAsync();

        _explorer.Press("<Space>v");
        await Task.Yield();
        Assert.Equal(2, Workspace.Panes.Count);
        Assert.Equal(SplitAxis.Columns, ((SplitNode)Workspace.Layout.Root).Axis);

        _explorer.Press("<Space>s");
        await Task.Yield();
        Assert.Equal(3, Workspace.Panes.Count);
    }

    [Fact]
    public async Task GHJKL_MoveBetweenPanes_AndTheLeaderCyclesThem()
    {
        await StartAsync();
        PaneViewModel left = Workspace.ActivePane;
        _explorer.Press("<Space>v");
        PaneViewModel right = Workspace.ActivePane;
        _explorer.Press("<Space>s");
        PaneViewModel bottomRight = Workspace.ActivePane;
        await Task.Yield();

        _explorer.Press("gk");
        Assert.Same(right, Workspace.ActivePane);

        _explorer.Press("gh");
        Assert.Same(left, Workspace.ActivePane);

        _explorer.Press("gl");
        Assert.Same(bottomRight, Workspace.ActivePane);

        _explorer.Press("gk");
        _explorer.Press("gj");
        Assert.Same(bottomRight, Workspace.ActivePane);

        _explorer.Press("<Space>w");
        Assert.Same(left, Workspace.ActivePane);

        _explorer.Press("<Space>W");
        Assert.Same(bottomRight, Workspace.ActivePane);
    }

    [Fact]
    public async Task LeaderC_Closes_ButNotTheLastPane()
    {
        await StartAsync();
        _explorer.Press("<Space>v");

        _explorer.Press("<Space>c");
        Assert.Single(Workspace.Panes);

        _explorer.Press("<Space>c");
        Assert.Single(Workspace.Panes);
        Assert.Equal("Cannot close the last pane", _explorer.Status.Current?.Text);
        Assert.Equal(0, _explorer.Shutdown.Requests);
    }

    [Fact]
    public async Task LeaderO_KeepsOnlyTheActivePane()
    {
        await StartAsync();
        _explorer.Press("<Space>v<Space>s");
        PaneViewModel active = Workspace.ActivePane;

        _explorer.Press("<Space>o");

        Assert.Equal([active], Workspace.Panes);
    }

    [Fact]
    public async Task ResizeKeys_AndEqualize()
    {
        await StartAsync();
        _explorer.Press("<Space>v");

        _explorer.Press("<Space>>");
        Assert.Equal(0.55, Workspace.Layout.Geometry()[Workspace.ActivePaneId].Width, 6);

        _explorer.Press("2<Space><lt>");
        Assert.Equal(0.45, Workspace.Layout.Geometry()[Workspace.ActivePaneId].Width, 6);

        _explorer.Press("<Space>=");
        Assert.Equal(0.5, Workspace.Layout.Geometry()[Workspace.ActivePaneId].Width, 6);

        _explorer.Press("<Space>s<Space>+");
        Assert.Equal(0.55, Workspace.Layout.Geometry()[Workspace.ActivePaneId].Height, 6);

        _explorer.Press("<Space>-<Space>-");
        Assert.Equal(0.45, Workspace.Layout.Geometry()[Workspace.ActivePaneId].Height, 6);
    }

    [Fact]
    public async Task VsplitWithAPath_OpensItBesides()
    {
        await StartAsync();

        _explorer.Ex("vs docs");
        await Task.Yield();

        Assert.Equal(2, Workspace.Panes.Count);
        Assert.Equal(_docs, Workspace.ActivePane.CurrentPath);
    }

    [Fact]
    public async Task Q_ClosesThePane_AndQuitsWithTheLast()
    {
        await StartAsync();
        _explorer.Ex("sp");
        await Task.Yield();

        _explorer.Ex("q");
        Assert.Single(Workspace.Panes);
        Assert.Equal(0, _explorer.Shutdown.Requests);

        _explorer.Ex("q");
        Assert.Equal(1, _explorer.Shutdown.Requests);
    }

    [Fact]
    public async Task Qa_Quits()
    {
        await StartAsync();
        _explorer.Ex("vs");

        _explorer.Ex("qa");

        Assert.Equal(1, _explorer.Shutdown.Requests);
    }

    [Fact]
    public async Task CloseAndOnly_AlsoAsExCommands()
    {
        await StartAsync();
        _explorer.Ex("vs");
        _explorer.Ex("sp");

        _explorer.Ex("clo");
        Assert.Equal(2, Workspace.Panes.Count);

        _explorer.Ex("on");
        Assert.Single(Workspace.Panes);
    }
}

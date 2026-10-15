using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.Commands;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Input;

/// <summary>
/// Types the normal-mode keys against a workspace and checks the panes respond as vim users expect.
/// </summary>
public sealed class NormalModeTests
{
    private readonly ExplorerHarness _explorer = new();
    private readonly string _home;
    private readonly string _sub;

    public NormalModeTests()
    {
        string[] files = [.. Enumerable.Range(1, 30).Select(i => $"file{i:00}.txt")];
        _home = _explorer.Reader.Add(PathOf("home"), [Directory(PathOf("home"), "sub"), .. files.Select(name => File(PathOf("home"), name, size: 10))]);
        _sub = _explorer.Reader.Add(PathOf("home", "sub"), File(PathOf("home", "sub"), "inner.txt"));
        _explorer.Reader.Add(PathOf("docs"), File(PathOf("docs"), "doc.md"));
        _explorer.Reader.Add(Root, Directory(Root, "home"), Directory(Root, "docs"));
    }

    private async Task StartAsync()
    {
        await _explorer.Pane.StartAsync();
        _explorer.Pane.PageSize = 10;
    }

    [Fact]
    public async Task J_And_K_MoveTheCursor()
    {
        await StartAsync();

        _explorer.Press("jjj");
        Assert.Equal(3, _explorer.Pane.CursorIndex);

        _explorer.Press("k");
        Assert.Equal(2, _explorer.Pane.CursorIndex);

        _explorer.Press("<Down><Down><Up>");
        Assert.Equal(3, _explorer.Pane.CursorIndex);
    }

    [Fact]
    public async Task ACount_RepeatsTheMotion()
    {
        await StartAsync();

        _explorer.Press("12j");

        Assert.Equal(12, _explorer.Pane.CursorIndex);
    }

    [Fact]
    public async Task Gg_And_G_GoToTheEnds_AndCountedGGoesToTheNth()
    {
        await StartAsync();

        _explorer.Press("G");
        Assert.Equal(30, _explorer.Pane.CursorIndex);

        _explorer.Press("gg");
        Assert.Equal(0, _explorer.Pane.CursorIndex);

        _explorer.Press("5G");
        Assert.Equal(4, _explorer.Pane.CursorIndex);

        _explorer.Press("3gg");
        Assert.Equal(2, _explorer.Pane.CursorIndex);
    }

    [Fact]
    public async Task Paging_UsesThePageSize()
    {
        await StartAsync();

        _explorer.Press("<C-d>");
        Assert.Equal(5, _explorer.Pane.CursorIndex);

        _explorer.Press("<PageDown>");
        Assert.Equal(15, _explorer.Pane.CursorIndex);

        _explorer.Press("<C-u>");
        Assert.Equal(10, _explorer.Pane.CursorIndex);

        _explorer.Press("<PageUp>");
        Assert.Equal(0, _explorer.Pane.CursorIndex);
    }

    [Fact]
    public async Task L_Enters_H_Leaves_AndTheHistoryKeysWork()
    {
        await StartAsync();

        _explorer.Press("l");
        Assert.Equal(_sub, _explorer.Pane.CurrentPath);

        _explorer.Press("h");
        Assert.Equal(_home, _explorer.Pane.CurrentPath);
        Assert.Equal("sub", _explorer.Pane.CursorItem?.Name);

        _explorer.Press("H");
        Assert.Equal(_sub, _explorer.Pane.CurrentPath);

        _explorer.Press("L");
        Assert.Equal(_home, _explorer.Pane.CurrentPath);
    }

    [Fact]
    public async Task EnterOpensAFileAndBackspaceGoesUp()
    {
        await StartAsync();

        _explorer.Press("j<CR>");
        Assert.Equal(PathOf("home", "file01.txt"), Assert.Single(_explorer.Launcher.Opened));

        _explorer.Press("<BS>");
        Assert.Equal(Root, _explorer.Pane.CurrentPath);
    }

    [Fact]
    public async Task ArrowsMoveThroughTheTreeInTheDetailsView()
    {
        await StartAsync();

        _explorer.Press("<Right>");
        Assert.Equal(_sub, _explorer.Pane.CurrentPath);

        _explorer.Press("<Left>");
        Assert.Equal(_home, _explorer.Pane.CurrentPath);
    }

    [Fact]
    public async Task GShiftH_GoesHome_AndGSlashGoesToTheVolumeRoot()
    {
        string volumeRoot = Core.FileSystem.PathUtilities.GetVolumeRoot(Root);
        _explorer.Reader.Add(volumeRoot);
        await _explorer.Pane.NavigateAsync(PathOf("docs"));

        _explorer.Press("gH");
        Assert.Equal(_home, _explorer.Pane.CurrentPath);

        _explorer.Press("g/");
        Assert.Equal(volumeRoot, _explorer.Pane.CurrentPath);
    }

    [Fact]
    public async Task GDigit_GoesToThatPlace()
    {
        _explorer.PlacesProvider.Places.Add(new Place("Home", _home, PlaceKind.Home));
        _explorer.PlacesProvider.Places.Add(new Place("Docs", PathOf("docs"), PlaceKind.Documents));
        await _explorer.Places.RefreshAsync();
        await StartAsync();

        _explorer.Press("g2");
        Assert.Equal(PathOf("docs"), _explorer.Pane.CurrentPath);

        _explorer.Press("g9");
        Assert.Equal("No place 9", _explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task M_MarksAndMovesOn_AsDoesInsert()
    {
        await StartAsync();

        _explorer.Press("jmm");

        Assert.Equal(["file01.txt", "file02.txt"], _explorer.Pane.GetTargets().Select(item => item.Name));
        Assert.Equal(3, _explorer.Pane.CursorIndex);

        _explorer.Press("3m");
        Assert.Equal(5, _explorer.Pane.MarkedCount);

        _explorer.Press("<Insert>");
        Assert.Equal(6, _explorer.Pane.MarkedCount);
    }

    [Fact]
    public async Task Space_IsTheLeader_AndMarksNothing()
    {
        await StartAsync();

        Assert.True(_explorer.Press("<Space>"));
        Assert.Equal("<Space>", _explorer.Keyboard.PendingKeys);

        _explorer.Press("<Esc>");
        Assert.Equal(0, _explorer.Pane.MarkedCount);
        Assert.Equal(string.Empty, _explorer.Keyboard.PendingKeys);
    }

    [Fact]
    public async Task VisualMode_MarksTheRangeAndVKeepsIt()
    {
        await StartAsync();

        _explorer.Press("jvjj");

        Assert.True(_explorer.Pane.IsVisualMode);
        Assert.Equal("VISUAL", _explorer.Keyboard.ModeText);
        Assert.Equal(["file01.txt", "file02.txt", "file03.txt"], _explorer.Pane.GetTargets().Select(item => item.Name));

        _explorer.Press("k");
        Assert.Equal(2, _explorer.Pane.MarkedCount);

        _explorer.Press("v");
        Assert.False(_explorer.Pane.IsVisualMode);
        Assert.Equal("NORMAL", _explorer.Keyboard.ModeText);
        Assert.Equal(2, _explorer.Pane.MarkedCount);
    }

    [Fact]
    public async Task VisualMode_UpwardsFromTheAnchor()
    {
        await StartAsync();

        _explorer.Press("5jvkk");

        Assert.Equal(["file03.txt", "file04.txt", "file05.txt"], _explorer.Pane.GetTargets().Select(item => item.Name));
    }

    [Fact]
    public async Task Escape_LeavesVisualModeAndClearsTheMarks()
    {
        await StartAsync();
        _explorer.Press("vjj");

        _explorer.Press("<Esc>");

        Assert.False(_explorer.Pane.IsVisualMode);
        Assert.Equal(0, _explorer.Pane.MarkedCount);
    }

    [Fact]
    public async Task Escape_ClearsMarksAndTheMessage()
    {
        await StartAsync();
        _explorer.Press("<C-a>");
        Assert.Equal(31, _explorer.Pane.MarkedCount);
        _explorer.Status.Error("old news");

        _explorer.Press("<Esc>");

        Assert.Equal(0, _explorer.Pane.MarkedCount);
        Assert.Null(_explorer.Status.Current);
    }

    [Fact]
    public async Task Escape_CancelsAPendingSequenceFirst()
    {
        await StartAsync();
        _explorer.Press("<C-a>");

        _explorer.Press("5g");
        Assert.Equal("5g", _explorer.Keyboard.PendingKeys);

        _explorer.Press("<Esc>");

        Assert.Equal(string.Empty, _explorer.Keyboard.PendingKeys);
        Assert.Equal(31, _explorer.Pane.MarkedCount);
    }

    [Fact]
    public async Task CtrlL_EditsThePath_AndRRefreshes()
    {
        await StartAsync();

        _explorer.Press("<C-l>");
        Assert.True(_explorer.Pane.IsEditingPath);

        _explorer.Pane.CancelEditPath();
        _explorer.Reader.Add(_home, File(_home, "only.txt"));
        _explorer.Press("R");
        Assert.Single(_explorer.Pane.Items);
    }

    [Fact]
    public async Task AnUnwiredCommand_SaysSo()
    {
        // Only navigation is wired here, so a file command is guaranteed to have no handler.
        ExplorerHarness navigationOnly = new(harness => [new NavigationCommands(harness.Workspace, harness.Places, harness.Folders, harness.Status)]);
        navigationOnly.Reader.Add(PathOf("home"), File(PathOf("home"), "a.txt"));
        await navigationOnly.Pane.StartAsync();

        navigationOnly.Press("yy");

        Assert.Equal("Not available yet: Copy", navigationOnly.Status.Current?.Text);
    }

    [Fact]
    public async Task AnUnboundKey_IsNotConsumed()
    {
        await StartAsync();

        Assert.False(_explorer.Press("<Tab>"));
        Assert.True(_explorer.Press("j"));
    }
}

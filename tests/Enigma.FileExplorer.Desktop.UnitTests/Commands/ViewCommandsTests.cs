using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Commands;

/// <summary>
/// View modes, grid motions, sorting and hidden files, typed.
/// </summary>
public sealed class ViewCommandsTests
{
    private readonly ExplorerHarness _explorer = new();
    private readonly string _home;

    public ViewCommandsTests()
    {
        _home = _explorer.Reader.Add(
            PathOf("home"),
            [
                Directory(PathOf("home"), "sub"),
                File(PathOf("home"), ".hidden", size: 1),
                .. Enumerable.Range(1, 11).Select(i => File(PathOf("home"), $"f{i:00}.txt", size: i * 100)),
            ]);
        _explorer.Reader.Add(PathOf("home", "sub"), File(PathOf("home", "sub"), "in.txt"));
        _explorer.Reader.Add(Root, Directory(Root, "home"));
    }

    private PaneViewModel Pane => _explorer.Pane;

    private Task StartAsync() => Pane.StartAsync();

    [Fact]
    public async Task ZdZmZl_SwitchTheViewMode()
    {
        await StartAsync();

        _explorer.Press("zm");
        Assert.Equal(ViewMode.MediumIcons, Pane.ViewMode);
        Assert.True(Pane.IsIconView);

        _explorer.Press("zl");
        Assert.Equal(ViewMode.LargeIcons, Pane.ViewMode);

        _explorer.Press("zd");
        Assert.Equal(ViewMode.Details, Pane.ViewMode);
        Assert.Empty(Pane.Rows);
    }

    [Fact]
    public async Task TheExplorerChords_AlsoSwitch()
    {
        await StartAsync();

        _explorer.Press("<C-S-3>");
        Assert.Equal(ViewMode.MediumIcons, Pane.ViewMode);

        _explorer.Press("<C-S-2>");
        Assert.Equal(ViewMode.LargeIcons, Pane.ViewMode);

        _explorer.Press("<C-S-6>");
        Assert.Equal(ViewMode.Details, Pane.ViewMode);
    }

    [Fact]
    public async Task ViewEx_AcceptsPrefixes_AndReportsUnknownModes()
    {
        await StartAsync();

        _explorer.Ex("view large");
        Assert.Equal(ViewMode.LargeIcons, Pane.ViewMode);

        _explorer.Ex("view m");
        Assert.Equal(ViewMode.MediumIcons, Pane.ViewMode);

        _explorer.Ex("view huge");
        Assert.Equal(ViewMode.MediumIcons, Pane.ViewMode);
        Assert.StartsWith("Unknown view: huge", _explorer.Status.Current?.Text, System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task IconViews_ChunkTheEntriesIntoRows()
    {
        await StartAsync();
        Pane.ViewMode = ViewMode.MediumIcons;

        Pane.ItemsPerRow = 5;

        Assert.Equal([5, 5, 2], Pane.Rows.Select(row => row.Items.Count));
        Assert.Same(Pane.Items[5], Pane.Rows[1].Items[0]);

        Pane.ItemsPerRow = 4;
        Assert.Equal(3, Pane.Rows.Count);
    }

    [Fact]
    public async Task InAGrid_JKMoveARow_ArrowsMoveATile_HLStillNavigate()
    {
        await StartAsync();
        Pane.ViewMode = ViewMode.MediumIcons;
        Pane.ItemsPerRow = 4;

        _explorer.Press("j");
        Assert.Equal(4, Pane.CursorIndex);

        _explorer.Press("2j");
        Assert.Equal(11, Pane.CursorIndex);

        _explorer.Press("k<Left><Left>");
        Assert.Equal(5, Pane.CursorIndex);

        _explorer.Press("<Right>");
        Assert.Equal(6, Pane.CursorIndex);

        _explorer.Press("gg<Down>l");
        Assert.Equal(PathOf("home", "f04.txt"), Assert.Single(_explorer.Launcher.Opened));

        _explorer.Press("h");
        Assert.Equal(Root, Pane.CurrentPath);
    }

    [Fact]
    public async Task SortKeys_UseNaturalDirections_AndOrReverses()
    {
        await StartAsync();

        _explorer.Press("os");
        Assert.Equal(new EntrySort(SortKey.Size, SortDirection.Descending), Pane.Sort);
        Assert.Equal("f11.txt", Pane.Items[1].Name);

        _explorer.Press("or");
        Assert.Equal(new EntrySort(SortKey.Size, SortDirection.Ascending), Pane.Sort);

        _explorer.Press("om");
        Assert.Equal(SortDirection.Descending, Pane.Sort.Direction);

        _explorer.Press("ot");
        Assert.Equal(new EntrySort(SortKey.Type, SortDirection.Ascending), Pane.Sort);

        _explorer.Press("on");
        Assert.Equal(EntrySort.Default, Pane.Sort);
    }

    [Fact]
    public async Task SortEx_WithBangReverses_AndReportsUnknownKeys()
    {
        await StartAsync();

        _explorer.Ex("sort size");
        Assert.Equal(new EntrySort(SortKey.Size, SortDirection.Descending), Pane.Sort);

        _explorer.Ex("sort! name");
        Assert.Equal(new EntrySort(SortKey.Name, SortDirection.Descending), Pane.Sort);

        _explorer.Ex("sort colour");
        Assert.StartsWith("Unknown sort: colour", _explorer.Status.Current?.Text, System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task HeaderClicks_SortThenReverse_AndShowTheArrow()
    {
        await StartAsync();

        Pane.SortByCommand.Execute(SortKey.Size);
        Assert.Equal("Size ↓", Pane.SizeHeader);
        Assert.Equal("Name", Pane.NameHeader);

        Pane.SortByCommand.Execute(SortKey.Size);
        Assert.Equal("Size ↑", Pane.SizeHeader);

        Pane.SortByCommand.Execute(SortKey.Name);
        Assert.Equal("Name ↑", Pane.NameHeader);
    }

    [Fact]
    public async Task Zh_ShowsHiddenFilesInEveryPane()
    {
        await StartAsync();
        await _explorer.Workspace.SplitAsync(SplitAxis.Columns);
        int visible = Pane.Items.Count;

        _explorer.Press("zh");

        Assert.True(_explorer.Options.ShowHidden);
        Assert.All(_explorer.Workspace.Panes, pane => Assert.Equal(visible + 1, pane.Items.Count));
        Assert.Equal("Hidden files shown", _explorer.Status.Current?.Text);

        _explorer.Press("zh");
        Assert.All(_explorer.Workspace.Panes, pane => Assert.Equal(visible, pane.Items.Count));
    }

    [Fact]
    public async Task SetEx_HiddenNohiddenAndToggle()
    {
        await StartAsync();

        _explorer.Ex("set hidden");
        Assert.True(_explorer.Options.ShowHidden);

        _explorer.Ex("set nohidden");
        Assert.False(_explorer.Options.ShowHidden);

        _explorer.Ex("set hidden!");
        Assert.True(_explorer.Options.ShowHidden);

        _explorer.Ex("set number");
        Assert.Equal("Unknown option: number", _explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task SetEx_ConfirmTrash_SwitchesTheQuestion()
    {
        await StartAsync();
        Assert.True(_explorer.Options.ConfirmTrash);

        _explorer.Ex("set noconfirmtrash");
        Assert.False(_explorer.Options.ConfirmTrash);
        Assert.Equal("Moving to the trash no longer asks", _explorer.Status.Current?.Text);

        _explorer.Ex("set confirmtrash");
        Assert.True(_explorer.Options.ConfirmTrash);
        Assert.Equal("Moving to the trash asks first", _explorer.Status.Current?.Text);

        _explorer.Ex("set confirmtrash!");
        Assert.False(_explorer.Options.ConfirmTrash);
    }

    [Fact]
    public async Task ANewPane_FollowsTheHiddenSetting()
    {
        await StartAsync();
        _explorer.Press("zh");

        PaneViewModel created = await _explorer.Workspace.SplitAsync(SplitAxis.Rows);

        Assert.True(created.ShowHidden);
    }
}

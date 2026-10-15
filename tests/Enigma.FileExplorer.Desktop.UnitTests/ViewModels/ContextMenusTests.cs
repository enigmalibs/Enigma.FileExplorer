using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Core.Search;
using Enigma.FileExplorer.Desktop.Commands;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

/// <summary>
/// The item and background menus: what they offer in each state, the keys they show, and that each entry
/// runs the command its key runs.
/// </summary>
public sealed class ContextMenusTests
{
    private readonly ExplorerHarness _explorer = new();
    private readonly string _home;
    private readonly string _docs;

    public ContextMenusTests()
    {
        _home = _explorer.Reader.Add(PathOf("home"), Directory(PathOf("home"), "docs"), File(PathOf("home"), "a.txt"), File(PathOf("home"), "b.txt"));
        _docs = _explorer.Reader.Add(PathOf("home", "docs"), File(PathOf("home", "docs"), "x.md"));
    }

    private PaneViewModel Pane => _explorer.Pane;

    private ContextMenus Menus => _explorer.Menus;

    [Fact]
    public async Task ItemMenu_OnAFile_OffersTheFileCommands_WithTheirKeys()
    {
        await Pane.StartAsync();
        Pane.MoveCursorTo(1);

        IReadOnlyList<MenuEntry> menu = await Menus.ForItemsAsync(Pane);

        Assert.Equal(["Open", "-", "Cut", "Copy", "Copy path", "-", "Rename", "Move to trash", "Delete permanently"], Titles(menu));
        Assert.Equal(["l", null, "x", "yy", "yp", null, "r", "d", "D"], menu.Select(entry => entry.Gesture));
        Assert.All(menu, entry => Assert.True(entry.IsEnabled));
    }

    [Fact]
    public async Task ItemMenu_OnAFolder_OpensItInASplit_AndPastesIntoIt()
    {
        await Pane.StartAsync();

        IReadOnlyList<MenuEntry> menu = await Menus.ForItemsAsync(Pane);

        Assert.Equal(["Open", "Open in a split to the right", "Open in a split below", "-", "Cut", "Copy", "Paste into folder", "Copy path", "-", "Rename", "Move to trash", "Delete permanently"], Titles(menu));
        MenuEntry right = Find(menu, "Open in a split to the right");
        Assert.Equal(new(CommandIds.SplitVertical, Argument: _docs), right.Invocation);
        Assert.Null(right.Gesture);
        Assert.Equal(new(CommandIds.SplitHorizontal, Argument: _docs), Find(menu, "Open in a split below").Invocation);
        Assert.False(Find(menu, "Paste into folder").IsEnabled);
    }

    [Fact]
    public async Task PasteIntoFolder_PastesIntoThatFolder_NotThePane()
    {
        await Pane.StartAsync();
        await _explorer.Clipboard.SetAsync([PathOf("home", "b.txt")], TransferMode.Copy);

        MenuEntry paste = Find(await Menus.ForItemsAsync(Pane), "Paste into folder");
        await Menus.RunAsync(paste);

        Assert.True(paste.IsEnabled);
        Assert.Null(paste.Gesture);
        TransferRequest request = Assert.Single(_explorer.Engine.Requests);
        Assert.Equal((_docs, TransferMode.Copy), (request.TargetDirectory, request.Mode));
        Assert.Equal([PathOf("home", "b.txt")], request.Sources);
    }

    [Fact]
    public async Task OpenInASplitToTheRight_OpensTheFolderInANewPaneSideBySide()
    {
        await Pane.StartAsync();

        await Menus.RunAsync(Find(await Menus.ForItemsAsync(Pane), "Open in a split to the right"));

        Assert.Equal(2, _explorer.Workspace.Panes.Count);
        Assert.Equal(_docs, _explorer.Workspace.ActivePane.CurrentPath);
        Assert.Equal(_home, _explorer.Workspace.Panes[0].CurrentPath);
        Assert.Equal(SplitAxis.Columns, Assert.IsType<SplitNode>(_explorer.Workspace.Layout.Root).Axis);
    }

    [Fact]
    public async Task ItemMenu_InSearchResults_OffersReveal()
    {
        _explorer.Search.Hits.Add(new SearchHit(File(_docs, "x.md"), "docs"));
        await Pane.StartAsync();
        await _explorer.RunAsync(CommandIds.FindName, "x");

        IReadOnlyList<MenuEntry> menu = await Menus.ForItemsAsync(Pane);
        await Menus.RunAsync(Find(menu, "Reveal in folder"));

        Assert.Equal("gf", Find(menu, "Reveal in folder").Gesture);
        Assert.False(Pane.IsSearchResults);
        Assert.Equal(_docs, Pane.CurrentPath);
    }

    [Fact]
    public async Task ItemMenu_WithSeveralTargets_CopiesTheirPaths()
    {
        await Pane.StartAsync();
        Pane.MarkAll();

        IReadOnlyList<MenuEntry> menu = await Menus.ForItemsAsync(Pane);
        await Menus.RunAsync(Find(menu, "Copy paths"));

        Assert.Equal(3, _explorer.SystemClipboard.Text?.Split('\n').Length);
    }

    [Fact]
    public async Task ItemMenu_OnNothing_IsEmpty()
    {
        _explorer.Reader.Add(PathOf("home", "empty"));
        await Pane.StartAsync(PathOf("home", "empty"));

        Assert.Empty(await Menus.ForItemsAsync(Pane));
    }

    [Fact]
    public async Task BackgroundMenu_ShowsHowThePaneLooks()
    {
        await Pane.StartAsync();
        Pane.ViewMode = ViewMode.MediumIcons;
        Pane.Sort = EntrySort.For(SortKey.Size).Reversed();
        await _explorer.RunAsync(CommandIds.ToggleHidden);

        IReadOnlyList<MenuEntry> menu = await Menus.ForBackgroundAsync(Pane);

        Assert.Equal(["New folder", "New file", "Paste", "-", "View", "Sort by", "Show hidden files", "Refresh", "-", "Split", "Close pane"], Titles(menu));
        Assert.Equal(["A", "a", "p", null, null, null, "zh", "R", null, null, "Space c"], menu.Select(entry => entry.Gesture));

        IReadOnlyList<MenuEntry> view = Find(menu, "View").Children;
        Assert.Equal(["Details", "Medium icons", "Large icons"], Titles(view));
        Assert.All(view, entry => Assert.Equal(MenuToggle.Radio, entry.Toggle));
        Assert.Equal([false, true, false], view.Select(entry => entry.IsChecked));

        IReadOnlyList<MenuEntry> sort = Find(menu, "Sort by").Children;
        Assert.Equal(["Name", "Date modified", "Size", "Type", "-", "Reverse"], Titles(sort));
        Assert.Equal([false, false, true, false, false, true], sort.Select(entry => entry.IsChecked));
        Assert.Equal(MenuToggle.Check, Find(sort, "Reverse").Toggle);

        MenuEntry hidden = Find(menu, "Show hidden files");
        Assert.Equal((MenuToggle.Check, true), (hidden.Toggle, hidden.IsChecked));

        Assert.Equal(["Side by side", "Top and bottom"], Titles(Find(menu, "Split").Children));
        Assert.Equal(["Space v", "Space s"], Find(menu, "Split").Children.Select(entry => entry.Gesture));

        Assert.False(Find(menu, "Paste").IsEnabled);
        Assert.False(Find(menu, "Close pane").IsEnabled);
    }

    [Fact]
    public async Task BackgroundMenu_PasteFollowsTheSystemClipboard_AndClosePaneTheSplits()
    {
        await Pane.StartAsync();
        _explorer.SystemClipboard.Files.Add(PathOf("elsewhere", "report.pdf"));
        await _explorer.RunAsync(CommandIds.SplitHorizontal);

        IReadOnlyList<MenuEntry> menu = await Menus.ForBackgroundAsync(Pane);

        Assert.True(Find(menu, "Paste").IsEnabled);
        Assert.True(Find(menu, "Close pane").IsEnabled);
    }

    [Fact]
    public async Task EveryEntry_RunsACommandWithAHandler_AndShowsItsKey()
    {
        _explorer.Search.Hits.Add(new SearchHit(Directory(_home, "docs"), string.Empty));
        await Pane.StartAsync();
        List<MenuEntry> entries = [.. Flatten(await Menus.ForItemsAsync(Pane)), .. Flatten(await Menus.ForBackgroundAsync(Pane))];
        await _explorer.RunAsync(CommandIds.FindName, "docs");
        entries.AddRange(Flatten(await Menus.ForItemsAsync(Pane)));

        Assert.All(entries.Where(entry => entry.Invocation is not null), entry =>
        {
            CommandInvocation invocation = entry.Invocation!;
            Assert.True(_explorer.Dispatcher.IsAvailable(invocation.CommandId), invocation.CommandId);

            if (invocation.Argument is null)
            {
                Assert.NotNull(entry.Gesture);
                Assert.Equal(CommandCatalog.Default.PrimaryGesture(invocation.CommandId), entry.Gesture);
            }
        }, throwIfEmpty: true);
    }

    [Fact]
    public async Task RunningAnEntry_DoesWhatItsKeyDoes()
    {
        await Pane.StartAsync();
        IReadOnlyList<MenuEntry> menu = await Menus.ForBackgroundAsync(Pane);

        await Menus.RunAsync(Find(menu, "Large icons"));
        await Menus.RunAsync(Find(menu, "Show hidden files"));
        await Menus.RunAsync(Find(menu, "Date modified"));

        Assert.Equal(ViewMode.LargeIcons, Pane.ViewMode);
        Assert.True(_explorer.Options.ShowHidden);
        Assert.Equal(EntrySort.For(SortKey.Modified), Pane.Sort);
    }

    [Fact]
    public async Task ADisabledEntry_DoesNothing()
    {
        await Pane.StartAsync();

        await Menus.RunAsync(Find(await Menus.ForBackgroundAsync(Pane), "Close pane"));

        Assert.Single(_explorer.Workspace.Panes);
        Assert.Null(_explorer.Status.Current);
    }

    [Theory]
    [InlineData("<Apps>")]
    [InlineData("<S-F10>")]
    public async Task TheMenuKeys_AskTheActivePaneForItsMenu(string keys)
    {
        await Pane.StartAsync();
        int requests = 0;
        Pane.ContextMenuRequested += (_, _) => requests++;

        await _explorer.PressAsync(keys);

        Assert.Equal(1, requests);
    }

    private static string[] Titles(IEnumerable<MenuEntry> entries) => [.. entries.Select(entry => entry.IsSeparator ? "-" : entry.Title)];

    private static IEnumerable<MenuEntry> Flatten(IEnumerable<MenuEntry> entries)
        => entries.SelectMany(entry => (IEnumerable<MenuEntry>)[entry, .. Flatten(entry.Children)]);

    private static MenuEntry Find(IEnumerable<MenuEntry> entries, string title) => Flatten(entries).Single(entry => entry.Title == title);
}

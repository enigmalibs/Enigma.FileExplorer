using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Search;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Services;

/// <summary>
/// The bookmarked folders, and <c>B</c>.
/// </summary>
public sealed class BookmarksTests
{
    private static readonly string Home = PathOf("home");
    private static readonly string Docs = PathOf("home", "docs");

    private readonly Bookmarks _bookmarks = new();
    private int _changes;

    public BookmarksTests() => _bookmarks.Changed += (_, _) => _changes++;

    [Fact]
    public void Add_AppendsAFolderOnce_UnderItsName()
    {
        Bookmark? docs = _bookmarks.Add(Docs);
        Bookmark? home = _bookmarks.Add(Home + System.IO.Path.DirectorySeparatorChar);

        Assert.Equal(new Bookmark("docs", Docs), docs);
        Assert.Equal(new Bookmark("home", Home), home);
        Assert.Null(_bookmarks.Add(Docs));
        Assert.Equal([new Bookmark("docs", Docs), new Bookmark("home", Home)], _bookmarks.Items);
        Assert.Equal(2, _changes);
    }

    [Fact]
    public void AVolumeRoot_IsListedUnderItsPath()
    {
        string root = PathUtilities.GetVolumeRoot(Root);

        Assert.Equal(root, Bookmarks.NameOf(root));
    }

    [Fact]
    public void Remove_TakesItOut_AndSaysWhenItWasNotThere()
    {
        Bookmark docs = _bookmarks.Add(Docs)!;

        Assert.True(_bookmarks.Remove(docs));
        Assert.False(_bookmarks.Remove(docs));
        Assert.Empty(_bookmarks.Items);
        Assert.Equal(2, _changes);
    }

    [Fact]
    public void Load_ReplacesTheList_SkipsBlanksAndRepeats_AndNamesTheNameless_WithoutAChange()
    {
        _bookmarks.Add(Home);
        _changes = 0;

        _bookmarks.Load([new Bookmark("Work", Docs), new Bookmark(string.Empty, Home), new Bookmark("again", Docs), new Bookmark("blank", " ")]);

        Assert.Equal([new Bookmark("Work", Docs), new Bookmark("home", Home)], _bookmarks.Items);
        Assert.Equal(0, _changes);
    }

    [Fact]
    public async Task ShiftB_BookmarksTheCurrentFolder_Once()
    {
        ExplorerHarness explorer = Explorer();
        await explorer.Pane.StartAsync(Docs);

        await explorer.PressAsync("B");
        Assert.Equal([new Bookmark("docs", Docs)], explorer.Bookmarks.Items);
        Assert.Equal("Bookmarked 'docs'", explorer.Status.Current?.Text);

        await explorer.PressAsync("B");
        Assert.Single(explorer.Bookmarks.Items);
        Assert.Equal("'docs' is already bookmarked", explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task ShiftB_InSearchResults_BookmarksNothing()
    {
        ExplorerHarness explorer = Explorer();
        explorer.Search.Hits.Add(new SearchHit(File(Docs, "x.md"), "docs"));
        await explorer.Pane.StartAsync();
        await explorer.RunAsync(CommandIds.FindName, "x");

        await explorer.PressAsync("B");

        Assert.Empty(explorer.Bookmarks.Items);
        Assert.Equal("Only a folder can be bookmarked", explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task B_ShowsTheBookmarks_AndThePickedOneOpensInTheActivePane()
    {
        ExplorerHarness explorer = Explorer();
        await explorer.Pane.StartAsync();
        explorer.BookmarksDialog.Answer = new Bookmark("docs", Docs);

        await explorer.PressAsync("b");

        Assert.Equal(1, explorer.BookmarksDialog.Shown);
        Assert.Equal(Docs, explorer.Pane.CurrentPath);
    }

    [Fact]
    public async Task ClosingTheBookmarks_WithoutAPick_LeavesThePaneWhereItIs()
    {
        ExplorerHarness explorer = Explorer();
        await explorer.Pane.StartAsync();

        explorer.Ex("bookmarks");
        await Task.Yield();

        Assert.Equal(1, explorer.BookmarksDialog.Shown);
        Assert.Equal(Home, explorer.Pane.CurrentPath);
    }

    [Fact]
    public async Task ABookmarkWhoseFolderIsGone_SaysSo_AndThePaneStays()
    {
        ExplorerHarness explorer = Explorer();
        await explorer.Pane.StartAsync();
        string gone = PathOf("home", "gone");
        explorer.BookmarksDialog.Answer = new Bookmark("gone", gone);

        await explorer.PressAsync("b");

        Assert.Equal(Home, explorer.Pane.CurrentPath);
        Assert.Equal($"'{gone}' does not exist", explorer.Status.Current?.Text);
    }

    private static ExplorerHarness Explorer()
    {
        ExplorerHarness explorer = new();
        explorer.Reader.Add(Home, Directory(Home, "docs"));
        explorer.Reader.Add(Docs, File(Docs, "x.md"));
        return explorer;
    }
}

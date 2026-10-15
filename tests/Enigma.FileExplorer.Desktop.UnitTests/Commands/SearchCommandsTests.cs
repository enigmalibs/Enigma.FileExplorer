using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Search;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Commands;

/// <summary>
/// Searching from a pane and working with the results, typed.
/// </summary>
public sealed class SearchCommandsTests
{
    private readonly ExplorerHarness _explorer = new();
    private readonly string _home;
    private readonly string _docs;

    public SearchCommandsTests()
    {
        _home = _explorer.Reader.Add(PathOf("home"), Directory(PathOf("home"), "docs"), File(PathOf("home"), "zz.txt"), File(PathOf("home"), "b.txt"));
        _docs = _explorer.Reader.Add(PathOf("home", "docs"), File(PathOf("home", "docs"), "a.md"), Directory(PathOf("home", "docs"), "notes"));
        _explorer.Reader.Add(PathOf("home", "docs", "notes"));

        _explorer.Search.Hits.Add(new SearchHit(File(_docs, "a.md"), "docs"));
        _explorer.Search.Hits.Add(new SearchHit(Directory(_docs, "notes"), "docs"));
        _explorer.Search.Hits.Add(new SearchHit(File(_home, "b.txt"), string.Empty, 3, "a needle here"));
        _explorer.Search.Hits.Add(new SearchHit(File(_home, "b.txt"), string.Empty, 9, "another needle"));
    }

    private PaneViewModel Pane => _explorer.Pane;

    private string[] Names => [.. Pane.Items.Select(item => item.Name)];

    [Fact]
    public async Task F_AsksForANamePattern_AndShowsTheResults()
    {
        await Pane.StartAsync();
        _explorer.Prompt.Answers.Enqueue("  a  ");

        await _explorer.PressAsync("f");

        SearchQuery query = Assert.Single(_explorer.Search.Queries);
        Assert.Equal((_home, "a", SearchKind.Name, false), (query.Root, query.Pattern, query.Kind, query.IncludeHidden));
        Assert.Equal("Find name: ", _explorer.Prompt.Asked[0].Label);
        Assert.True(Pane.IsSearchResults);
        Assert.False(Pane.IsSearching);
        Assert.Equal(["a.md", "notes", "b.txt", "b.txt"], Names);
        Assert.Equal("a.md", Pane.CursorItem?.Name);
        Assert.Equal("4 names matching \"a\" in home", Pane.SearchTitle);
        Assert.Equal("docs", Pane.Items[0].Location);
    }

    [Fact]
    public async Task Grep_SearchesContents_WithLinesAndText()
    {
        await Pane.StartAsync();

        await _explorer.RunAsync(CommandIds.FindContent, "needle");

        Assert.Equal(SearchKind.Content, _explorer.Search.Queries[0].Kind);
        Assert.Equal("3", Pane.Items[2].LineNumberText);
        Assert.Equal("a needle here", Pane.Items[2].LineText);
        Assert.StartsWith("4 lines containing", Pane.SearchTitle, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACancelledPrompt_SearchesNothing()
    {
        await Pane.StartAsync();

        await _explorer.PressAsync("<C-f>");

        Assert.Empty(_explorer.Search.Queries);
        Assert.False(Pane.IsSearchResults);
    }

    [Fact]
    public async Task Escape_LeavesTheResults_AndLBringsThemBackWithoutSearchingAgain()
    {
        await Pane.StartAsync();
        Pane.MoveCursorTo(2);
        await _explorer.RunAsync(CommandIds.FindName, "a");
        Pane.MoveCursorTo(2);

        await _explorer.PressAsync("<Esc>");

        Assert.False(Pane.IsSearchResults);
        Assert.Equal(_home, Pane.CurrentPath);
        Assert.Equal("zz.txt", Pane.CursorItem?.Name);

        await _explorer.PressAsync("L");

        Assert.True(Pane.IsSearchResults);
        Assert.Equal(["a.md", "notes", "b.txt", "b.txt"], Names);
        Assert.Equal(2, Pane.CursorIndex);
        Assert.Single(_explorer.Search.Queries);
    }

    [Fact]
    public async Task OpeningAFolderHit_EntersIt_AndHReturnsToTheResults()
    {
        await Pane.StartAsync();
        await _explorer.RunAsync(CommandIds.FindName, "a");
        Pane.MoveCursorTo(1);

        await _explorer.PressAsync("l");

        Assert.False(Pane.IsSearchResults);
        Assert.Equal(PathOf("home", "docs", "notes"), Pane.CurrentPath);

        await _explorer.PressAsync("H");

        Assert.True(Pane.IsSearchResults);
        Assert.Equal("notes", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task OpeningAFileHit_OpensIt_AndStaysInTheResults()
    {
        await Pane.StartAsync();
        await _explorer.RunAsync(CommandIds.FindName, "a");

        await _explorer.PressAsync("<CR>");

        Assert.Equal([PathOf("home", "docs", "a.md")], _explorer.Launcher.Opened);
        Assert.True(Pane.IsSearchResults);
    }

    [Fact]
    public async Task Gf_RevealsTheHitInItsFolder()
    {
        await Pane.StartAsync();
        await _explorer.RunAsync(CommandIds.FindName, "a");

        await _explorer.PressAsync("gf");

        Assert.False(Pane.IsSearchResults);
        Assert.Equal(_docs, Pane.CurrentPath);
        Assert.Equal("a.md", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task Gf_OutsideResults_Explains()
    {
        await Pane.StartAsync();

        await _explorer.PressAsync("gf");

        Assert.Equal("gf shows a search result in its folder", _explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task Escape_StopsARunningSearchFirst_KeepingWhatWasFound()
    {
        await Pane.StartAsync();
        _explorer.Search.Gate = new TaskCompletionSource();

        Task search = _explorer.RunAsync(CommandIds.FindName, "a");
        await WaitUntilAsync(() => Pane.Items.Count == 0 && Pane.IsSearching);
        Assert.True(Pane.IsSearching);
        Assert.StartsWith("Searching names matching", Pane.SearchTitle, StringComparison.Ordinal);

        await _explorer.PressAsync("<Esc>");
        await search;

        Assert.True(Pane.IsSearchResults);
        Assert.False(Pane.IsSearching);
        Assert.Equal(["a.md"], Names);
        Assert.EndsWith("(stopped)", Pane.SearchTitle, StringComparison.Ordinal);

        await _explorer.PressAsync("<Esc>");
        Assert.False(Pane.IsSearchResults);
    }

    [Fact]
    public async Task NavigatingAway_StopsTheSearch()
    {
        await Pane.StartAsync();
        _explorer.Search.Gate = new TaskCompletionSource();
        Task search = _explorer.RunAsync(CommandIds.FindName, "a");

        await Pane.NavigateAsync(_docs);
        await search;

        Assert.False(Pane.IsSearchResults);
        Assert.False(Pane.IsSearching);
        Assert.Equal(_docs, Pane.CurrentPath);
    }

    [Fact]
    public async Task R_SearchesAgain()
    {
        await Pane.StartAsync();
        await _explorer.RunAsync(CommandIds.FindName, "a");

        await _explorer.PressAsync("R");

        Assert.Equal(2, _explorer.Search.Queries.Count);
        Assert.True(Pane.IsSearchResults);
    }

    [Fact]
    public async Task Results_KeepTheirOrder_AndIgnoreTheDiskWatcher()
    {
        await Pane.StartAsync();
        await _explorer.RunAsync(CommandIds.FindName, "a");

        _explorer.Press("os");
        _explorer.Watchers.Change(_home);

        Assert.Equal(["a.md", "notes", "b.txt", "b.txt"], Names);
        Assert.True(Pane.IsSearchResults);
    }

    [Fact]
    public async Task MarkingTwoLinesOfOneFile_TargetsTheFileOnce()
    {
        await Pane.StartAsync();
        await _explorer.RunAsync(CommandIds.FindContent, "needle");

        Pane.MarkRange(2, 3);

        Assert.Equal([PathOf("home", "b.txt")], Pane.GetTargets().Select(item => item.FullPath));
    }

    [Fact]
    public async Task NoHits_ShowsNoResults()
    {
        _explorer.Search.Hits.Clear();
        await Pane.StartAsync();

        await _explorer.RunAsync(CommandIds.FindName, "zzz");

        Assert.Empty(Pane.Items);
        Assert.Equal("No results", Pane.PlaceholderText);
        Assert.Equal("0 names matching \"zzz\" in home", Pane.SearchTitle);
    }

    [Fact]
    public async Task ResultsUseAListLayout_EvenFromAnIconView()
    {
        await Pane.StartAsync();
        Pane.ViewMode = Core.Layout.ViewMode.LargeIcons;
        Pane.ItemsPerRow = 4;

        await _explorer.RunAsync(CommandIds.FindName, "a");

        Assert.True(Pane.IsListLayout);
        Assert.False(Pane.ShowsIconView);
        Assert.Equal(1, Pane.RowStep);

        await _explorer.PressAsync("<Esc>");
        Assert.True(Pane.ShowsIconView);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int i = 0; (i < 200) && !condition(); i++)
        {
            await Task.Delay(5, TestContext.Current.CancellationToken);
        }
    }
}

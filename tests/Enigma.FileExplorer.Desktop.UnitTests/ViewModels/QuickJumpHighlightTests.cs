using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Search;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

/// <summary>
/// vim's <c>incsearch</c> and <c>hlsearch</c> for the quick-jump: every match lit while typing and after
/// Enter, until Esc, <c>:noh</c> or another folder.
/// </summary>
public sealed class QuickJumpHighlightTests
{
    private readonly ExplorerHarness _explorer = new();

    public QuickJumpHighlightTests()
    {
        _explorer.Reader.Add(
            PathOf("home"),
            Directory(PathOf("home"), "Documents"),
            File(PathOf("home"), "alpha.txt"),
            File(PathOf("home"), "beta.md"),
            File(PathOf("home"), "Alpine.cfg"),
            File(PathOf("home"), "gamma.txt"));
        _explorer.Reader.Add(PathOf("home", "Documents"), File(PathOf("home", "Documents"), "alpaca.pdf"));
    }

    private PaneViewModel Pane => _explorer.Pane;

    private CommandLineViewModel CommandLine => _explorer.CommandLine;

    private string[] Lit => [.. Pane.Items.Where(item => item.IsMatch).Select(item => item.Name)];

    [Fact]
    public async Task Typing_LightsEveryMatch_SmartCase()
    {
        await Pane.StartAsync();

        _explorer.Press("/");
        CommandLine.Text = "al";
        Assert.Equal(["alpha.txt", "Alpine.cfg"], Lit);

        CommandLine.Text = "Al";
        Assert.Equal(["Alpine.cfg"], Lit);

        CommandLine.Text = "a";
        Assert.Equal(["alpha.txt", "Alpine.cfg", "beta.md", "gamma.txt"], Lit);
    }

    [Fact]
    public async Task Enter_KeepsTheHighlight_AndEscapeOnAJumpRestoresTheOneBefore()
    {
        await Pane.StartAsync();
        Jump("al");

        Assert.False(CommandLine.IsOpen);
        Assert.Equal(["alpha.txt", "Alpine.cfg"], Lit);

        _explorer.Press("/");
        CommandLine.Text = "gam";
        Assert.Equal(["gamma.txt"], Lit);

        CommandLine.Cancel();
        Assert.Equal(["alpha.txt", "Alpine.cfg"], Lit);
    }

    [Fact]
    public async Task ClearingThePattern_WhileTyping_ShowsTheHighlightThatWasThere()
    {
        await Pane.StartAsync();
        Jump("beta");

        _explorer.Press("/");
        CommandLine.Text = "g";
        CommandLine.Text = string.Empty;

        Assert.Equal(["beta.md"], Lit);
    }

    [Fact]
    public async Task EscapeInNormalMode_ClearsTheHighlight_BeforeTheMarks()
    {
        await Pane.StartAsync();
        Pane.MarkAll();
        Jump("al");

        await _explorer.PressAsync("<Esc>");
        Assert.Empty(Lit);
        Assert.Null(Pane.HighlightPattern);
        Assert.Equal(5, Pane.MarkedCount);

        await _explorer.PressAsync("<Esc>");
        Assert.Equal(0, Pane.MarkedCount);
    }

    [Fact]
    public async Task Noh_ClearsTheHighlight_AndNBringsItBack()
    {
        await Pane.StartAsync();
        Jump("al");

        _explorer.Ex("noh");
        Assert.Empty(Lit);

        await _explorer.PressAsync("n");
        Assert.Equal(["alpha.txt", "Alpine.cfg"], Lit);

        _explorer.Ex("nohlsearch");
        await _explorer.PressAsync("N");
        Assert.Equal(["alpha.txt", "Alpine.cfg"], Lit);
    }

    [Fact]
    public async Task ARefresh_KeepsTheHighlight_OnTheNewEntries()
    {
        await Pane.StartAsync();
        Jump("al");

        _explorer.Reader.Add(PathOf("home"), File(PathOf("home"), "alpha.txt"), File(PathOf("home"), "salt.txt"), File(PathOf("home"), "zeta.txt"));
        await Pane.RefreshAsync();

        Assert.Equal(["alpha.txt", "salt.txt"], Lit);
    }

    [Fact]
    public async Task AnotherFolder_StartsWithoutAHighlight()
    {
        await Pane.StartAsync();
        Jump("al");

        await Pane.NavigateAsync(PathOf("home", "Documents"));

        Assert.Null(Pane.HighlightPattern);
        Assert.Empty(Lit);
    }

    [Fact]
    public async Task SearchResults_AreHighlightedToo()
    {
        _explorer.Search.Hits.Add(new SearchHit(File(PathOf("home", "Documents"), "alpaca.pdf"), "Documents"));
        _explorer.Search.Hits.Add(new SearchHit(File(PathOf("home"), "beta.md"), string.Empty));
        await Pane.StartAsync();
        Jump("al");

        await _explorer.RunAsync(CommandIds.FindName, "a");
        Assert.True(Pane.IsSearchResults);
        Assert.Empty(Lit);

        Jump("alp");
        Assert.Equal(["alpaca.pdf"], Lit);
    }

    [Fact]
    public async Task TheHighlight_BelongsToThePaneItWasMadeIn()
    {
        await Pane.StartAsync();
        PaneViewModel first = Pane;
        Jump("al");

        await _explorer.Workspace.SplitAsync(Core.Layout.SplitAxis.Columns);

        Assert.Empty(Lit);
        Assert.Equal(["alpha.txt", "Alpine.cfg"], first.Items.Where(item => item.IsMatch).Select(item => item.Name));
    }

    private void Jump(string pattern)
    {
        _explorer.Press("/");
        CommandLine.Text = pattern;
        CommandLine.Accept();
    }
}

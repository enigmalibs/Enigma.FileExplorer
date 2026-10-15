using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

public sealed class CommandLineViewModelTests
{
    private readonly ExplorerHarness _explorer = new();
    private readonly string _home;

    public CommandLineViewModelTests()
    {
        _home = _explorer.Reader.Add(
            PathOf("home"),
            Directory(PathOf("home"), "Documents"),
            File(PathOf("home"), "alpha.txt"),
            File(PathOf("home"), "beta.md"),
            File(PathOf("home"), "Alpine.cfg"),
            File(PathOf("home"), "gamma.txt"));
        _explorer.Reader.Add(PathOf("home", "Documents"), File(PathOf("home", "Documents"), "cv.pdf"));
        _explorer.Reader.Add(Root, Directory(Root, "home"));
    }

    private CommandLineViewModel CommandLine => _explorer.CommandLine;

    [Fact]
    public async Task Colon_OpensTheCommandLine_AndCdRuns()
    {
        await _explorer.Pane.StartAsync();

        _explorer.Press(":");
        Assert.Equal(CommandLineMode.Command, CommandLine.Mode);
        Assert.Equal(":", CommandLine.Prefix);

        CommandLine.Text = "cd Documents";
        CommandLine.Accept();
        await Task.Yield();

        Assert.False(CommandLine.IsOpen);
        Assert.Equal(PathOf("home", "Documents"), _explorer.Pane.CurrentPath);
        Assert.Equal(["cd Documents"], CommandLine.History);
    }

    [Fact]
    public async Task Cd_ResolvesDotDotTildeAndNothing()
    {
        await _explorer.Pane.NavigateAsync(PathOf("home", "Documents"));

        _explorer.Ex("cd ..");
        Assert.Equal(_home, _explorer.Pane.CurrentPath);

        _explorer.Ex("cd ..");
        _explorer.Ex("cd ~/Documents");
        Assert.Equal(PathOf("home", "Documents"), _explorer.Pane.CurrentPath);

        _explorer.Ex("cd");
        Assert.Equal(_home, _explorer.Pane.CurrentPath);
    }

    [Fact]
    public void AParseError_IsReported()
    {
        _explorer.Ex("frobnicate");

        Assert.Equal("Not a command: frobnicate", _explorer.Status.Current?.Text);
        Assert.True(_explorer.Status.Current?.IsError);
    }

    [Fact]
    public void AnEmptyLine_DoesNothing()
    {
        _explorer.Ex("   ");

        Assert.Null(_explorer.Status.Current);
        Assert.Empty(CommandLine.History);
    }

    [Fact]
    public void Escape_ClosesWithoutRunning()
    {
        List<string> closed = [];
        CommandLine.Closed += (_, _) => closed.Add("closed");
        CommandLine.OpenCommand();
        CommandLine.Text = "frobnicate";

        CommandLine.Cancel();

        Assert.False(CommandLine.IsOpen);
        Assert.Equal(string.Empty, CommandLine.Text);
        Assert.Null(_explorer.Status.Current);
        Assert.Single(closed);
    }

    [Fact]
    public void UpAndDown_BrowseTheHistory()
    {
        _explorer.Ex("help");
        _explorer.Ex("refresh");
        CommandLine.OpenCommand();

        CommandLine.HistoryOlder();
        Assert.Equal("refresh", CommandLine.Text);

        CommandLine.HistoryOlder();
        Assert.Equal("help", CommandLine.Text);

        CommandLine.HistoryNewer();
        CommandLine.HistoryNewer();
        Assert.Equal(string.Empty, CommandLine.Text);
    }

    [Fact]
    public void Tab_CyclesCompletions_AndShiftTabGoesBack()
    {
        CommandLine.OpenCommand("s");

        CommandLine.Complete();
        Assert.Equal("set", CommandLine.Text);

        CommandLine.Complete();
        Assert.Equal("sort", CommandLine.Text);

        CommandLine.Complete(backwards: true);
        Assert.Equal("set", CommandLine.Text);

        // Typing starts a new completion from what is now typed.
        CommandLine.Text = "sp";
        CommandLine.Complete();
        Assert.Equal("split", CommandLine.Text);
    }

    [Fact]
    public async Task Tab_CompletesDirectoriesOfTheActivePane()
    {
        using TempDirectory temp = new();
        temp.CreateDirectory("projects");
        temp.CreateDirectory("photos");
        temp.CreateFile("plan.txt");
        _explorer.Reader.Add(temp.Path);
        await _explorer.Pane.NavigateAsync(temp.Path);
        CommandLine.OpenCommand("cd p");

        CommandLine.Complete();
        Assert.Equal($"cd photos{Path.DirectorySeparatorChar}", CommandLine.Text);

        CommandLine.Complete();
        Assert.Equal($"cd projects{Path.DirectorySeparatorChar}", CommandLine.Text);
    }

    [Fact]
    public async Task Slash_JumpsIncrementally_SmartCase()
    {
        await _explorer.Pane.StartAsync();

        _explorer.Press("/");
        Assert.Equal(CommandLineMode.Jump, CommandLine.Mode);

        CommandLine.Text = "al";
        Assert.Equal("alpha.txt", _explorer.Pane.CursorItem?.Name);

        CommandLine.Text = "Al";
        Assert.Equal("Alpine.cfg", _explorer.Pane.CursorItem?.Name);

        CommandLine.Text = "zzz";
        Assert.True(CommandLine.HasNoMatch);
        Assert.Equal("Documents", _explorer.Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task Slash_EscapeRestoresTheCursor()
    {
        await _explorer.Pane.StartAsync();
        _explorer.Press("j");
        _explorer.Press("/");
        CommandLine.Text = "gam";

        CommandLine.Cancel();

        Assert.Equal(1, _explorer.Pane.CursorIndex);
        Assert.Null(CommandLine.LastJumpPattern);
    }

    [Fact]
    public async Task Slash_EnterRemembers_AndNAndShiftNCycle()
    {
        await _explorer.Pane.StartAsync();
        _explorer.Press("/");
        CommandLine.Text = "a";
        CommandLine.Accept();
        string first = _explorer.Pane.CursorItem!.Name;

        _explorer.Press("n");
        string second = _explorer.Pane.CursorItem!.Name;
        _explorer.Press("N");

        Assert.Equal("a", CommandLine.LastJumpPattern);
        Assert.NotEqual(first, second);
        Assert.Equal(first, _explorer.Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task N_WithoutAPattern_SaysSo()
    {
        await _explorer.Pane.StartAsync();

        _explorer.Press("n");

        Assert.Equal("No previous pattern", _explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task Slash_AMissConfirmed_IsReported()
    {
        await _explorer.Pane.StartAsync();
        _explorer.Press("/");
        CommandLine.Text = "zzz";

        CommandLine.Accept();

        Assert.Equal("Pattern not found: zzz", _explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task PromptAsync_ReturnsTheAnswer()
    {
        List<TextSelection> selections = [];
        CommandLine.TextReplaced += (_, selection) => selections.Add(selection);

        Task<string?> answer = CommandLine.PromptAsync("Rename: ", "report.pdf", 0, 6);

        Assert.Equal(CommandLineMode.Prompt, CommandLine.Mode);
        Assert.Equal("Rename: ", CommandLine.Prefix);
        Assert.Equal("report.pdf", CommandLine.Text);
        Assert.Equal(new TextSelection(0, 6), selections[0]);

        CommandLine.Text = "summary.pdf";
        CommandLine.Accept();

        Assert.Equal("summary.pdf", await answer);
        Assert.False(CommandLine.IsOpen);
    }

    [Fact]
    public async Task PromptAsync_CancelledAnswersNull()
    {
        Task<string?> answer = CommandLine.PromptAsync("New folder: ");

        CommandLine.Cancel();

        Assert.Null(await answer);
        Assert.False(CommandLine.IsOpen);
    }

    [Fact]
    public async Task PromptAsync_ANewPromptCancelsTheOldOne()
    {
        Task<string?> first = CommandLine.PromptAsync("First: ");
        Task<string?> second = CommandLine.PromptAsync("Second: ");

        Assert.Null(await first);
        Assert.Equal("Second: ", CommandLine.Prefix);

        CommandLine.Accept();
        Assert.Equal(string.Empty, await second);
    }

    [Fact]
    public void QuestionMark_ShowsTheHelp()
    {
        _explorer.Press("?");
        _explorer.Press("<F1>");
        _explorer.Ex("h");

        Assert.Equal(3, _explorer.Help.Shown);
    }

    [Fact]
    public void ColonAbout_ShowsTheAboutDialog_AbbreviatedToo()
    {
        _explorer.Ex("about");
        _explorer.Ex("ab");

        Assert.Equal(2, _explorer.About.Shown);
        Assert.Equal(0, _explorer.Help.Shown);
    }

    [Fact]
    public void HelpContent_ListsAboutInTheGeneralCard()
    {
        HelpSection general = HelpViewModel.Build(Core.Commands.CommandCatalog.Default)
            .Single(section => section.Category == Core.Commands.CommandCatalog.GeneralCategory);

        Assert.Contains(general.Rows, row => (row.Title == "About Enigma File Explorer") && (row.ExCommand == ":ab[out]") && (row.Keys.Length == 0));
    }

    [Fact]
    public void HelpContent_ListsEveryBindingOfTheCatalog()
    {
        IReadOnlyList<HelpSection> sections = HelpViewModel.Build(Core.Commands.CommandCatalog.Default);
        string[] keys = [.. sections.SelectMany(section => section.Rows).Select(row => row.Keys)];

        foreach (Core.Keymaps.KeyBinding binding in Core.Commands.CommandCatalog.Default.Keymap.Bindings.Where(binding => binding.Argument is null))
        {
            Assert.Contains(keys, row => row.Split(", ").Contains(binding.Description));
        }
    }

    [Fact]
    public void HelpContent_FormatsExCommandsAndCollapsesPlaces()
    {
        HelpRow[] rows = [.. HelpViewModel.Build(Core.Commands.CommandCatalog.Default).SelectMany(section => section.Rows)];

        Assert.Contains(rows, row => (row.Keys == "Space v") && (row.ExCommand == ":vs[plit]"));
        Assert.Contains(rows, row => (row.Keys == "D, Shift+Del") && (row.ExCommand == ":del[ete]!"));
        Assert.Contains(rows, row => row.Keys == "g1 … g9");
        Assert.Contains(rows, row => (row.Keys.Length == 0) && (row.ExCommand == ":th[eme]"));
    }
}

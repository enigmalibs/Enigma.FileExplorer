using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Commands;

/// <summary>
/// Creating, renaming and deleting from the keyboard and the command line.
/// </summary>
public sealed class FileCommandsTests
{
    private readonly ExplorerHarness _explorer = new();
    private readonly string _home;

    public FileCommandsTests()
    {
        _home = _explorer.Reader.Add(
            PathOf("home"),
            Directory(PathOf("home"), "v1.2"),
            File(PathOf("home"), "a.txt"),
            File(PathOf("home"), "b.txt"),
            File(PathOf("home"), "c.txt"),
            File(PathOf("home"), "report.final.pdf"));

        // These tests are about what trashing does; the question before it has tests of its own.
        _explorer.Options.ConfirmTrash = false;
    }

    private PaneViewModel Pane => _explorer.Pane;

    private string[] Names => [.. Pane.Items.Select(item => item.Name)];

    private Task StartAsync() => Pane.StartAsync();

    [Fact]
    public async Task A_CreatesAFile_AndPutsTheCursorOnIt()
    {
        await StartAsync();
        _explorer.Prompt.Answers.Enqueue("notes.md");

        await _explorer.PressAsync("a");

        Assert.Contains("notes.md", Names);
        Assert.Equal("notes.md", Pane.CursorItem?.Name);
        Assert.False(Pane.CursorItem?.IsDirectory);
        Assert.Equal("Created file 'notes.md'", _explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task A_WithATrailingSlash_CreatesAFolder()
    {
        await StartAsync();
        _explorer.Prompt.Answers.Enqueue("src/");

        await _explorer.PressAsync("a");

        Assert.True(Pane.CursorItem?.IsDirectory);
        Assert.Equal("src", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task ShiftA_AnInvalidName_AsksAgainWithWhatWasTyped()
    {
        await StartAsync();
        _explorer.Prompt.Answers.Enqueue("bad/name");
        _explorer.Prompt.Answers.Enqueue("good");

        await _explorer.PressAsync("A");

        Assert.Equal(2, _explorer.Prompt.Asked.Count);
        Assert.Equal("bad/name", _explorer.Prompt.Asked[1].Initial);
        Assert.Equal("good", Pane.CursorItem?.Name);
        Assert.True(Pane.CursorItem?.IsDirectory);
    }

    [Fact]
    public async Task ShiftA_ATakenName_IsReported_AndCancellingStops()
    {
        await StartAsync();
        _explorer.Prompt.Answers.Enqueue("a.txt");

        await _explorer.PressAsync("A");

        Assert.Equal("'a.txt' already exists", _explorer.Status.Current?.Text);
        Assert.Equal(2, _explorer.Prompt.Asked.Count);
        Assert.Equal(5, Pane.Items.Count);
    }

    [Fact]
    public async Task MkdirAndTouch_TakeTheNameDirectly()
    {
        await StartAsync();

        await _explorer.RunAsync(CommandIds.NewDirectory, "made");
        await _explorer.RunAsync(CommandIds.NewFile, "made.txt");

        Assert.Empty(_explorer.Prompt.Asked);
        Assert.Contains("made", Names);
        Assert.Contains("made.txt", Names);
    }

    [Fact]
    public async Task R_PreselectsTheStem_AndRenames()
    {
        await StartAsync();
        Pane.MoveCursorTo(4);
        _explorer.Prompt.Answers.Enqueue("summary.final.pdf");

        await _explorer.PressAsync("r");

        (string label, string initial, int start, int? length) = Assert.Single(_explorer.Prompt.Asked);
        Assert.Equal("Rename: ", label);
        Assert.Equal("report.final.pdf", initial);
        Assert.Equal((0, "report.final".Length), (start, length ?? -1));
        Assert.Equal("summary.final.pdf", Pane.CursorItem?.Name);
        Assert.DoesNotContain("report.final.pdf", Names);
    }

    [Fact]
    public async Task Cw_OnAFolder_SelectsTheWholeName()
    {
        await StartAsync();

        await _explorer.PressAsync("cw");

        Assert.Equal("v1.2".Length, _explorer.Prompt.Asked[0].SelectionLength);
    }

    [Fact]
    public async Task Rename_ToATakenName_AsksAgain()
    {
        await StartAsync();
        Pane.MoveCursorTo(1);
        _explorer.Prompt.Answers.Enqueue("b.txt");
        _explorer.Prompt.Answers.Enqueue("z.txt");

        await _explorer.PressAsync("<F2>");

        Assert.Equal(2, _explorer.Prompt.Asked.Count);
        Assert.Equal("z.txt", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task RenameEx_TakesTheNameDirectly()
    {
        await StartAsync();
        Pane.MoveCursorTo(1);

        await _explorer.RunAsync(CommandIds.Rename, "aa.txt");

        Assert.Empty(_explorer.Prompt.Asked);
        Assert.Equal("aa.txt", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task D_TrashesTheCursorItem_AndTheCursorMovesOn()
    {
        await StartAsync();
        Pane.MoveCursorTo(2);

        await _explorer.PressAsync("d");

        Assert.Equal([PathOf("home", "b.txt")], _explorer.Trash.Trashed);
        Assert.Equal("c.txt", Pane.CursorItem?.Name);
        Assert.Equal("Moved 1 item to the trash", _explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task Delete_TrashesTheMarks_AndClearsThem()
    {
        await StartAsync();
        Pane.Click(Pane.Items[1], ClickModifiers.Toggle);
        Pane.Click(Pane.Items[3], ClickModifiers.Toggle);

        await _explorer.PressAsync("<Del>");

        Assert.Equal(2, _explorer.Trash.Trashed.Count);
        Assert.Equal(0, Pane.MarkedCount);
        Assert.Equal(["v1.2", "b.txt", "report.final.pdf"], Names);
        Assert.Equal("report.final.pdf", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task DeletingTheLastItem_PutsTheCursorOnThePreviousOne()
    {
        await StartAsync();
        Pane.MoveCursorTo(4);

        await _explorer.PressAsync("d");

        Assert.Equal("c.txt", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task WithoutATrash_DeletingPermanentlyNeedsAYes()
    {
        await StartAsync();
        Pane.MoveCursorTo(1);
        _explorer.Trash.NoTrash.Add(PathOf("home", "a.txt"));

        await _explorer.PressAsync("d");

        Assert.Single(_explorer.Confirmation.Asked);
        Assert.Contains("a.txt", Names);

        _explorer.Confirmation.Answers.Enqueue(true);
        await _explorer.PressAsync("d");

        Assert.Equal([PathOf("home", "a.txt")], _explorer.Operations.Deleted);
        Assert.DoesNotContain("a.txt", Names);
    }

    [Fact]
    public async Task D_AsksBeforeTrashing_AndEnterWouldConfirm()
    {
        _explorer.Options.ConfirmTrash = true;
        await StartAsync();
        Pane.MoveCursorTo(2);
        _explorer.Confirmation.Answers.Enqueue(true);

        await _explorer.PressAsync("d");

        Assert.Equal(["Move 'b.txt' to the trash?"], _explorer.Confirmation.Asked);
        Assert.Equal([true], _explorer.Confirmation.DefaultedToConfirm);
        Assert.Equal([PathOf("home", "b.txt")], _explorer.Trash.Trashed);
    }

    [Fact]
    public async Task D_Declined_LeavesEverythingAsItWas()
    {
        _explorer.Options.ConfirmTrash = true;
        await StartAsync();
        Pane.MarkAll();

        await _explorer.PressAsync("d");

        Assert.Equal(["Move 5 items to the trash?"], _explorer.Confirmation.Asked);
        Assert.Empty(_explorer.Trash.Trashed);
        Assert.Equal(5, Pane.MarkedCount);
        Assert.Equal(5, Pane.Items.Count);
    }

    [Fact]
    public async Task WithTheQuestionSwitchedOff_DTrashesAtOnce()
    {
        await StartAsync();
        Pane.MoveCursorTo(1);

        await _explorer.PressAsync("d");

        Assert.Empty(_explorer.Confirmation.Asked);
        Assert.Equal([PathOf("home", "a.txt")], _explorer.Trash.Trashed);
    }

    [Fact]
    public async Task ShiftD_AsksFirst_WithCancelAsTheDefault_AndOnlyAYesDeletes()
    {
        await StartAsync();
        Pane.MoveCursorTo(1);

        await _explorer.PressAsync("D");

        Assert.Equal(["Delete 'a.txt' permanently? This cannot be undone."], _explorer.Confirmation.Asked);
        Assert.Equal([false], _explorer.Confirmation.DefaultedToConfirm);
        Assert.Contains("a.txt", Names);

        _explorer.Confirmation.Answers.Enqueue(true);
        await _explorer.PressAsync("D");

        Assert.Equal([PathOf("home", "a.txt")], _explorer.Operations.Deleted);
        Assert.Empty(_explorer.Trash.Trashed);
    }

    [Fact]
    public async Task ShiftDelete_AsksFirst_AndOnlyAYesDeletes()
    {
        await StartAsync();
        Pane.MarkAll();

        await _explorer.PressAsync("<S-Del>");

        Assert.Contains("5 items", _explorer.Confirmation.Asked[0], System.StringComparison.Ordinal);
        Assert.Equal(5, Pane.Items.Count);

        _explorer.Confirmation.Answers.Enqueue(true);
        await _explorer.RunAsync(CommandIds.DeletePermanently);

        Assert.Empty(Pane.Items);
        Assert.Equal("Deleted 5 items permanently", _explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task AFailedDeletion_IsReportedByName()
    {
        await StartAsync();
        Pane.MarkAll();
        _explorer.Trash.FailOn.Add(PathOf("home", "a.txt"));

        await _explorer.PressAsync("d");

        Assert.Equal("Could not delete a.txt", _explorer.Status.Current?.Text);
        Assert.Equal(["a.txt"], Names);
    }

    [Fact]
    public async Task VisualMode_DeletesTheRangeAndLeavesVisualMode()
    {
        await StartAsync();
        _explorer.Press("jvj");

        await _explorer.PressAsync("d");

        Assert.False(Pane.IsVisualMode);
        Assert.Equal(["v1.2", "c.txt", "report.final.pdf"], Names);
    }
}

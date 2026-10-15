using System;
using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Commands;

/// <summary>
/// Copy, cut and paste between panes, typed.
/// </summary>
public sealed class ClipboardCommandsTests
{
    private readonly ExplorerHarness _explorer = new();
    private readonly string _home;
    private readonly string _docs;

    public ClipboardCommandsTests()
    {
        _home = _explorer.Reader.Add(PathOf("home"), Directory(PathOf("home"), "docs"), File(PathOf("home"), "a.txt"), File(PathOf("home"), "b.txt"));
        _docs = _explorer.Reader.Add(PathOf("home", "docs"), File(PathOf("home", "docs"), "x.md"));
    }

    private PaneViewModel Pane => _explorer.Pane;

    [Fact]
    public async Task YyThenP_CopiesIntoTheOtherPane()
    {
        await Pane.StartAsync();
        PaneViewModel source = Pane;
        await _explorer.Workspace.SplitAsync(SplitAxis.Columns, _docs);
        _explorer.Workspace.Activate(source);
        source.MoveCursorTo(1);

        await _explorer.PressAsync("yy");
        Assert.Equal("Copied 'a.txt'", _explorer.Status.Current?.Text);
        Assert.Equal([PathOf("home", "a.txt")], _explorer.SystemClipboard.Files);

        _explorer.Press("gl");
        await _explorer.PressAsync("p");

        TransferRequest request = Assert.Single(_explorer.Engine.Requests);
        Assert.Equal(TransferMode.Copy, request.Mode);
        Assert.Equal(_docs, request.TargetDirectory);
        Assert.Equal("a.txt", _explorer.Pane.CursorItem?.Name);
        Assert.Contains("a.txt", source.Items.Select(item => item.Name));
        Assert.Equal("Copying 1 item: done", _explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task XThenP_Moves_FadesTheCutEntries_AndEmptiesTheClipboard()
    {
        await Pane.StartAsync();
        Pane.MarkAll();
        Pane.Click(Pane.Items[0], ClickModifiers.Toggle);

        await _explorer.PressAsync("x");

        Assert.All(Pane.Items.Skip(1), item => Assert.True(item.IsCut));
        Assert.False(Pane.Items[0].IsCut);
        Assert.Equal(0, Pane.MarkedCount);

        await Pane.NavigateAsync(_docs);
        await _explorer.PressAsync("p");

        Assert.Equal(TransferMode.Move, _explorer.Engine.Requests[0].Mode);
        Assert.Empty(_explorer.Clipboard.Paths);
        Assert.Equal(["a.txt", "b.txt", "x.md"], Pane.Items.Select(item => item.Name));
        Assert.All(Pane.Items, item => Assert.False(item.IsCut));
    }

    [Fact]
    public async Task CtrlCAndCtrlV_AreTheSame()
    {
        await Pane.StartAsync();
        Pane.MoveCursorTo(2);

        await _explorer.PressAsync("<C-c>");
        await _explorer.PressAsync("<C-v>");

        Assert.Equal(_home, Assert.Single(_explorer.Engine.Requests).TargetDirectory);
    }

    [Fact]
    public async Task Paste_WithNothingCopied_SaysSo()
    {
        await Pane.StartAsync();

        await _explorer.PressAsync("p");

        Assert.Equal("Nothing to paste", _explorer.Status.Current?.Text);
        Assert.Empty(_explorer.Engine.Requests);
    }

    [Fact]
    public async Task Paste_FilesCopiedInAnotherApplication_AreCopied()
    {
        await Pane.NavigateAsync(_docs);
        _explorer.SystemClipboard.Files.Add(PathOf("home", "a.txt"));

        await _explorer.PressAsync("p");

        TransferRequest request = Assert.Single(_explorer.Engine.Requests);
        Assert.Equal(TransferMode.Copy, request.Mode);
        Assert.Equal([PathOf("home", "a.txt")], request.Sources);
    }

    [Fact]
    public async Task Paste_SystemFilesNewerThanOurCut_Win()
    {
        await Pane.StartAsync();
        Pane.MoveCursorTo(1);
        await _explorer.PressAsync("x");
        _explorer.SystemClipboard.Files.Clear();
        _explorer.SystemClipboard.Files.Add(PathOf("home", "b.txt"));

        await _explorer.PressAsync("p");

        TransferRequest request = Assert.Single(_explorer.Engine.Requests);
        Assert.Equal(TransferMode.Copy, request.Mode);
        Assert.Equal([PathOf("home", "b.txt")], request.Sources);
        Assert.All(Pane.Items, item => Assert.False(item.IsCut));
    }

    [Fact]
    public async Task Yp_CopiesThePathsAsText()
    {
        await Pane.StartAsync();
        Pane.MarkAll();

        await _explorer.PressAsync("yp");

        Assert.Equal(string.Join(Environment.NewLine, Pane.Items.Select(item => item.FullPath)), _explorer.SystemClipboard.Text);
        Assert.Equal("Copied 3 paths", _explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task ARefusedTransfer_IsReported()
    {
        await Pane.StartAsync();
        Pane.MoveCursorTo(0);
        await _explorer.PressAsync("yy");
        _explorer.Engine.Refuse = new FileOperationException("Cannot copy 'docs' into itself");
        await Pane.NavigateAsync(_docs);

        await _explorer.PressAsync("p");

        Assert.Equal("Cannot copy 'docs' into itself", _explorer.Status.Current?.Text);
        Assert.False(_explorer.Transfers.IsBusy);
    }

    [Fact]
    public async Task Errors_AreSummarized_AndReported()
    {
        await Pane.StartAsync();
        await _explorer.PressAsync("yy");
        _explorer.Engine.Errors.Add(new TransferError(PathOf("home", "docs"), "docs: access denied"));

        await _explorer.PressAsync("p");

        Assert.Equal("Copying 1 item: 1 failed", _explorer.Status.Current?.Text);
        Assert.Single(_explorer.ErrorReport.Shown);
    }

    [Fact]
    public async Task Cancel_StopsTheRunningTransfer()
    {
        await Pane.StartAsync();
        await _explorer.PressAsync("yy");
        _explorer.Engine.Gate = new TaskCompletionSource();

        _explorer.Press("p");
        Task paste = _explorer.Keyboard.LastCommand;
        Assert.True(_explorer.Transfers.IsBusy);
        Assert.Equal("Copying 1 item", _explorer.Transfers.Title);

        await _explorer.RunAsync(CommandIds.CancelTransfer);
        await paste;

        Assert.False(_explorer.Transfers.IsBusy);
        Assert.StartsWith("Copying 1 item: cancelled", _explorer.Status.Current?.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancelWithNothingRunning_SaysSo()
    {
        await _explorer.RunAsync(CommandIds.CancelTransfer);

        Assert.Equal("No transfer is running", _explorer.Status.Current?.Text);
    }

    [Fact]
    public async Task Transfers_RunOneAtATime()
    {
        await Pane.StartAsync();
        await _explorer.PressAsync("yy");
        TaskCompletionSource gate = new();
        _explorer.Engine.Gate = gate;

        Task first = _explorer.Transfers.RunAsync(new TransferRequest([PathOf("home", "a.txt")], _docs, TransferMode.Copy), "first");
        Task second = _explorer.Transfers.RunAsync(new TransferRequest([PathOf("home", "b.txt")], _docs, TransferMode.Copy), "second");

        Assert.Single(_explorer.Engine.Requests);
        gate.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(2, _explorer.Engine.Requests.Count);
    }

    [Fact]
    public void ConflictViewModel_DescribesBothSides()
    {
        ConflictViewModel question = new(new ConflictInfo(
            PathOf("src", "a.txt"),
            PathOf("dst", "a.txt"),
            2048,
            new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Local),
            1024,
            new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Local),
            TargetIsDirectory: false));

        Assert.Equal("'a.txt' already exists in 'dst'.", question.Question);
        Assert.StartsWith("1 KB, modified", question.ExistingText, StringComparison.Ordinal);
        Assert.StartsWith("2 KB, modified", question.IncomingText, StringComparison.Ordinal);
        Assert.False(question.ApplyToAll);
    }
}

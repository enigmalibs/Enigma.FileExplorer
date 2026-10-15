using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

/// <summary>
/// Where a drop may land, what the modifiers ask for, and the transfer a drop runs.
/// </summary>
public sealed class FileDropsTests
{
    private readonly ExplorerHarness _explorer = new();
    private readonly string _home;
    private readonly string _docs;

    public FileDropsTests()
    {
        _home = _explorer.Reader.Add(PathOf("home"), Directory(PathOf("home"), "docs"), Directory(PathOf("home"), "docs2"), File(PathOf("home"), "a.txt"));
        _docs = _explorer.Reader.Add(PathOf("home", "docs"), Directory(PathOf("home", "docs"), "deep"), File(PathOf("home", "docs"), "x.md"));
        _explorer.Reader.Add(PathOf("home", "docs", "deep"));
        _explorer.Reader.Add(PathOf("home", "docs2"));
    }

    [Theory]
    [InlineData(false, false, DropAction.Ask)]
    [InlineData(true, false, DropAction.Copy)]
    [InlineData(false, true, DropAction.Move)]
    [InlineData(true, true, DropAction.Ask)]
    public void ActionFor_FollowsKde(bool control, bool shift, DropAction expected)
        => Assert.Equal(expected, FileDrops.ActionFor(control, shift));

    [Fact]
    public void TargetFor_AFolder_IsThatFolder()
    {
        Assert.Equal(_docs, FileDrops.TargetFor([PathOf("home", "a.txt")], _docs));

        // A sibling whose name starts like the dragged folder is not inside it.
        Assert.Equal(PathOf("home", "docs2"), FileDrops.TargetFor([_docs], PathOf("home", "docs2")));
    }

    [Fact]
    public void TargetFor_RefusesAFolderIntoItselfOrItsSubfolders()
    {
        Assert.Null(FileDrops.TargetFor([PathOf("home", "a.txt"), _docs], _docs));
        Assert.Null(FileDrops.TargetFor([_docs], PathOf("home", "docs", "deep")));
    }

    [Fact]
    public void TargetFor_RefusesNoDirectory_AndNothingDragged()
    {
        Assert.Null(FileDrops.TargetFor([PathOf("home", "a.txt")], null));
        Assert.Null(FileDrops.TargetFor([], _docs));
    }

    [Theory]
    [InlineData(DropAction.Ask, DropAction.None)]
    [InlineData(DropAction.Move, DropAction.None)]
    [InlineData(DropAction.Copy, DropAction.Copy)]
    public void Decide_WhereTheEntriesAlreadyAre_OnlyCopies(DropAction requested, DropAction expected)
        => Assert.Equal(expected, FileDrops.Decide([PathOf("home", "a.txt"), _docs], _home, requested));

    [Theory]
    [InlineData(DropAction.Ask)]
    [InlineData(DropAction.Move)]
    [InlineData(DropAction.Copy)]
    public void Decide_Elsewhere_DoesWhatWasAsked(DropAction requested)
    {
        Assert.Equal(requested, FileDrops.Decide([PathOf("home", "a.txt")], _docs, requested));

        // Entries from two directories are not all "already there".
        Assert.Equal(requested, FileDrops.Decide([PathOf("home", "a.txt"), PathOf("home", "docs", "x.md")], _docs, requested));
    }

    [Fact]
    public async Task DropAsync_Moves_ThenRefreshesSourceAndTarget()
    {
        await _explorer.Pane.StartAsync();
        PaneViewModel docsPane = await _explorer.Workspace.SplitAsync(Core.Layout.SplitAxis.Columns, _docs);

        await _explorer.Drops.DropAsync([PathOf("home", "a.txt")], _docs, TransferMode.Move, docsPane);

        TransferRequest request = Assert.Single(_explorer.Engine.Requests);
        Assert.Equal([PathOf("home", "a.txt")], request.Sources);
        Assert.Equal((_docs, TransferMode.Move), (request.TargetDirectory, request.Mode));
        Assert.Equal(["docs", "docs2"], _explorer.Workspace.Panes[0].Items.Select(item => item.Name));
        Assert.Contains("a.txt", docsPane.Items.Select(item => item.Name));
        Assert.Equal("a.txt", docsPane.CursorItem?.Name);
    }

    [Fact]
    public async Task DropAsync_Copies_WithATitleForTheStatusLine()
    {
        await _explorer.Pane.StartAsync();
        _explorer.Engine.Gate = new TaskCompletionSource();

        Task drop = _explorer.Drops.DropAsync([PathOf("home", "a.txt"), _docs], PathOf("home", "docs2"), TransferMode.Copy, into: null);

        Assert.Equal("Copying 2 items", _explorer.Transfers.Title);
        _explorer.Engine.Gate.SetResult();
        await drop;
        Assert.Equal(TransferMode.Copy, Assert.Single(_explorer.Engine.Requests).Mode);
    }
}

using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

/// <summary>
/// The bookmarks dialog's list: a selection that moves like the cursor, and what open, remove and add do.
/// </summary>
public sealed class BookmarksViewModelTests
{
    private static readonly string Home = PathOf("home");
    private static readonly string Docs = PathOf("home", "docs");
    private static readonly string Pictures = PathOf("home", "Pictures");

    private readonly Bookmarks _bookmarks = new();
    private string? _currentFolder = Pictures;
    private int _closed;

    [Fact]
    public void TheFirstBookmark_IsSelected_AndMovesStopAtTheEnds()
    {
        BookmarksViewModel dialog = Dialog(Home, Docs);

        Assert.Equal(0, dialog.SelectedIndex);
        Assert.Equal(Home, dialog.Selected?.Path);

        dialog.Move(1);
        dialog.Move(1);
        Assert.Equal(1, dialog.SelectedIndex);

        dialog.Move(-5);
        Assert.Equal(0, dialog.SelectedIndex);
    }

    [Fact]
    public void Open_ChoosesTheSelectedBookmark_AndCloses()
    {
        BookmarksViewModel dialog = Dialog(Home, Docs);
        dialog.Move(1);

        dialog.OpenCommand.Execute(null);

        Assert.Equal(Docs, dialog.Chosen?.Path);
        Assert.Equal(1, _closed);
    }

    [Fact]
    public void AnEmptyList_SaysSo_AndOpensNothing()
    {
        BookmarksViewModel dialog = Dialog();

        dialog.OpenCommand.Execute(null);
        dialog.ChooseSelected();

        Assert.True(dialog.IsEmpty);
        Assert.Equal(-1, dialog.SelectedIndex);
        Assert.Null(dialog.Chosen);
        Assert.Equal(0, _closed);
    }

    [Fact]
    public void Remove_TakesTheSelectedOne_AndTheSelectionStaysInPlace()
    {
        BookmarksViewModel dialog = Dialog(Home, Docs, Pictures);
        dialog.Move(1);

        dialog.RemoveCommand.Execute(null);
        Assert.Equal([Home, Pictures], Paths(dialog));
        Assert.Equal(Pictures, dialog.Selected?.Path);

        dialog.RemoveCommand.Execute(null);
        Assert.Equal(Home, dialog.Selected?.Path);

        dialog.RemoveCommand.Execute(null);
        Assert.True(dialog.IsEmpty);
        Assert.Empty(_bookmarks.Items);
    }

    [Fact]
    public void AddCurrent_BookmarksTheCurrentFolder_AndSelectsIt()
    {
        BookmarksViewModel dialog = Dialog(Home, Docs);

        dialog.AddCurrentCommand.Execute(null);
        Assert.Equal([Home, Docs, Pictures], Paths(dialog));
        Assert.Equal(Pictures, dialog.Selected?.Path);

        _currentFolder = Home;
        dialog.AddCurrentCommand.Execute(null);
        Assert.Equal(3, dialog.Items.Count);
        Assert.Equal(Home, dialog.Selected?.Path);

        _currentFolder = null;
        dialog.AddCurrentCommand.Execute(null);
        Assert.Equal(3, dialog.Items.Count);
    }

    [Fact]
    public void TheOpenButton_ChoosesTheSelection_UnlessEnterChoseAlready()
    {
        BookmarksViewModel dialog = Dialog(Home, Docs);

        dialog.ChooseSelected();
        Assert.Equal(Home, dialog.Chosen?.Path);

        dialog.Move(1);
        dialog.ChooseSelected();
        Assert.Equal(Home, dialog.Chosen?.Path);
    }

    private static string[] Paths(BookmarksViewModel dialog) => [.. System.Linq.Enumerable.Select(dialog.Items, item => item.Path)];

    private BookmarksViewModel Dialog(params string[] folders)
    {
        foreach (string folder in folders)
        {
            _bookmarks.Add(folder);
        }

        return new BookmarksViewModel(_bookmarks, () => _currentFolder, () => _closed++);
    }
}

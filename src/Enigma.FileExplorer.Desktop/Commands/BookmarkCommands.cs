using System;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// Bookmarks: <c>B</c> bookmarks the active pane's folder; <c>b</c> (<c>:bookmarks</c>) shows them, and the one
/// picked there opens in the active pane.
/// </summary>
/// <param name="workspace">The panes; the active one's folder is bookmarked, and a picked bookmark opens in it.</param>
/// <param name="bookmarks">The bookmarks.</param>
/// <param name="dialog">Shows the bookmarks.</param>
/// <param name="status">The status line.</param>
public sealed class BookmarkCommands(WorkspaceViewModel workspace, Bookmarks bookmarks, IBookmarksDialogService dialog, IStatusMessages status) : ICommandModule
{
    /// <inheritdoc />
    public void Register(ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(CommandIds.BookmarkAdd, _ =>
        {
            AddCurrent();
            return Task.CompletedTask;
        });
        registry.Register(CommandIds.Bookmarks, _ => ShowAsync());
    }

    private async Task ShowAsync()
    {
        // The pane is read after the dialog: it is the one active when the user picks.
        if (await dialog.ShowAsync() is { } chosen)
        {
            await workspace.ActivePane.NavigateAsync(chosen.Path);
        }
    }

    private void AddCurrent()
    {
        PaneViewModel pane = workspace.ActivePane;

        // Search results are a list of hits, not a folder.
        if (pane.IsSearchResults || (pane.CurrentPath.Length == 0))
        {
            status.Error("Only a folder can be bookmarked");
            return;
        }

        if (bookmarks.Add(pane.CurrentPath) is { } added)
        {
            status.Info($"Bookmarked '{added.Name}'");
        }
        else
        {
            status.Info($"'{Bookmarks.NameOf(pane.CurrentPath)}' is already bookmarked");
        }
    }
}

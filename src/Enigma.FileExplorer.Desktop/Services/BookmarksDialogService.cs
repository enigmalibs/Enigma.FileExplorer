using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Shows the bookmarks in the window's content dialog.
/// </summary>
/// <param name="dialogs">The content dialog service.</param>
/// <param name="bookmarks">The bookmarks.</param>
/// <param name="workspace">The panes; <c>a</c> in the dialog bookmarks the active one's folder.</param>
public sealed class BookmarksDialogService(IContentDialogService dialogs, Bookmarks bookmarks, WorkspaceViewModel workspace) : IBookmarksDialogService
{
    /// <inheritdoc />
    public async Task<Bookmark?> ShowAsync()
    {
        BookmarksViewModel viewModel = new(bookmarks, CurrentFolder, () => dialogs.HideAsync().Forget());

        DialogResult result = await dialogs.ShowAsync(dialog =>
        {
            dialog.Title = "Bookmarks";
            dialog.Content = new BookmarksView { DataContext = viewModel };
            dialog.PrimaryButtonText = "Open";
            dialog.CloseButtonText = "Close";
        });

        if (result == DialogResult.Primary)
        {
            viewModel.ChooseSelected();
        }

        return viewModel.Chosen;
    }

    private string? CurrentFolder()
    {
        PaneViewModel pane = workspace.ActivePane;
        return pane.IsSearchResults || (pane.CurrentPath.Length == 0) ? null : pane.CurrentPath;
    }
}

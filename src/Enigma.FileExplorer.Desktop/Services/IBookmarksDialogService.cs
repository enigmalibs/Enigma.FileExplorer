using System.Threading.Tasks;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Shows the bookmarks, where the user manages them and picks one to open.
/// </summary>
public interface IBookmarksDialogService
{
    /// <summary>
    /// Shows the bookmarks and waits until the dialog closes.
    /// </summary>
    /// <returns>The bookmark chosen to open; <see langword="null"/> when the dialog was closed without one.</returns>
    Task<Bookmark?> ShowAsync();
}

using System.Threading.Tasks;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Shows the About dialog.
/// </summary>
public interface IAboutDialogService
{
    /// <summary>
    /// Shows the dialog and waits until it is closed.
    /// </summary>
    /// <returns>A task that completes when the dialog is closed.</returns>
    /// <remarks>
    /// Nothing is returned: the dialog states what the application is and offers no choice to carry back.
    /// </remarks>
    Task ShowAsync();
}

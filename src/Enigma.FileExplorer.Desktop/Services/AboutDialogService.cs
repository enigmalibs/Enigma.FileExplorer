using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Shows the About view in the window's content dialog; <c>q</c> and Enter in the view close it.
/// </summary>
/// <param name="dialogs">The content dialog service.</param>
public sealed class AboutDialogService(IContentDialogService dialogs) : IAboutDialogService
{
    /// <inheritdoc />
    public async Task ShowAsync()
    {
        await dialogs.ShowAsync(dialog =>
        {
            // No title and no icon: the card would draw them as a column beside the content, and the view
            // already shows the application's own icon and name above everything else.
            dialog.Title = null;
            dialog.Content = new AboutView { DataContext = new AboutViewModel(() => _ = dialogs.HideAsync()) };

            // One button, and it only dismisses: the dialog asks nothing.
            dialog.CloseButtonText = "Close";
            dialog.DefaultButton = DefaultButton.Close;
        });
    }
}

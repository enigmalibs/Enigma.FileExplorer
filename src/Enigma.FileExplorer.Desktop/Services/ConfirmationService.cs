using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Confirmations in the window's content dialog.
/// </summary>
/// <param name="dialogs">The content dialog service.</param>
public sealed class ConfirmationService(IContentDialogService dialogs) : IConfirmationService
{
    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message, string confirmText, bool defaultToConfirm = false)
    {
        DialogResult result = await dialogs.ShowAsync(dialog =>
        {
            dialog.Title = title;
            dialog.Content = message;
            dialog.PrimaryButtonText = confirmText;
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = defaultToConfirm ? DefaultButton.Primary : DefaultButton.Close;
        });

        // Esc or a click on the scrim answers None: only the confirming button is a yes.
        return result == DialogResult.Primary;
    }
}

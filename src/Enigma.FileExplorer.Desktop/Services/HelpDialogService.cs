using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Services;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Shows the key-binding help in the window's content dialog; <c>q</c> in the help closes it.
/// </summary>
/// <param name="dialogs">The content dialog service.</param>
/// <param name="catalog">The catalog the help is generated from.</param>
public sealed class HelpDialogService(IContentDialogService dialogs, CommandCatalog catalog) : IHelpDialogService
{
    /// <inheritdoc />
    public async Task ShowAsync()
    {
        await dialogs.ShowAsync(dialog =>
        {
            dialog.Title = "Key bindings";
            dialog.Content = new HelpView { DataContext = new HelpViewModel(catalog, () => dialogs.HideAsync().Forget()) };
            dialog.CloseButtonText = "Close";
        });
    }
}

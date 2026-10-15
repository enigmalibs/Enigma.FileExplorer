using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Asks the user about a conflict in the window's content dialog: Overwrite, Keep both or Skip, with "apply
/// to all"; Esc cancels the whole transfer.
/// </summary>
/// <param name="dialogs">The content dialog service.</param>
public sealed class ConflictDialogService(IContentDialogService dialogs) : IConflictResolver
{
    /// <inheritdoc />
    public Task<ConflictResolution> ResolveAsync(ConflictInfo conflict, CancellationToken cancellationToken)
        // The engine asks from the thread pool; dialogs live on the UI thread.
        => Dispatcher.UIThread.InvokeAsync(() => AskAsync(conflict));

    private async Task<ConflictResolution> AskAsync(ConflictInfo conflict)
    {
        ConflictViewModel question = new(conflict);

        DialogResult result = await dialogs.ShowAsync(dialog =>
        {
            dialog.Title = "Already exists";
            dialog.Content = new ConflictView { DataContext = question };
            dialog.PrimaryButtonText = "Overwrite";
            dialog.SecondaryButtonText = "Keep both";
            dialog.CloseButtonText = "Skip";
            dialog.DefaultButton = DefaultButton.Close;
        });

        ConflictChoice choice = result switch
        {
            DialogResult.Primary => ConflictChoice.Overwrite,
            DialogResult.Secondary => ConflictChoice.KeepBoth,
            DialogResult.Close => ConflictChoice.Skip,
            _ => ConflictChoice.Cancel,
        };

        return new ConflictResolution(choice, question.ApplyToAll && (choice != ConflictChoice.Cancel));
    }
}

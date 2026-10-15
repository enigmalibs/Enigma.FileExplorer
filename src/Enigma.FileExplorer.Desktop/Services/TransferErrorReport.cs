using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Services;
using Enigma.FileExplorer.Core.Operations;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Lists a transfer's failures in the window's content dialog.
/// </summary>
/// <param name="dialogs">The content dialog service.</param>
public sealed class TransferErrorReport(IContentDialogService dialogs) : ITransferErrorReport
{
    /// <summary>
    /// The most failures listed; a disk that filled up can fail thousands of files.
    /// </summary>
    private const int Listed = 20;

    /// <inheritdoc />
    public async Task ShowAsync(string title, IReadOnlyList<TransferError> errors)
    {
        string list = string.Join('\n', errors.Take(Listed).Select(error => error.Message));
        string more = errors.Count > Listed ? $"\n… and {errors.Count - Listed} more" : string.Empty;

        await dialogs.ShowMessageAsync($"{title}: {errors.Count} could not be done", list + more, "Close");
    }
}

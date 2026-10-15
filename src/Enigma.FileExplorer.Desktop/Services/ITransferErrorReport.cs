using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Operations;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Shows what a transfer could not do, when one line on the status bar is not enough.
/// </summary>
public interface ITransferErrorReport
{
    /// <summary>
    /// Shows the failures and waits until the user has read them.
    /// </summary>
    /// <param name="title">The transfer, e.g. "Copying 3 items".</param>
    /// <param name="errors">The failures.</param>
    /// <returns>A task that completes when the report is closed.</returns>
    Task ShowAsync(string title, IReadOnlyList<TransferError> errors);
}

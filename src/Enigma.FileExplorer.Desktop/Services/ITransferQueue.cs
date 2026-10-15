using System.ComponentModel;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Operations;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Runs copies and moves one at a time in the background, and shows the running one on the status line.
/// </summary>
public interface ITransferQueue : INotifyPropertyChanged
{
    /// <summary>
    /// Whether a transfer is running.
    /// </summary>
    bool IsBusy { get; }

    /// <summary>
    /// What the running transfer is, e.g. "Copying 3 items".
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// The name being transferred.
    /// </summary>
    string? CurrentItem { get; }

    /// <summary>
    /// How far the running transfer has got, from 0 to 1.
    /// </summary>
    double Fraction { get; }

    /// <summary>
    /// Runs a transfer once the ones before it have finished, and reports how it went.
    /// </summary>
    /// <param name="request">What to transfer.</param>
    /// <param name="title">How the status line names it.</param>
    /// <returns>The result, or <see langword="null"/> when the request was refused outright (reported).</returns>
    Task<TransferResult?> RunAsync(TransferRequest request, string title);

    /// <summary>
    /// Cancels the running transfer.
    /// </summary>
    /// <returns><see langword="true"/> when a transfer was running.</returns>
    bool CancelCurrent();
}

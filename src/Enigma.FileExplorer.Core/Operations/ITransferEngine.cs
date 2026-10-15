using System;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// Copies and moves files and directory trees.
/// </summary>
public interface ITransferEngine
{
    /// <summary>
    /// Runs a transfer. Item failures are collected rather than thrown, and cancellation ends the transfer
    /// with <see cref="TransferResult.Cancelled"/> set, never leaving a half-written file behind.
    /// </summary>
    /// <param name="request">What to transfer.</param>
    /// <param name="resolver">Decides conflicts.</param>
    /// <param name="progress">Receives progress, throttled.</param>
    /// <param name="cancellationToken">Stops the transfer.</param>
    /// <returns>How the transfer ended.</returns>
    /// <exception cref="FileOperationException">The request itself is impossible (a missing target, a folder into itself); nothing was done.</exception>
    Task<TransferResult> RunAsync(TransferRequest request, IConflictResolver resolver, IProgress<TransferProgress>? progress, CancellationToken cancellationToken = default);
}

using System.Collections.Generic;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// How a transfer ended.
/// </summary>
/// <param name="Created">The top-level destinations created, for selecting them afterwards.</param>
/// <param name="Errors">The items that failed; the others were transferred.</param>
/// <param name="Skipped">How many items were skipped on a conflict.</param>
/// <param name="Cancelled">Whether the transfer was stopped before the end.</param>
public sealed record TransferResult(IReadOnlyList<string> Created, IReadOnlyList<TransferError> Errors, int Skipped, bool Cancelled)
{
    /// <summary>
    /// Whether every item was transferred.
    /// </summary>
    public bool Succeeded => (Errors.Count == 0) && !Cancelled;
}

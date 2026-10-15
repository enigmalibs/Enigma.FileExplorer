using System;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// How far a transfer has got.
/// </summary>
/// <param name="CompletedItems">Files (and links) done, skipped ones included.</param>
/// <param name="TotalItems">Files (and links) to do.</param>
/// <param name="CompletedBytes">Bytes done.</param>
/// <param name="TotalBytes">Bytes to do.</param>
/// <param name="CurrentItem">The name being transferred.</param>
public sealed record TransferProgress(int CompletedItems, int TotalItems, long CompletedBytes, long TotalBytes, string? CurrentItem)
{
    /// <summary>
    /// The share done, by bytes when there are any, else by items; between 0 and 1.
    /// </summary>
    public double Fraction
        => TotalBytes > 0 ? Math.Clamp((double)CompletedBytes / TotalBytes, 0, 1)
            : TotalItems > 0 ? Math.Clamp((double)CompletedItems / TotalItems, 0, 1)
            : 1;
}

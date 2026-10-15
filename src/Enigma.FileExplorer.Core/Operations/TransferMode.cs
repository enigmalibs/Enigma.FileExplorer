namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// Whether a transfer copies or moves.
/// </summary>
public enum TransferMode
{
    /// <summary>The sources stay where they are.</summary>
    Copy,

    /// <summary>The sources leave where they were.</summary>
    Move,
}

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// The user's answer to a conflict.
/// </summary>
/// <param name="Choice">What to do.</param>
/// <param name="ApplyToAll">Whether the same answer holds for every later conflict of this transfer.</param>
public sealed record ConflictResolution(ConflictChoice Choice, bool ApplyToAll = false);

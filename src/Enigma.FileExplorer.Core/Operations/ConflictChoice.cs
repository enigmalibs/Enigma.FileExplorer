namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// What to do when the destination already exists.
/// </summary>
public enum ConflictChoice
{
    /// <summary>Replace what is there.</summary>
    Overwrite,

    /// <summary>Leave both alone; this item is not transferred.</summary>
    Skip,

    /// <summary>Transfer under a free name, <c>name (2).ext</c>.</summary>
    KeepBoth,

    /// <summary>Stop the whole transfer.</summary>
    Cancel,
}

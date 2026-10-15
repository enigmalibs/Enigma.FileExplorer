namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// The platform's trash (Recycle Bin): deletion the user can undo from the system's own tools.
/// </summary>
public interface ITrash
{
    /// <summary>
    /// Whether an entry can go to the trash. When it cannot, deleting it means deleting it for good, which
    /// the user must agree to first.
    /// </summary>
    /// <param name="path">The entry.</param>
    /// <returns><see langword="true"/> when <see cref="MoveToTrash"/> would keep it recoverable.</returns>
    bool CanTrash(string path);

    /// <summary>
    /// Moves an entry to the trash.
    /// </summary>
    /// <param name="path">The entry.</param>
    /// <exception cref="FileOperationException">The entry could not be moved to the trash; nothing was deleted.</exception>
    void MoveToTrash(string path);
}

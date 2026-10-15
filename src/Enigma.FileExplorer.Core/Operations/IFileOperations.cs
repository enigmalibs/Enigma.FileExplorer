using System.Threading;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// Creates, renames and permanently deletes entries. Every failure is a <see cref="FileOperationException"/>
/// whose message can be shown to the user as is.
/// </summary>
public interface IFileOperations
{
    /// <summary>
    /// Creates a directory.
    /// </summary>
    /// <param name="parent">Where to create it.</param>
    /// <param name="name">Its name, validated first.</param>
    /// <returns>The new directory's full path.</returns>
    /// <exception cref="FileOperationException">The name is invalid or taken, or the directory cannot be created.</exception>
    string CreateDirectory(string parent, string name);

    /// <summary>
    /// Creates an empty file; never overwrites one.
    /// </summary>
    /// <param name="parent">Where to create it.</param>
    /// <param name="name">Its name, validated first.</param>
    /// <returns>The new file's full path.</returns>
    /// <exception cref="FileOperationException">The name is invalid or taken, or the file cannot be created.</exception>
    string CreateFile(string parent, string name);

    /// <summary>
    /// Renames an entry within its directory. A change of case alone is supported, including on
    /// case-insensitive file systems.
    /// </summary>
    /// <param name="path">The entry.</param>
    /// <param name="newName">Its new name, validated first.</param>
    /// <returns>The entry's new full path.</returns>
    /// <exception cref="FileOperationException">The name is invalid or taken, or the entry cannot be renamed.</exception>
    string Rename(string path, string newName);

    /// <summary>
    /// Deletes an entry for good: a directory with everything in it, read-only files included. Links are
    /// removed, never followed.
    /// </summary>
    /// <param name="path">The entry.</param>
    /// <param name="cancellationToken">Stops a large deletion between entries.</param>
    /// <exception cref="FileOperationException">Something could not be deleted.</exception>
    void DeletePermanently(string path, CancellationToken cancellationToken = default);
}

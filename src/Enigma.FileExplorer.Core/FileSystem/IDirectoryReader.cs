using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.FileExplorer.Core.FileSystem;

/// <summary>
/// Reads the entries of a directory.
/// </summary>
public interface IDirectoryReader
{
    /// <summary>
    /// Reads a directory's entries off the calling thread.
    /// </summary>
    /// <param name="path">The directory to read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The listing.</returns>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">The directory itself cannot be read.</exception>
    /// <exception cref="IOException">The directory cannot be read for another reason.</exception>
    Task<DirectoryListing> ReadAsync(string path, CancellationToken cancellationToken = default);
}

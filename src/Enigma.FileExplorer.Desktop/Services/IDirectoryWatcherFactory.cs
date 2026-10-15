using System;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Watches directories for changes made on disk, by this application or any other.
/// </summary>
public interface IDirectoryWatcherFactory
{
    /// <summary>
    /// Starts watching a directory's entries (not its subdirectories' contents).
    /// </summary>
    /// <param name="path">The directory.</param>
    /// <param name="onChanged">
    /// Called after a burst of changes has settled, on the synchronization context that called
    /// <see cref="Watch"/> (the UI thread in the application).
    /// </param>
    /// <returns>Disposing it stops the watch. A directory that cannot be watched yields a watch that never fires.</returns>
    IDisposable Watch(string path, Action onChanged);
}

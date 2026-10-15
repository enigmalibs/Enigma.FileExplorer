using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Core.Search;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// Creates panes over the application's shared, singleton services.
/// </summary>
/// <param name="reader">Reads directories.</param>
/// <param name="launcher">Opens files with their default application.</param>
/// <param name="status">The status bar's message line.</param>
/// <param name="folders">The user's well-known folders.</param>
/// <param name="watchers">Watches the panes' directories.</param>
/// <param name="search">Searches below the panes' directories.</param>
public sealed class PaneFactory(
    IDirectoryReader reader,
    IShellLauncher launcher,
    IStatusMessages status,
    IKnownFolders folders,
    IDirectoryWatcherFactory watchers,
    IFileSearch search) : IPaneFactory
{
    /// <inheritdoc />
    public PaneViewModel Create() => new(reader, launcher, status, folders, watchers, search);
}

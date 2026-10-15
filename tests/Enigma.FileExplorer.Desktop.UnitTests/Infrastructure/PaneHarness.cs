using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A pane over the in-memory file system, with its collaborators exposed for assertions.
/// </summary>
public sealed class PaneHarness
{
    /// <summary>
    /// Creates the harness; the home directory is <c>Root/home</c>.
    /// </summary>
    public PaneHarness()
    {
        Folders = new FakeKnownFolders(TestEntries.PathOf("home"));
        Pane = new PaneViewModel(Reader, Launcher, Status, Folders, Watchers, Search);
    }

    /// <summary>
    /// The in-memory file system.
    /// </summary>
    public FakeDirectoryReader Reader { get; } = new();

    /// <summary>
    /// The recording launcher.
    /// </summary>
    public FakeShellLauncher Launcher { get; } = new();

    /// <summary>
    /// The real status line.
    /// </summary>
    public StatusMessages Status { get; } = new();

    /// <summary>
    /// The scripted search.
    /// </summary>
    public FakeFileSearch Search { get; } = new();

    /// <summary>
    /// The recording watcher factory.
    /// </summary>
    public FakeDirectoryWatcherFactory Watchers { get; } = new();

    /// <summary>
    /// The known folders.
    /// </summary>
    public FakeKnownFolders Folders { get; }

    /// <summary>
    /// The pane under test.
    /// </summary>
    public PaneViewModel Pane { get; }
}

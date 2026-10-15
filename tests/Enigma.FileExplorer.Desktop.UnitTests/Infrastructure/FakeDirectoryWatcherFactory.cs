using System;
using System.Collections.Generic;
using System.Linq;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Records the watches panes start and lets a test announce a change.
/// </summary>
public sealed class FakeDirectoryWatcherFactory : IDirectoryWatcherFactory
{
    /// <summary>
    /// Every watch started, in order, disposed ones included.
    /// </summary>
    public List<FakeWatch> Watches { get; } = [];

    /// <summary>
    /// The watches still running.
    /// </summary>
    public IEnumerable<FakeWatch> Active => Watches.Where(watch => !watch.IsDisposed);

    /// <inheritdoc />
    public IDisposable Watch(string path, Action onChanged)
    {
        FakeWatch watch = new(path, onChanged);
        Watches.Add(watch);
        return watch;
    }

    /// <summary>
    /// Announces a change to every running watch of a path.
    /// </summary>
    /// <param name="path">The directory that changed.</param>
    public void Change(string path)
    {
        foreach (FakeWatch watch in Active.Where(watch => string.Equals(watch.Path, path, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            watch.OnChanged();
        }
    }

    /// <summary>
    /// One watch.
    /// </summary>
    /// <param name="path">The directory watched.</param>
    /// <param name="onChanged">What the watch calls on a change.</param>
    public sealed class FakeWatch(string path, Action onChanged) : IDisposable
    {
        /// <summary>
        /// The directory watched.
        /// </summary>
        public string Path { get; } = path;

        /// <summary>
        /// What the watch calls on a change.
        /// </summary>
        public Action OnChanged { get; } = onChanged;

        /// <summary>
        /// Whether the pane stopped the watch.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc />
        public void Dispose() => IsDisposed = true;
    }
}

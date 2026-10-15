using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Watches directories with <see cref="FileSystemWatcher"/>, coalescing bursts of events into one call.
/// </summary>
/// <param name="logger">Where unwatchable directories are recorded.</param>
public sealed class DirectoryWatcherFactory(ILogger<DirectoryWatcherFactory> logger) : IDirectoryWatcherFactory
{
    /// <summary>
    /// How long the directory must stay quiet before the change is reported.
    /// </summary>
    public static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// The longest a change waits while events keep arriving (a file being written continuously).
    /// </summary>
    public static readonly TimeSpan MaximumDelay = TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public IDisposable Watch(string path, Action onChanged)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(onChanged);

        try
        {
            return new DebouncedWatcher(path, onChanged, SynchronizationContext.Current);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            // Some locations (certain network shares, pseudo file systems) cannot be watched. The pane still
            // works; it just needs a manual refresh there.
            logger.LogInformation(exception, "Cannot watch {Path}", path);
            return new NoWatch();
        }
    }

    private sealed class NoWatch : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class DebouncedWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly Timer _timer;
        private readonly Action _onChanged;
        private readonly SynchronizationContext? _context;
        private readonly Lock _gate = new();
        private long _pendingSince = -1;
        private volatile bool _disposed;

        public DebouncedWatcher(string path, Action onChanged, SynchronizationContext? context)
        {
            _onChanged = onChanged;
            _context = context;
            _timer = new Timer(_ => Fire(), null, Timeout.Infinite, Timeout.Infinite);
            _watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
                    | NotifyFilters.Size | NotifyFilters.Attributes | NotifyFilters.CreationTime,
                InternalBufferSize = 64 * 1024,
            };

            _watcher.Created += OnEvent;
            _watcher.Deleted += OnEvent;
            _watcher.Changed += OnEvent;
            _watcher.Renamed += OnEvent;

            // A buffer overflow loses events; a full refresh is the only truthful answer to that.
            _watcher.Error += (_, _) => Schedule();
            _watcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            _disposed = true;
            _watcher.Dispose();
            _timer.Dispose();
        }

        private void OnEvent(object sender, FileSystemEventArgs e) => Schedule();

        private void Schedule()
        {
            if (_disposed)
            {
                return;
            }

            lock (_gate)
            {
                long now = Environment.TickCount64;

                if (_pendingSince < 0)
                {
                    _pendingSince = now;
                }

                // Trailing debounce, capped: each event pushes the report back, but never beyond the
                // maximum delay after the first one.
                long remaining = (long)MaximumDelay.TotalMilliseconds - (now - _pendingSince);
                long delay = Math.Clamp(Math.Min((long)QuietPeriod.TotalMilliseconds, remaining), 0, (long)QuietPeriod.TotalMilliseconds);

                try
                {
                    _timer.Change(delay, Timeout.Infinite);
                }
                catch (ObjectDisposedException)
                {
                    // Disposed between the check and here; nothing is watching any more.
                }
            }
        }

        private void Fire()
        {
            lock (_gate)
            {
                _pendingSince = -1;
            }

            if (_disposed)
            {
                return;
            }

            if (_context is null)
            {
                _onChanged();
                return;
            }

            _context.Post(_ =>
            {
                if (!_disposed)
                {
                    _onChanged();
                }
            }, null);
        }
    }
}

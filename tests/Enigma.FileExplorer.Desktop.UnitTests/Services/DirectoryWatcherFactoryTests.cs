using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests.Services;

public sealed class DirectoryWatcherFactoryTests
{
    private readonly DirectoryWatcherFactory _factory = new(NullLogger<DirectoryWatcherFactory>.Instance);

    [Fact]
    public async Task Watch_ReportsABurstOfChangesOnce()
    {
        using TempDirectory temp = new();
        int calls = 0;
        TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        using IDisposable watch = _factory.Watch(temp.Path, () =>
        {
            Interlocked.Increment(ref calls);
            changed.TrySetResult();
        });

        temp.CreateFile("a.txt", "1");
        temp.CreateFile("b.txt", "2");
        temp.CreateDirectory("c");

        await changed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // Wait past the quiet period: the burst must not trickle in as several reports.
        await Task.Delay(DirectoryWatcherFactory.QuietPeriod * 3, TestContext.Current.CancellationToken);
        Assert.Equal(1, Volatile.Read(ref calls));
    }

    [Fact]
    public async Task Watch_StopsReportingOnceDisposed()
    {
        using TempDirectory temp = new();
        int calls = 0;

        IDisposable watch = _factory.Watch(temp.Path, () => Interlocked.Increment(ref calls));
        watch.Dispose();
        temp.CreateFile("late.txt");

        await Task.Delay(DirectoryWatcherFactory.QuietPeriod * 3, TestContext.Current.CancellationToken);
        Assert.Equal(0, Volatile.Read(ref calls));
    }

    [Fact]
    public void Watch_AMissingDirectory_ReturnsAWatchThatNeverFires()
    {
        using TempDirectory temp = new();

        using IDisposable watch = _factory.Watch(Path.Combine(temp.Path, "missing"), () => throw new InvalidOperationException("must not fire"));

        Assert.NotNull(watch);
    }
}

using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

public sealed class PaneAutoRefreshTests
{
    private readonly PaneHarness _harness = new();
    private readonly string _home;
    private readonly string _projects;
    private readonly string _deep;

    public PaneAutoRefreshTests()
    {
        _home = _harness.Reader.Add(PathOf("home"), Directory(PathOf("home"), "projects"), File(PathOf("home"), "a.txt"), File(PathOf("home"), "b.txt"));
        _projects = _harness.Reader.Add(PathOf("home", "projects"), Directory(PathOf("home", "projects"), "deep"));
        _deep = _harness.Reader.Add(PathOf("home", "projects", "deep"), File(PathOf("home", "projects", "deep"), "x.txt"));
    }

    private PaneViewModel Pane => _harness.Pane;

    [Fact]
    public async Task Loading_WatchesTheDirectoryOnDisplay()
    {
        await Pane.StartAsync();

        Assert.Equal([_home], _harness.Watchers.Active.Select(watch => watch.Path));
    }

    [Fact]
    public async Task Navigating_MovesTheWatch()
    {
        await Pane.StartAsync();

        await Pane.NavigateAsync(_projects);

        Assert.Equal([_projects], _harness.Watchers.Active.Select(watch => watch.Path));
        Assert.True(_harness.Watchers.Watches[0].IsDisposed);
    }

    [Fact]
    public async Task Refreshing_KeepsTheSameWatch()
    {
        await Pane.StartAsync();

        await Pane.RefreshAsync();

        Assert.Single(_harness.Watchers.Watches);
    }

    [Fact]
    public async Task AChangeOnDisk_RefreshesTheListingAndKeepsTheCursor()
    {
        await Pane.StartAsync();
        Pane.MoveCursorTo(2);
        _harness.Reader.Add(_home, Directory(_home, "projects"), File(_home, "a.txt"), File(_home, "b.txt"), File(_home, "c.txt"));

        _harness.Watchers.Change(_home);
        await WaitForIdleAsync();

        Assert.Equal(["projects", "a.txt", "b.txt", "c.txt"], Pane.Items.Select(item => item.Name));
        Assert.Equal("b.txt", Pane.CursorItem?.Name);
    }

    [Fact]
    public async Task AVanishedDirectory_MovesThePaneToItsNearestExistingAncestor()
    {
        await Pane.NavigateAsync(_deep);
        _harness.Reader.Remove(_deep);
        _harness.Reader.Remove(_projects);

        _harness.Watchers.Change(_deep);
        await WaitForIdleAsync();

        Assert.Equal(_home, Pane.CurrentPath);
        Assert.False(_harness.Status.Current?.IsError);
        Assert.Contains("no longer exists", _harness.Status.Current?.Text, System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task ManualRefresh_OfAVanishedDirectory_AlsoRecovers()
    {
        await Pane.NavigateAsync(_projects);
        _harness.Reader.Remove(_projects);

        await Pane.RefreshAsync();

        Assert.Equal(_home, Pane.CurrentPath);
        Assert.Contains("no longer exists", _harness.Status.Current?.Text, System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dispose_StopsTheWatch()
    {
        await Pane.StartAsync();

        Pane.Dispose();

        Assert.Empty(_harness.Watchers.Active);
    }

    private async Task WaitForIdleAsync()
    {
        // The refresh a change starts runs on the fake reader's completed tasks; yielding lets it finish.
        for (int i = 0; (i < 50) && Pane.IsLoading; i++)
        {
            await Task.Yield();
        }
    }
}

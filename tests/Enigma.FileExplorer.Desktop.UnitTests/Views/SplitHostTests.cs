using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Views;

[Collection(HeadlessCollection.Name)]
public sealed class SplitHostTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ThreePanes_AreLaidOutWithSplitters_AndClosingOneRemovesItsView()
    {
        int panesShown = 0;
        int splitters = 0;
        int afterClose = 0;
        bool activeFocused = false;
        PaneView? keptView = null;
        PaneView? keptAfter = null;

        fixture.RunAsync(async () =>
        {
            ExplorerHarness explorer = new();
            explorer.Reader.Add(PathOf("home"), File(PathOf("home"), "a.txt"));
            await explorer.Workspace.ActivePane.StartAsync();

            SplitHost host = new() { DataContext = explorer.Workspace };
            Window window = new() { Content = host, Width = 1000, Height = 700 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            keptView = host.ActivePaneView;

            await explorer.Workspace.SplitAsync(SplitAxis.Columns);
            await explorer.Workspace.SplitAsync(SplitAxis.Rows);
            Dispatcher.UIThread.RunJobs();

            panesShown = host.GetVisualDescendants().OfType<PaneView>().Count();
            splitters = host.GetVisualDescendants().OfType<GridSplitter>().Count();
            activeFocused = host.ActivePaneView?.IsFocused ?? false;

            explorer.Workspace.Close();
            Dispatcher.UIThread.RunJobs();
            afterClose = host.GetVisualDescendants().OfType<PaneView>().Count();
            keptAfter = host.PaneViews[0];

            window.Close();
        });

        Assert.Equal(3, panesShown);
        Assert.Equal(2, splitters);
        Assert.Equal(2, afterClose);
        Assert.Same(keptView, keptAfter);
        Assert.True(activeFocused, "The keyboard follows the active pane: a new split is ready to type into.");
    }

    [Theory]
    [InlineData(new[] { SplitAxis.Columns, SplitAxis.Columns, SplitAxis.Columns })]
    [InlineData(new[] { SplitAxis.Rows, SplitAxis.Rows, SplitAxis.Rows })]
    [InlineData(new[] { SplitAxis.Columns, SplitAxis.Rows, SplitAxis.Columns, SplitAxis.Rows })]
    public void SplittingTheNewPaneAgain_GivesEveryPaneItsOwnArea(SplitAxis[] splits)
    {
        Rect[] areas = [];
        Rect[] afterClose = [];

        fixture.RunAsync(async () =>
        {
            ExplorerHarness explorer = new();
            explorer.Reader.Add(PathOf("home"), File(PathOf("home"), "a.txt"));
            await explorer.Workspace.ActivePane.StartAsync();

            SplitHost host = new() { DataContext = explorer.Workspace };
            Window window = new() { Content = host, Width = 1200, Height = 900 };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            // Each split activates the new pane, so the next one nests inside it: the view that was the
            // second side of a split becomes the first side of the next.
            foreach (SplitAxis axis in splits)
            {
                await explorer.Workspace.SplitAsync(axis);
                Dispatcher.UIThread.RunJobs();
            }

            areas = AreasOf(host);

            explorer.Workspace.Close();
            Dispatcher.UIThread.RunJobs();
            afterClose = AreasOf(host);

            window.Close();
        });

        AssertTiled(areas, splits.Length + 1);
        AssertTiled(afterClose, splits.Length);
    }

    [Fact]
    public void ClickingAPane_MakesItActive()
    {
        bool activated = false;

        fixture.RunAsync(async () =>
        {
            ExplorerHarness explorer = new();
            explorer.Reader.Add(PathOf("home"), File(PathOf("home"), "a.txt"));
            await explorer.Workspace.ActivePane.StartAsync();
            PaneViewModel first = explorer.Workspace.ActivePane;
            await explorer.Workspace.SplitAsync(SplitAxis.Columns);

            SplitHost host = new() { DataContext = explorer.Workspace };
            Window window = new() { Content = host, Width = 1000, Height = 700 };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            host.PaneViews[0].Focus();
            Dispatcher.UIThread.RunJobs();
            activated = ReferenceEquals(explorer.Workspace.ActivePane, first);
            window.Close();
        });

        Assert.True(activated);
    }

    private static Rect[] AreasOf(SplitHost host)
        => [.. host.PaneViews.Select(view => new Rect(view.TranslatePoint(default, host) ?? default, view.Bounds.Size))];

    private static void AssertTiled(Rect[] areas, int expected)
    {
        Assert.Equal(expected, areas.Length);
        Assert.All(areas, area => Assert.True(area.Width > 1 && area.Height > 1, $"A pane has no area: {area}"));

        for (int i = 0; i < areas.Length; i++)
        {
            for (int j = i + 1; j < areas.Length; j++)
            {
                Rect overlap = areas[i].Intersect(areas[j]);
                Assert.True(overlap.Width < 1 || overlap.Height < 1, $"Panes {i} and {j} overlap: {areas[i]} and {areas[j]}");
            }
        }
    }
}

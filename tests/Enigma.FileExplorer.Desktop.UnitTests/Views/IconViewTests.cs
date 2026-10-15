using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Views;

[Collection(HeadlessCollection.Name)]
public sealed class IconViewTests(HeadlessAvaloniaFixture fixture)
{
    [Theory]
    [InlineData(1000, 104, 8)]
    [InlineData(1000, 148, 6)]
    [InlineData(100, 104, 1)]
    [InlineData(0, 104, 1)]
    public void TilesPerRow_FitsTheWidth(double width, double tileWidth, int expected)
        => Assert.Equal(expected, PaneView.TilesPerRow(width, tileWidth));

    [Fact]
    public void AHugeDirectory_InAnIconView_RealizesOnlyTheVisibleRows()
    {
        int itemsPerRow = 0;
        int realizedRows = 0;
        int rowCount = 0;
        int pageSize = 0;

        fixture.RunAsync(async () =>
        {
            PaneHarness harness = new();
            FileSystemEntry[] entries = [.. Enumerable.Range(0, 5000).Select(i => File(PathOf("big"), $"file{i:0000}.dat", size: i))];
            harness.Reader.Add(PathOf("big"), entries);
            await harness.Pane.NavigateAsync(PathOf("big"));

            PaneView view = new() { DataContext = harness.Pane };
            Window window = new() { Content = view, Width = 1000, Height = 700 };
            window.Show();
            harness.Pane.ViewMode = ViewMode.MediumIcons;
            Dispatcher.UIThread.RunJobs();

            itemsPerRow = harness.Pane.ItemsPerRow;
            rowCount = harness.Pane.Rows.Count;
            pageSize = harness.Pane.PageSize;
            realizedRows = view.IconListControl.GetRealizedContainers().Count();
            window.Close();
        });

        Assert.True(itemsPerRow > 1, $"{itemsPerRow} tiles per row");
        Assert.Equal((5000 + itemsPerRow - 1) / itemsPerRow, rowCount);
        Assert.True(realizedRows < 20, $"{realizedRows} rows realized of {rowCount}");
        Assert.True(pageSize >= itemsPerRow);
    }

    [Fact]
    public void TheIconView_ShowsTiles()
    {
        int tiles = 0;

        fixture.RunAsync(async () =>
        {
            PaneHarness harness = new();
            harness.Reader.Add(PathOf("home"), Directory(PathOf("home"), "docs"), File(PathOf("home"), "a.png"), File(PathOf("home"), "b.txt"));
            await harness.Pane.StartAsync();

            PaneView view = new() { DataContext = harness.Pane };
            Window window = new() { Content = view, Width = 900, Height = 600 };
            window.Show();
            harness.Pane.ViewMode = ViewMode.LargeIcons;
            Dispatcher.UIThread.RunJobs();

            tiles = view.GetVisualDescendants().OfType<Border>().Count(border => border.Classes.Contains("tile"));
            window.Close();
        });

        Assert.Equal(3, tiles);
    }
}

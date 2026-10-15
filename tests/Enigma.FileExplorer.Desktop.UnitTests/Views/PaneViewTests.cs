using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Views;

[Collection(HeadlessCollection.Name)]
public sealed class PaneViewTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void PaneView_RendersARowPerEntry()
    {
        int rows = fixture.Run(() =>
        {
            PaneHarness harness = new();
            harness.Reader.Add(PathOf("home"), File(PathOf("home"), "a.txt"), File(PathOf("home"), "b.txt"), Directory(PathOf("home"), "c"));
            harness.Pane.StartAsync().GetAwaiter().GetResult();

            PaneView view = new() { DataContext = harness.Pane };
            Window window = new() { Content = view, Width = 800, Height = 600 };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            int count = view.ListControl.GetRealizedContainers().Count();
            window.Close();
            return count;
        });

        Assert.Equal(3, rows);
    }

    [Fact]
    public void QuickJumpMatches_PutTheirNameOnAHighlightBand()
    {
        (string Name, bool Banded)[] names = fixture.Run(() =>
        {
            PaneHarness harness = new();
            harness.Reader.Add(PathOf("home"), File(PathOf("home"), "alpha.txt"), File(PathOf("home"), "beta.txt"), File(PathOf("home"), "salt.txt"));
            harness.Pane.StartAsync().GetAwaiter().GetResult();

            PaneView view = new() { DataContext = harness.Pane };
            Window window = new() { Content = view, Width = 800, Height = 600 };
            window.Show();
            harness.Pane.HighlightPattern = "al";
            Dispatcher.UIThread.RunJobs();

            (string, bool)[] shown = [.. view.ListControl.GetVisualDescendants()
                .OfType<TextBlock>()
                .Where(block => block.Classes.Contains("name"))
                .Select(block => (block.Text ?? string.Empty, block.Background is not null))];
            window.Close();
            return shown;
        });

        Assert.Equal([("alpha.txt", true), ("beta.txt", false), ("salt.txt", true)], names);
    }

    [Theory]
    [InlineData(KeyModifiers.None, ClickModifiers.None)]
    [InlineData(KeyModifiers.Control, ClickModifiers.Toggle)]
    [InlineData(KeyModifiers.Meta, ClickModifiers.Toggle)]
    [InlineData(KeyModifiers.Shift, ClickModifiers.Range)]
    [InlineData(KeyModifiers.Shift | KeyModifiers.Control, ClickModifiers.Range)]
    public void ToClickModifiers_MapsThePlatformModifiers(KeyModifiers modifiers, ClickModifiers expected)
        => Assert.Equal(expected, PaneView.ToClickModifiers(modifiers));
}

using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests.Views;

[Collection(HeadlessCollection.Name)]
public sealed class PlacesViewTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void PlacesView_RendersAButtonPerPlace()
    {
        int buttons = 0;

        // RunAsync pumps the dispatcher: the refresh resumes on the UI thread, which a blocking wait would hold.
        fixture.RunAsync(async () =>
        {
            FakePlacesProvider provider = new();
            provider.Places.Add(new Place("Home", TestEntries.PathOf("home"), PlaceKind.Home));
            provider.Places.Add(new Place("Documents", TestEntries.PathOf("home", "Documents"), PlaceKind.Documents));
            provider.Places.Add(new Place("C:", TestEntries.Root, PlaceKind.FixedDrive));
            PlacesViewModel places = new(provider, NullLogger<PlacesViewModel>.Instance);

            PlacesView view = new() { DataContext = places };
            Window window = new() { Content = view, Width = 300, Height = 600 };
            window.Show();
            await places.RefreshAsync();
            Dispatcher.UIThread.RunJobs();

            buttons = view.GetVisualDescendants().OfType<Button>().Count();
            window.Close();
        });

        Assert.Equal(3, buttons);
    }
}

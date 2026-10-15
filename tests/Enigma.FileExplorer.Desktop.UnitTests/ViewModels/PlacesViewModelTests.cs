using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

public sealed class PlacesViewModelTests
{
    private readonly FakePlacesProvider _provider = new();
    private readonly PlacesViewModel _places;

    public PlacesViewModelTests()
    {
        _provider.Places.Add(new Place("Home", PathOf("home"), PlaceKind.Home));
        _provider.Places.Add(new Place("Documents", PathOf("home", "Documents"), PlaceKind.Documents));
        _provider.Places.Add(new Place("Root", Root, PlaceKind.Root));
        _places = new PlacesViewModel(_provider, NullLogger<PlacesViewModel>.Instance);
    }

    [Fact]
    public async Task RefreshAsync_SplitsFoldersFromDevices()
    {
        await _places.RefreshAsync();

        Assert.Equal(["Home", "Documents"], _places.Folders.Select(place => place.Name));
        Assert.Equal(["Root"], _places.Devices.Select(place => place.Name));
        Assert.Equal(["Home", "Documents", "Root"], _places.All.Select(place => place.Name));
    }

    [Fact]
    public async Task UpdateCurrent_HighlightsTheMatchingPlaceOnly()
    {
        await _places.RefreshAsync();

        _places.UpdateCurrent(PathOf("home", "Documents"));

        Assert.Equal([false, true, false], _places.All.Select(place => place.IsCurrent));
    }

    [Fact]
    public async Task RefreshAsync_KeepsTheHighlight()
    {
        _places.UpdateCurrent(PathOf("home"));

        await _places.RefreshAsync();

        Assert.True(_places.All[0].IsCurrent);
    }

    [Fact]
    public async Task Open_RaisesPlaceRequested()
    {
        await _places.RefreshAsync();
        List<PlaceViewModel> requested = [];
        _places.PlaceRequested += (_, place) => requested.Add(place);

        _places.OpenCommand.Execute(_places.All[1]);
        _places.Open(null);

        Assert.Equal(["Documents"], requested.Select(place => place.Name));
    }
}

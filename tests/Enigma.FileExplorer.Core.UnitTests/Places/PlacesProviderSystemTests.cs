using System.Collections.Generic;
using Enigma.FileExplorer.Core.Places;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Places;

/// <summary>
/// Runs the real provider against the machine the tests run on.
/// </summary>
public sealed class PlacesProviderSystemTests
{
    [Fact]
    public void GetPlaces_OnThisMachine_ListsHomeFirstAndAtLeastOneDevice()
    {
        PlacesProvider provider = new(new KnownFolders(), new SystemVolumeSource());

        IReadOnlyList<Place> places = provider.GetPlaces();

        Assert.Equal(PlaceKind.Home, places[0].Kind);
        Assert.Contains(places, place => place.IsDevice);
    }
}

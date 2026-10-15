using System.Collections.Generic;
using Enigma.FileExplorer.Core.Places;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Returns whatever places a test sets.
/// </summary>
public sealed class FakePlacesProvider : IPlacesProvider
{
    /// <summary>
    /// The places returned.
    /// </summary>
    public List<Place> Places { get; } = [];

    /// <inheritdoc />
    public IReadOnlyList<Place> GetPlaces() => [.. Places];
}

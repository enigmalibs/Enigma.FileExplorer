using System.Collections.Generic;

namespace Enigma.FileExplorer.Core.Places;

/// <summary>
/// Lists the places of the sidebar.
/// </summary>
public interface IPlacesProvider
{
    /// <summary>
    /// Lists the places, folders first, then devices. Blocks on the file system: call it off the UI thread.
    /// </summary>
    /// <returns>The places, in sidebar order.</returns>
    IReadOnlyList<Place> GetPlaces();
}

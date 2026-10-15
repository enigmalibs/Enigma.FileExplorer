namespace Enigma.FileExplorer.Core.Places;

/// <summary>
/// A destination of the places sidebar.
/// </summary>
/// <param name="Name">What the sidebar shows.</param>
/// <param name="Path">Where it navigates.</param>
/// <param name="Kind">What it is.</param>
public sealed record Place(string Name, string Path, PlaceKind Kind)
{
    /// <summary>
    /// Whether the place is a device (a drive, a mount or the root) rather than a folder.
    /// </summary>
    public bool IsDevice => Kind >= PlaceKind.Root;
}

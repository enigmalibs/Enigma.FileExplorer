using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.Icons.Phosphor;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// One entry of the places sidebar.
/// </summary>
public sealed class PlaceViewModel : ObservableObject
{
    /// <summary>
    /// Wraps a place.
    /// </summary>
    /// <param name="place">The place.</param>
    public PlaceViewModel(Place place)
    {
        ArgumentNullException.ThrowIfNull(place);

        Place = place;
    }

    /// <summary>
    /// The place.
    /// </summary>
    public Place Place { get; }

    /// <summary>
    /// What the sidebar shows.
    /// </summary>
    public string Name => Place.Name;

    /// <summary>
    /// Where the place navigates.
    /// </summary>
    public string Path => Place.Path;

    /// <summary>
    /// The place's glyph.
    /// </summary>
    public PhosphorIcon Icon => FileIcons.For(Place.Kind);

    /// <summary>
    /// Whether the active pane is showing this place.
    /// </summary>
    public bool IsCurrent { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Whether files dragged over this place would be dropped into it.
    /// </summary>
    public bool IsDropTarget { get; set => SetProperty(ref field, value); }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Places;
using Microsoft.Extensions.Logging;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// The places sidebar: the user's folders, then the devices, in the order <c>g1</c>…<c>g9</c> reach them.
/// </summary>
public sealed class PlacesViewModel : ObservableObject
{
    private readonly IPlacesProvider _provider;
    private readonly ILogger<PlacesViewModel> _logger;
    private string _currentPath = string.Empty;

    /// <summary>
    /// Creates the sidebar; it is empty until <see cref="RefreshAsync"/> runs.
    /// </summary>
    /// <param name="provider">Lists the places.</param>
    /// <param name="logger">Where listing failures are recorded.</param>
    public PlacesViewModel(IPlacesProvider provider, ILogger<PlacesViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(logger);

        _provider = provider;
        _logger = logger;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        OpenCommand = new RelayCommand<PlaceViewModel>(Open);
    }

    /// <summary>
    /// Raised when the user picks a place; the window navigates its active pane there.
    /// </summary>
    public event EventHandler<PlaceViewModel>? PlaceRequested;

    /// <summary>
    /// The user's folders.
    /// </summary>
    public IReadOnlyList<PlaceViewModel> Folders { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>
    /// The drives and mounts.
    /// </summary>
    public IReadOnlyList<PlaceViewModel> Devices { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>
    /// Every place in sidebar order.
    /// </summary>
    public IReadOnlyList<PlaceViewModel> All => [.. Folders, .. Devices];

    /// <summary>
    /// Lists the places again: drives come and go.
    /// </summary>
    public AsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Picks a place.
    /// </summary>
    public RelayCommand<PlaceViewModel> OpenCommand { get; }

    /// <summary>
    /// Lists the places again, off the UI thread: a disconnected network drive can take seconds to answer.
    /// </summary>
    /// <returns>A task that completes when the sidebar is current.</returns>
    public async Task RefreshAsync()
    {
        IReadOnlyList<Place> places;

        try
        {
            places = await Task.Run(_provider.GetPlaces);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The last good list stays; a sidebar that empties itself on a transient error helps nobody.
            _logger.LogWarning(exception, "Could not list the places");
            return;
        }

        Folders = [.. places.Where(place => !place.IsDevice).Select(place => new PlaceViewModel(place))];
        Devices = [.. places.Where(place => place.IsDevice).Select(place => new PlaceViewModel(place))];
        OnPropertyChanged(nameof(All));
        UpdateCurrent(_currentPath);
    }

    /// <summary>
    /// Highlights the place the active pane shows.
    /// </summary>
    /// <param name="path">The active pane's directory.</param>
    public void UpdateCurrent(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        _currentPath = path;

        foreach (PlaceViewModel place in All)
        {
            place.IsCurrent = (path.Length > 0) && PathUtilities.AreSame(place.Path, path);
        }
    }

    /// <summary>
    /// Picks a place.
    /// </summary>
    /// <param name="place">The place.</param>
    public void Open(PlaceViewModel? place)
    {
        if (place is not null)
        {
            PlaceRequested?.Invoke(this, place);
        }
    }
}

using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Diagnostics;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Desktop.Commands;
using Enigma.FileExplorer.Desktop.Formatting;
using Enigma.FileExplorer.Desktop.Input;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// The main window's ViewModel: the workspace of panes, the places sidebar, the window title and the
/// status bar.
/// </summary>
public sealed class MainWindowViewModel : ObservableObject
{
    /// <summary>
    /// The product name, shown in the title bar.
    /// </summary>
    public const string ProductName = ProductInformation.Name;

    private PaneViewModel _observedPane;

    /// <summary>
    /// Creates the ViewModel.
    /// </summary>
    /// <param name="workspace">The panes.</param>
    /// <param name="status">The status bar's message line.</param>
    /// <param name="places">The places sidebar.</param>
    /// <param name="keyboard">Normal mode, and what the status line shows of it.</param>
    /// <param name="commandLine">The command line.</param>
    /// <param name="dispatcher">Runs the command lines submitted.</param>
    /// <param name="transfers">The transfer queue, shown on the status line.</param>
    /// <param name="menus">The panes' context menus.</param>
    /// <param name="drops">Drops of files on the panes and the places.</param>
    /// <param name="catalog">The commands, for the keys the buttons' tooltips name.</param>
    public MainWindowViewModel(
        WorkspaceViewModel workspace,
        IStatusMessages status,
        PlacesViewModel places,
        KeyboardController keyboard,
        CommandLineViewModel commandLine,
        ICommandDispatcher dispatcher,
        ITransferQueue transfers,
        ContextMenus menus,
        FileDrops drops,
        CommandCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        SplitColumnsTip = Tip("Split side by side", catalog, CommandIds.SplitVertical);
        SplitRowsTip = Tip("Split top and bottom", catalog, CommandIds.SplitHorizontal);
        ClosePaneTip = Tip("Close this pane", catalog, CommandIds.ClosePane);

        ArgumentNullException.ThrowIfNull(transfers);
        ArgumentNullException.ThrowIfNull(menus);
        ArgumentNullException.ThrowIfNull(drops);

        Menus = menus;
        Drops = drops;

        Transfers = transfers;
        CancelTransferCommand = new RelayCommand(() => transfers.CancelCurrent());

        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(places);
        ArgumentNullException.ThrowIfNull(keyboard);
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentNullException.ThrowIfNull(dispatcher);

        Workspace = workspace;
        Status = status;
        Places = places;
        Keyboard = keyboard;
        CommandLine = commandLine;
        CommandLine.CommandSubmitted += (_, invocation) => dispatcher.ExecuteAsync(ToInvocation(invocation)).Forget();

        _observedPane = workspace.ActivePane;
        _observedPane.PropertyChanged += OnPanePropertyChanged;
        Workspace.PropertyChanged += OnWorkspacePropertyChanged;
        Places.PlaceRequested += OnPlaceRequested;
    }

    /// <summary>
    /// The panes.
    /// </summary>
    public WorkspaceViewModel Workspace { get; }

    /// <summary>
    /// The active pane.
    /// </summary>
    public PaneViewModel Pane => Workspace.ActivePane;

    /// <summary>
    /// The status bar's message line.
    /// </summary>
    public IStatusMessages Status { get; }

    /// <summary>
    /// The places sidebar.
    /// </summary>
    public PlacesViewModel Places { get; }

    /// <summary>
    /// Normal mode: the status line shows its mode and pending keys.
    /// </summary>
    public KeyboardController Keyboard { get; }

    /// <summary>
    /// The command line.
    /// </summary>
    public CommandLineViewModel CommandLine { get; }

    /// <summary>
    /// The transfer queue: the status line shows the running copy or move.
    /// </summary>
    public ITransferQueue Transfers { get; }

    /// <summary>
    /// The panes' context menus, which the pane views open.
    /// </summary>
    public ContextMenus Menus { get; }

    /// <summary>
    /// Drops of files, which the pane views and the places sidebar receive.
    /// </summary>
    public FileDrops Drops { get; }

    /// <summary>
    /// Cancels the running transfer (the status line's button).
    /// </summary>
    public RelayCommand CancelTransferCommand { get; }

    /// <summary>
    /// The tooltip of the panes' side-by-side split button, with its key.
    /// </summary>
    public string SplitColumnsTip { get; }

    /// <summary>
    /// The tooltip of the panes' stacked split button, with its key.
    /// </summary>
    public string SplitRowsTip { get; }

    /// <summary>
    /// The tooltip of the panes' close button, with its key.
    /// </summary>
    public string ClosePaneTip { get; }

    /// <summary>
    /// The window title: the active directory's name, then the product.
    /// </summary>
    public string Title => Pane.DisplayName.Length > 0 ? $"{Pane.DisplayName} — {ProductName}" : ProductName;

    /// <summary>
    /// The status bar's right-hand summary: item count, and what is marked.
    /// </summary>
    public string SummaryText
    {
        get
        {
            int count = Pane.Items.Count;
            string items = string.Create(CultureInfo.CurrentCulture, $"{count} {(count == 1 ? "item" : "items")}");

            return Pane.MarkedCount == 0
                ? items
                : string.Create(CultureInfo.CurrentCulture, $"{items} · {Pane.MarkedCount} marked ({ByteSize.Format(Pane.MarkedSize)})");
        }
    }

    /// <summary>
    /// Opens the workspace's single pane and lists the places.
    /// </summary>
    /// <param name="path">Where the pane opens; the home directory when omitted.</param>
    /// <param name="view">The view and sort the pane opens with; the defaults when omitted.</param>
    /// <returns>A task that completes when both are on display.</returns>
    public Task StartAsync(string? path = null, PaneSettings? view = null) => Task.WhenAll(Workspace.StartAsync(view, path), Places.RefreshAsync());

    private static string Tip(string title, CommandCatalog catalog, string commandId)
        => catalog.PrimaryGesture(commandId) is { } gesture ? $"{title} ({gesture})" : title;

    private static CommandInvocation ToInvocation(ExInvocation invocation)
        => new(invocation.CommandId, Count: null, invocation.Argument, invocation.Bang);

    private void OnPlaceRequested(object? sender, PlaceViewModel place) => Pane.NavigateAsync(place.Path).Forget();

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WorkspaceViewModel.ActivePane))
        {
            return;
        }

        _observedPane.PropertyChanged -= OnPanePropertyChanged;
        _observedPane = Workspace.ActivePane;
        _observedPane.PropertyChanged += OnPanePropertyChanged;

        OnPropertyChanged(nameof(Pane));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(SummaryText));
        Places.UpdateCurrent(Pane.CurrentPath);
    }

    private void OnPanePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PaneViewModel.DisplayName):
                OnPropertyChanged(nameof(Title));
                break;

            case nameof(PaneViewModel.CurrentPath):
                Places.UpdateCurrent(Pane.CurrentPath);
                break;

            case nameof(PaneViewModel.Items):
            case nameof(PaneViewModel.MarkedCount):
            case nameof(PaneViewModel.MarkedSize):
                OnPropertyChanged(nameof(SummaryText));
                break;
        }
    }
}

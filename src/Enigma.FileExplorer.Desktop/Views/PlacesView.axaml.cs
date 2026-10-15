using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// The places sidebar. Every place is also a drop target: files dropped on it are copied or moved there.
/// </summary>
public partial class PlacesView : UserControl
{
    private PlaceViewModel? _dropPlace;
    private ContextMenu? _menu;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public PlacesView()
    {
        InitializeComponent();

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, OnDragOver);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    /// <summary>
    /// The drop menu on display, exposed for tests.
    /// </summary>
    internal ContextMenu? OpenMenu => _menu is { IsOpen: true } menu ? menu : null;

    private static PlaceViewModel? PlaceAt(object? source)
        => (source as Visual)?.FindAncestorOfType<Button>(includeSelf: true)?.DataContext as PlaceViewModel;

    private void ShowDropTarget(PlaceViewModel? place)
    {
        if (_dropPlace is not null)
        {
            _dropPlace.IsDropTarget = false;
        }

        _dropPlace = place;

        if (place is not null)
        {
            place.IsDropTarget = true;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        PlaceViewModel? place = PlaceAt(e.Source);
        DropPlan? plan = FileDropTarget.Plan(e, place?.Path);
        ShowDropTarget(plan is null ? null : place);
        e.DragEffects = FileDropTarget.HoverEffect(plan, e.DragEffects);
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e) => ShowDropTarget(null);

    private void OnDrop(object? sender, DragEventArgs e)
    {
        // Read now: the dragged data is only there while the drop event lasts.
        DropPlan? plan = FileDropTarget.Plan(e, PlaceAt(e.Source)?.Path);
        ShowDropTarget(null);
        e.DragEffects = FileDropTarget.DropEffect(plan, e.DragEffects);
        e.Handled = true;

        if ((plan is null) || (TopLevel.GetTopLevel(this)?.DataContext is not MainWindowViewModel { Drops: { } drops }))
        {
            return;
        }

        Point position = e.GetPosition(this);

        // Finished after the drop returns: the drag's own loop must be over before a menu opens.
        Dispatcher.UIThread.Post(() => _ = FileDropTarget.CompleteAsync(drops, plan, () => AskDropAsync(position), into: null));
    }

    private Task<TransferMode?> AskDropAsync(Point position)
    {
        (ContextMenu menu, Task<TransferMode?> choice) = FileDropTarget.BuildMenu();

        _menu?.Close();
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_menu, menu))
            {
                _menu = null;
            }
        };

        _menu = menu;
        ContextMenuFactory.Open(menu, this, new Rect(position, new Size(1, 1)));
        return choice;
    }
}

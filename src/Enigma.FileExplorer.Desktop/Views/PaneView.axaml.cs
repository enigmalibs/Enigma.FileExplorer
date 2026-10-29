using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// A pane: its path bar and its listing, as details or as a grid of tiles. Mouse gestures are translated
/// into calls on the <see cref="PaneViewModel"/>, which owns the cursor and the marks; the view keeps the
/// ViewModel told how many entries fit on a row and on a screen.
/// </summary>
public partial class PaneView : UserControl
{
    /// <summary>
    /// The height of a details row, from the row style; it sizes a page for paging keys.
    /// </summary>
    private const double RowHeight = 26;

    /// <summary>
    /// A tile's outer margin on each side, from the tile style.
    /// </summary>
    private const double TileMargin = 4;

    /// <summary>
    /// The horizontal room the icon list keeps: its padding plus a little slack. Fluent's scroll bars
    /// overlay the content, so they take none.
    /// </summary>
    private const double IconListChrome = 12 + 4;

    /// <summary>
    /// How far the pointer moves, pressed, before a press becomes a drag.
    /// </summary>
    private const double DragThreshold = 4;

    private PaneViewModel? _pane;
    private ContextMenu? _menu;
    private PointerPressedEventArgs? _dragTrigger;
    private Point _dragOrigin;
    private EntryViewModel? _pendingClick;
    private EntryViewModel? _dropFolder;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public PaneView()
    {
        InitializeComponent();

        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);
        AddHandler(ContextRequestedEvent, OnContextRequested, RoutingStrategies.Bubble);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Bubble);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, OnDragOver);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
        List.SizeChanged += (_, _) => UpdateGeometry();
        IconList.SizeChanged += (_, _) => UpdateGeometry();
        ResultsList.SizeChanged += (_, _) => UpdateGeometry();
    }

    /// <summary>
    /// The details listing, exposed for the views that drive scrolling and for tests.
    /// </summary>
    internal ItemsControl ListControl => List;

    /// <summary>
    /// The icon listing (rows of tiles), exposed for tests.
    /// </summary>
    internal ItemsControl IconListControl => IconList;

    /// <summary>
    /// The context menu on display, exposed for tests.
    /// </summary>
    internal ContextMenu? OpenMenu => _menu is { IsOpen: true } menu ? menu : null;

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_pane is not null)
        {
            _pane.PropertyChanged -= OnPanePropertyChanged;
            _pane.ContextMenuRequested -= OnContextMenuRequested;
        }

        _pane = DataContext as PaneViewModel;

        if (_pane is not null)
        {
            _pane.PropertyChanged += OnPanePropertyChanged;
            _pane.ContextMenuRequested += OnContextMenuRequested;
            UpdateGeometry();
        }

        base.OnDataContextChanged(e);
    }

    /// <summary>
    /// Maps the platform's modifier keys onto what a click means.
    /// </summary>
    /// <param name="modifiers">The modifiers held during the click.</param>
    /// <returns>The click's meaning.</returns>
    internal static ClickModifiers ToClickModifiers(KeyModifiers modifiers)
    {
        if ((modifiers & KeyModifiers.Shift) != 0)
        {
            return ClickModifiers.Range;
        }

        // Ctrl everywhere, and Cmd on macOS where Ctrl+click is the secondary click.
        return (modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0 ? ClickModifiers.Toggle : ClickModifiers.None;
    }

    /// <summary>
    /// How many tiles fit on a row of a given width.
    /// </summary>
    /// <param name="width">The icon list's width.</param>
    /// <param name="tileWidth">A tile's width, margins excluded.</param>
    /// <returns>At least one.</returns>
    internal static int TilesPerRow(double width, double tileWidth)
        => Math.Max(1, (int)((width - IconListChrome) / (tileWidth + (2 * TileMargin))));

    private void UpdateGeometry()
    {
        if (_pane is null)
        {
            return;
        }

        if (_pane.IsListLayout)
        {
            ItemsControl list = _pane.IsSearchResults ? ResultsList : List;

            if (list.Bounds.Height > 0)
            {
                _pane.PageSize = (int)(list.Bounds.Height / RowHeight);
            }

            return;
        }

        if (IconList.Bounds.Width > 0)
        {
            _pane.ItemsPerRow = TilesPerRow(IconList.Bounds.Width, _pane.TileWidth);
            int rows = Math.Max(1, (int)(IconList.Bounds.Height / (_pane.TileHeight + (2 * TileMargin))));
            _pane.PageSize = rows * _pane.ItemsPerRow;
        }
    }

    private void OnPanePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PaneViewModel.CursorIndex) when _pane is { CursorIndex: >= 0 } pane:
                int index = pane.CursorIndex;

                // Posted: after a load the new list has not been measured yet, and the panel can only bring
                // a realized row into view.
                Dispatcher.UIThread.Post(() => ScrollToCursor(pane, index), DispatcherPriority.Loaded);
                break;

            case nameof(PaneViewModel.ViewMode):
            case nameof(PaneViewModel.IsSearchResults):
                // The other listing becomes visible; its size is only known after the next layout pass.
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateGeometry();

                    if (_pane is { CursorIndex: >= 0 } current)
                    {
                        ScrollToCursor(current, current.CursorIndex);
                    }
                }, DispatcherPriority.Loaded);
                break;

            case nameof(PaneViewModel.IsEditingPath) when _pane is { IsEditingPath: false }:
                // Leaving the path box hands the keyboard back to the listing.
                Focus();
                break;
        }
    }

    private void ScrollToCursor(PaneViewModel pane, int index)
    {
        if (!ReferenceEquals(_pane, pane) || (index != pane.CursorIndex) || (index >= pane.Items.Count))
        {
            return;
        }

        if (pane.IsSearchResults)
        {
            ResultsList.ScrollIntoView(index);
        }
        else if (pane.IsDetailsView)
        {
            List.ScrollIntoView(index);
        }
        else if (pane.Rows.Count > 0)
        {
            IconList.ScrollIntoView(Math.Min(index / pane.ItemsPerRow, pane.Rows.Count - 1));
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ForgetDrag();

        if (_pane is null)
        {
            return;
        }

        PointerPointProperties properties = e.GetCurrentPoint(this).Properties;

        // The mouse's own back and forward buttons, as in every browser and file manager.
        if (properties.IsXButton1Pressed)
        {
            _pane.GoBackCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (properties.IsXButton2Pressed)
        {
            _pane.GoForwardCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if ((e.Source is not Visual source) || (!Listing.IsVisualAncestorOf(source) && !ReferenceEquals(source, Listing)))
        {
            return;
        }

        Focus();

        if (properties.IsRightButtonPressed)
        {
            // The press picks what the menu acts on; the menu itself opens on release (ContextRequested).
            if (source.FindAncestorOfType<Border>(includeSelf: true) is { DataContext: EntryViewModel target })
            {
                _pane.ContextClick(target);
            }

            return;
        }

        if (!properties.IsLeftButtonPressed)
        {
            return;
        }

        if (source.FindAncestorOfType<Border>(includeSelf: true) is { DataContext: EntryViewModel item })
        {
            if (e.ClickCount >= 2)
            {
                _pane.OpenAsync(item).Forget();
            }
            else
            {
                ClickModifiers modifiers = ToClickModifiers(e.KeyModifiers);

                // A plain press on a marked entry may start dragging every marked entry: the click that would
                // keep only this one waits for the release.
                if ((modifiers == ClickModifiers.None) && item.IsMarked)
                {
                    _pendingClick = item;
                }
                else
                {
                    _pane.Click(item, modifiers);
                }

                _dragTrigger = e;
                _dragOrigin = e.GetPosition(this);
            }
        }
        else
        {
            // A click on the empty area of the listing drops the marks, as in every file manager.
            _pane.ClearMarks();
        }

        e.Handled = true;
    }

    private bool IsInListing(Visual source) => ReferenceEquals(source, Listing) || Listing.IsVisualAncestorOf(source);

    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        // A request from the keyboard is left alone: the Menu key and Shift+F10 reach the keymap first, which
        // opens the menu at the cursor rather than at the mouse.
        if ((_pane is null) || (e.Source is not Visual source) || !IsInListing(source) || !e.TryGetPosition(this, out Point position))
        {
            return;
        }

        bool onItem = source.FindAncestorOfType<Border>(includeSelf: true) is { DataContext: EntryViewModel };
        e.Handled = true;
        OpenMenuAsync(onItem, new Rect(position, new Size(1, 1))).Forget();
    }

    private void OnContextMenuRequested(object? sender, EventArgs e)
    {
        if (_pane is null)
        {
            return;
        }

        // Below the cursor entry, as Explorer and Dolphin do; an empty pane gets its own menu near its corner.
        if ((_pane.CursorItem is { } item) && (ContainerOf(item) is { } container) && (container.TranslatePoint(default, this) is { } corner))
        {
            OpenMenuAsync(forItems: true, new Rect(corner, container.Bounds.Size)).Forget();
        }
        else
        {
            Point near = Listing.TranslatePoint(new Point(24, 8), this) ?? default;
            OpenMenuAsync(forItems: _pane.CursorItem is not null, new Rect(near, new Size(1, 1))).Forget();
        }
    }

    private Border? ContainerOf(EntryViewModel item)
        => Listing.GetVisualDescendants()
            .OfType<Border>()
            .FirstOrDefault(border => ReferenceEquals(border.DataContext, item)
                && (border.Classes.Contains("row") || border.Classes.Contains("tile"))
                && border.IsEffectivelyVisible);

    private async Task OpenMenuAsync(bool forItems, Rect anchor)
    {
        if ((_pane is not { } pane) || (Window?.Menus is not { } menus))
        {
            return;
        }

        IReadOnlyList<MenuEntry> entries = forItems ? await menus.ForItemsAsync(pane) : await menus.ForBackgroundAsync(pane);

        // The pane may have been closed, or shown another, while the clipboard was read.
        if ((entries.Count == 0) || !ReferenceEquals(_pane, pane) || (TopLevel.GetTopLevel(this) is null))
        {
            return;
        }

        ContextMenu menu = ContextMenuFactory.Build(entries, entry =>
        {
            // Run once the menu has closed and the listing has the keyboard back, so a command that asks a
            // question (Rename) can hand the keyboard to the command line.
            Dispatcher.UIThread.Post(() => menus.RunAsync(entry).Forget(), DispatcherPriority.Background);
        });

        ShowMenu(menu, anchor);
    }

    private void ShowMenu(ContextMenu menu, Rect anchor)
    {
        _menu?.Close();
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_menu, menu))
            {
                _menu = null;
                Focus();
            }
        };

        _menu = menu;
        ContextMenuFactory.Open(menu, this, anchor);
    }

    private MainWindowViewModel? Window => TopLevel.GetTopLevel(this)?.DataContext as MainWindowViewModel;

    private void ForgetDrag()
    {
        _dragTrigger = null;
        _pendingClick = null;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if ((_pendingClick is { } item) && (_pane is not null))
        {
            _pane.Click(item, ClickModifiers.None);
        }

        ForgetDrag();
    }

    // The one kind of place an async void is allowed: a UI event handler.
    private async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if ((_dragTrigger is not { } trigger) || (_pane is not { } pane) || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        Point position = e.GetPosition(this);

        if ((Math.Abs(position.X - _dragOrigin.X) < DragThreshold) && (Math.Abs(position.Y - _dragOrigin.Y) < DragThreshold))
        {
            return;
        }

        // Cleared before the drag, not after: the drag runs its own event loop, and a second drag started
        // from inside the first would be a drag of a drag.
        ForgetDrag();
        IReadOnlyList<EntryViewModel> dragged = pane.GetTargets();

        if (dragged.Count == 0)
        {
            return;
        }

        try
        {
            DataTransfer data = await FileDragData.CreateAsync(dragged, TopLevel.GetTopLevel(this)?.StorageProvider);

            // The source never deletes anything, whatever the target reports: a move between panes is done
            // by the drop side, and an application dropped on does its own.
            await DragDrop.DoDragDropAsync(trigger, data, DragDropEffects.Copy | DragDropEffects.Move);
        }
        catch (Exception)
        {
            // Swallowed deliberately, as in Enigma.Launchpad: a drag goes through platform code whose failures
            // are not a documented set, and a drag that never started is a gesture that did not happen
            // (nothing has moved), whereas an exception escaping an async void handler would end the
            // application.
        }
    }

    private DropPlan? PlanDrop(DragEventArgs e, out EntryViewModel? folder)
    {
        folder = null;

        if ((_pane is not { } pane) || (e.Source is not Visual source))
        {
            return null;
        }

        // A folder under the pointer takes the drop; anywhere else in the pane (a file included, as in
        // Explorer and Dolphin) drops into the directory on display. Search results have no directory.
        if (IsInListing(source) && (source.FindAncestorOfType<Border>(includeSelf: true) is { DataContext: EntryViewModel { IsDirectory: true } entry }))
        {
            folder = entry;
        }

        string? directory = folder?.FullPath ?? (pane.IsSearchResults || (pane.CurrentPath.Length == 0) ? null : pane.CurrentPath);
        return FileDropTarget.Plan(e, directory);
    }

    private void ShowDropTarget(DropPlan? plan, EntryViewModel? folder)
    {
        if (_dropFolder is not null)
        {
            _dropFolder.IsDropTarget = false;
            _dropFolder = null;
        }

        if (_pane is null)
        {
            return;
        }

        _pane.IsDropTarget = (plan is not null) && (folder is null);

        if ((plan is not null) && (folder is not null))
        {
            folder.IsDropTarget = true;
            _dropFolder = folder;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        DropPlan? plan = PlanDrop(e, out EntryViewModel? folder);
        ShowDropTarget(plan, folder);
        e.DragEffects = FileDropTarget.HoverEffect(plan, e.DragEffects);
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e) => ShowDropTarget(null, null);

    private void OnDrop(object? sender, DragEventArgs e)
    {
        // Read now: the dragged data is only there while the drop event lasts.
        DropPlan? plan = PlanDrop(e, out EntryViewModel? folder);
        ShowDropTarget(null, null);
        e.DragEffects = FileDropTarget.DropEffect(plan, e.DragEffects);
        e.Handled = true;

        if ((plan is null) || (Window?.Drops is not { } drops))
        {
            return;
        }

        Point position = e.GetPosition(this);
        PaneViewModel? into = folder is null ? _pane : null;

        // Finished after the drop returns: the drag's own loop must be over before a menu opens.
        Dispatcher.UIThread.Post(() => FileDropTarget.CompleteAsync(drops, plan, () => AskDropAsync(position), into).Forget());
    }

    private Task<TransferMode?> AskDropAsync(Point position)
    {
        (ContextMenu menu, Task<TransferMode?> choice) = FileDropTarget.BuildMenu();
        ShowMenu(menu, new Rect(position, new Size(1, 1)));
        return choice;
    }
}

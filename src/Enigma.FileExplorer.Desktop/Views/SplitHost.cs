using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// Shows the workspace's split groups: a grid per split, with a splitter between its sides, and a
/// <see cref="PaneView"/> per pane. The visual tree is rebuilt when the layout changes; pane views are
/// kept and moved, so a pane never loses its scroll position to a split elsewhere.
/// </summary>
public sealed class SplitHost : ContentControl
{
    private readonly Dictionary<PaneId, PaneView> _views = [];
    private WorkspaceViewModel? _workspace;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public SplitHost()
    {
        // Any press inside a pane — its list, its path bar, its buttons — makes that pane the active one.
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble);
    }

    /// <summary>
    /// The view of the active pane.
    /// </summary>
    public PaneView? ActivePaneView
        => (_workspace is not null) && _views.TryGetValue(_workspace.ActivePaneId, out PaneView? view) ? view : null;

    /// <summary>
    /// Every pane view, in reading order.
    /// </summary>
    internal IReadOnlyList<PaneView> PaneViews
        => _workspace is null ? [] : [.. _workspace.Layout.Panes.Where(_views.ContainsKey).Select(id => _views[id])];

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_workspace is not null)
        {
            _workspace.LayoutChanged -= OnLayoutChanged;
            _workspace.PropertyChanged -= OnWorkspacePropertyChanged;
        }

        _workspace = DataContext as WorkspaceViewModel;

        if (_workspace is not null)
        {
            _workspace.LayoutChanged += OnLayoutChanged;
            _workspace.PropertyChanged += OnWorkspacePropertyChanged;
        }

        Rebuild();
        base.OnDataContextChanged(e);
    }

    private void OnLayoutChanged(object? sender, EventArgs e) => Rebuild();

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceViewModel.ActivePane))
        {
            FocusActive();
        }
    }

    private void Rebuild()
    {
        bool hadFocus = IsKeyboardFocusWithin;

        // Detach every pane view first: a control can only have one parent, and the new grids are built
        // around the very same views.
        foreach (PaneView view in _views.Values)
        {
            if (view.Parent is Panel panel)
            {
                panel.Children.Remove(view);
            }
        }

        Content = null;

        if (_workspace is null)
        {
            _views.Clear();
            return;
        }

        HashSet<PaneId> live = [.. _workspace.Layout.Panes];

        foreach (PaneId gone in _views.Keys.Where(id => !live.Contains(id)).ToList())
        {
            _views.Remove(gone);
        }

        Content = Build(_workspace.Layout.Root);

        if (hadFocus)
        {
            FocusActive();
        }
    }

    private Control Build(LayoutNode node) => node switch
    {
        PaneNode pane => ViewFor(pane.Id),
        SplitNode split => BuildSplit(split),
        _ => throw new InvalidOperationException("Unknown layout node."),
    };

    private PaneView ViewFor(PaneId id)
    {
        PaneViewModel pane = _workspace!.PaneFor(id);

        if (!_views.TryGetValue(id, out PaneView? view))
        {
            view = new PaneView { DataContext = pane };
            _views[id] = view;
        }
        else if (!ReferenceEquals(view.DataContext, pane))
        {
            // A pane replaced under an id already on screen: the view follows the live pane.
            view.DataContext = pane;
        }

        return view;
    }

    private Grid BuildSplit(SplitNode split)
    {
        bool columns = split.Axis == SplitAxis.Columns;
        Grid grid = new();
        GridLength first = new(split.Ratio, GridUnitType.Star);
        GridLength second = new(1 - split.Ratio, GridUnitType.Star);

        if (columns)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(first));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(second));
        }
        else
        {
            grid.RowDefinitions.Add(new RowDefinition(first));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(second));
        }

        Control firstView = Build(split.First);
        Control secondView = Build(split.Second);
        GridSplitter splitter = new() { ResizeDirection = columns ? GridResizeDirection.Columns : GridResizeDirection.Rows };
        splitter.Classes.Add("paneSplitter");

        // Every cell is set on both axes, every time: pane views are reused across rebuilds, and one that was
        // the second side of a split still carries Column/Row 2 from it, which would stack it on its new
        // neighbour and leave its own cell empty.
        Place(firstView, columns, 0);
        Place(splitter, columns, 1);
        Place(secondView, columns, 2);

        // The ratio is written back once the drag ends, so the layout (and a later save of it) agrees with
        // what is on screen.
        splitter.DragCompleted += (_, _) =>
        {
            double firstSize = columns ? firstView.Bounds.Width : firstView.Bounds.Height;
            double secondSize = columns ? secondView.Bounds.Width : secondView.Bounds.Height;

            if ((firstSize + secondSize) > 0)
            {
                _workspace?.Layout.SetRatio(split, firstSize / (firstSize + secondSize));
            }
        };

        grid.Children.Add(firstView);
        grid.Children.Add(splitter);
        grid.Children.Add(secondView);
        return grid;
    }

    private static void Place(Control control, bool columns, int index)
    {
        Grid.SetColumn(control, columns ? index : 0);
        Grid.SetRow(control, columns ? 0 : index);
    }

    private void FocusActive()
    {
        // Posted: after a rebuild the view is only focusable once it is attached and laid out again.
        Dispatcher.UIThread.Post(() => ActivePaneView?.Focus(), DispatcherPriority.Loaded);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e) => ActivateFrom(e.Source);

    private void OnGotFocus(object? sender, FocusChangedEventArgs e) => ActivateFrom(e.Source);

    private void ActivateFrom(object? source)
    {
        if ((_workspace is not null)
            && (source as Visual)?.FindAncestorOfType<PaneView>(includeSelf: true) is { DataContext: PaneViewModel pane })
        {
            _workspace.Activate(pane);
        }
    }
}

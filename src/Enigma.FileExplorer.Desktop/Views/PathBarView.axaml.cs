using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// A pane's path bar: history buttons, a clickable breadcrumb and a text box to type a path into.
/// </summary>
public partial class PathBarView : UserControl
{
    private PaneViewModel? _pane;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public PathBarView()
    {
        InitializeComponent();

        CrumbArea.AddHandler(PointerPressedEvent, OnCrumbAreaPressed, RoutingStrategies.Bubble);
        PathBox.KeyDown += OnPathBoxKeyDown;
        PathBox.LostFocus += OnPathBoxLostFocus;
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_pane is not null)
        {
            _pane.PropertyChanged -= OnPanePropertyChanged;
        }

        _pane = DataContext as PaneViewModel;

        if (_pane is not null)
        {
            _pane.PropertyChanged += OnPanePropertyChanged;
        }

        base.OnDataContextChanged(e);
    }

    private void OnPanePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PaneViewModel.IsEditingPath) when _pane is { IsEditingPath: true }:
                // Posted: the text box only becomes focusable once the visibility change has been laid out.
                Dispatcher.UIThread.Post(() =>
                {
                    PathBox.Focus();
                    PathBox.SelectAll();
                });
                break;

            case nameof(PaneViewModel.Breadcrumbs):
                // The deepest segments are the interesting ones when the path is too long to fit.
                Dispatcher.UIThread.Post(() => CrumbScroller.ScrollToEnd(), DispatcherPriority.Background);
                break;
        }
    }

    private void OnCrumbAreaPressed(object? sender, PointerPressedEventArgs e)
    {
        // A press that reached the area itself, and not a segment button, means "let me type a path".
        if ((e.Source as Visual)?.FindAncestorOfType<Button>(includeSelf: true) is null)
        {
            _pane?.BeginEditPath();
            e.Handled = true;
        }
    }

    private void OnPathBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (_pane is null)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                _pane.CommitEditPathCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Escape:
                _pane.CancelEditPath();
                e.Handled = true;
                break;
        }
    }

    private void OnPathBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        // Leaving the box abandons the edit, as in every browser's address bar.
        if (_pane is { IsEditingPath: true } && !_pane.CommitEditPathCommand.IsRunning)
        {
            _pane.CancelEditPath();
        }
    }
}

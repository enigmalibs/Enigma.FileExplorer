using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Desktop.Input;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// The main window: the explorer's workspace and the three Enigma overlay hosts.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// How much of a restored window's top-left corner (its title bar) must lie on a screen, in pixels.
    /// </summary>
    private const int VisibleCorner = 120;

    private PixelPoint _normalPosition;
    private Size _normalSize;
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public MainWindow()
    {
        // The generated InitializeComponent is used deliberately: it is what assigns the x:Name'd
        // ContentDialog, Overlay and InfoBar host fields App hands to the Enigma services.
        InitializeComponent();

        // The listing takes the keyboard from the start: the explorer is meant to be driven from it.
        Opened += (_, _) => ActivePane.Focus();

        // Tunnel: the keymap sees a key before any control inside the pane can give it another meaning.
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        // Closing the command line or a dialog hands the keyboard back to the listing.
        HostDialog.Closed += (_, _) => ActivePane.Focus();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.CommandLine.Closed += (_, _) => ActivePane.Focus();
            }
        };

        // The bounds the window returns to from maximized are what a restart restores.
        Opened += (_, _) => RememberNormalBounds();
        PositionChanged += (_, _) => RememberNormalBounds();
        SizeChanged += (_, _) => RememberNormalBounds();

        // Drives come and go while the window is in the background; coming back is when to look again.
        Activated += (_, _) =>
        {
            if (DataContext is MainWindowViewModel { Places.RefreshCommand.IsRunning: false } viewModel)
            {
                viewModel.Places.RefreshCommand.Execute(null);
            }
        };
    }

    /// <summary>
    /// The view of the active pane, which takes the keyboard.
    /// </summary>
    internal PaneView ActivePane => Splits.ActivePaneView ?? throw new InvalidOperationException("No pane is on screen.");

    /// <summary>
    /// Whether a saved placement would put the window's title bar on one of the screens.
    /// </summary>
    /// <param name="placement">The saved placement.</param>
    /// <param name="workingAreas">The screens' working areas, in pixels.</param>
    /// <returns><see langword="true"/> when the window can be restored there.</returns>
    internal static bool IsOnScreen(WindowPlacement placement, IEnumerable<PixelRect> workingAreas)
    {
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(workingAreas);

        if (!double.IsFinite(placement.Width) || !double.IsFinite(placement.Height) || (placement.Width <= 0) || (placement.Height <= 0))
        {
            return false;
        }

        PixelRect corner = new(placement.X, placement.Y, VisibleCorner, VisibleCorner / 3);
        return workingAreas.Any(area => area.Intersects(corner));
    }

    /// <summary>
    /// Puts the window where it was, before it is shown; a placement no longer on any screen (a monitor
    /// unplugged since) is ignored, and the window opens centred at its default size.
    /// </summary>
    /// <param name="placement">The saved placement, if any.</param>
    internal void RestorePlacement(WindowPlacement? placement)
    {
        if ((placement is null) || !IsOnScreen(placement, Screens.All.Select(screen => screen.WorkingArea)))
        {
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Position = new PixelPoint(placement.X, placement.Y);
        Width = Math.Max(placement.Width, MinWidth);
        Height = Math.Max(placement.Height, MinHeight);
        _normalPosition = Position;
        _normalSize = new Size(Width, Height);

        if (placement.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    /// <summary>
    /// Where the window is, for saving: its normal bounds and whether it is maximized. Still answers once the
    /// window has closed.
    /// </summary>
    /// <returns>The placement.</returns>
    internal WindowPlacement CapturePlacement()
    {
        Size size = (_normalSize.Width > 0) && (_normalSize.Height > 0) ? _normalSize : new Size(Width, Height);

        return new WindowPlacement
        {
            X = _normalPosition.X,
            Y = _normalPosition.Y,
            Width = size.Width,
            Height = size.Height,
            IsMaximized = WindowState == WindowState.Maximized,
        };
    }

    private void RememberNormalBounds()
    {
        if ((WindowState != WindowState.Normal) || !IsVisible)
        {
            return;
        }

        _normalPosition = Position;
        _normalSize = ClientSize;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if ((DataContext is not MainWindowViewModel viewModel) || !IsListingFocused())
        {
            return;
        }

        if (KeyStrokeMapper.TryMap(e.Key, e.KeyModifiers, e.KeySymbol) is { } stroke && viewModel.Keyboard.Handle(stroke))
        {
            e.Handled = true;
        }
    }

    private bool IsListingFocused()
    {
        // Text boxes (the path bar, the command line, prompts) and open dialogs keep their keys.
        if (HostDialog.IsOpen || (FocusManager?.GetFocusedElement() is not Visual focused) || (focused is TextBox))
        {
            return false;
        }

        return focused.FindAncestorOfType<PaneView>(includeSelf: true) is not null;
    }
}

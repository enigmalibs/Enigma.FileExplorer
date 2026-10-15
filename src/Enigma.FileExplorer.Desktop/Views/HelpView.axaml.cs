using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Enigma.FileExplorer.Core.Keymaps;
using Enigma.FileExplorer.Desktop.Input;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// The key-binding help shown by <c>?</c>: one card per category in a single scroll area, driven like the
/// listing — <c>j</c>/<c>k</c>, <c>Ctrl+d</c>/<c>Ctrl+u</c>, <c>g</c>/<c>G</c> — and closed with <c>q</c> or Esc.
/// </summary>
/// <remarks>
/// While a dialog is open the window gives no key to the keymap, so the help reads its own keys. It also
/// keeps the dialog card from scrolling: two nested scroll areas fight over the wheel and the bar.
/// </remarks>
public partial class HelpView : UserControl
{
    /// <summary>
    /// How far one <c>j</c> or <c>k</c> scrolls, in pixels: about a line and a half of help.
    /// </summary>
    internal const double LineStep = 40;

    /// <summary>
    /// The tallest the scroll area grows, so the help sits in the window as a dialog, not over it.
    /// </summary>
    private const double TallestScroller = 440;

    /// <summary>
    /// The smallest it shrinks to in a short window, where the card may then scroll a little.
    /// </summary>
    private const double ShortestScroller = 120;

    private TopLevel? _topLevel;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public HelpView()
    {
        InitializeComponent();

        // Tunnel: the help's keys win over anything inside it, the expanders' headers included.
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// The help's scroll area.
    /// </summary>
    internal ScrollViewer ScrollArea => Scroller;

    /// <summary>
    /// Runs a help key.
    /// </summary>
    /// <param name="stroke">The stroke.</param>
    /// <returns>Whether the help used it; Esc is left to the dialog, which closes on it.</returns>
    internal bool Handle(KeyStroke stroke)
    {
        double page = Scroller.Viewport.Height;

        switch (KeyNotation.Format(stroke))
        {
            case "j":
            case "<Down>":
                ScrollBy(LineStep);
                return true;

            case "k":
            case "<Up>":
                ScrollBy(-LineStep);
                return true;

            case "<C-d>":
                ScrollBy(page / 2);
                return true;

            case "<C-u>":
                ScrollBy(-page / 2);
                return true;

            case "<PageDown>":
            case "<Space>":
                ScrollBy(page);
                return true;

            case "<PageUp>":
                ScrollBy(-page);
                return true;

            case "g":
            case "<Home>":
                Scroller.Offset = default;
                return true;

            case "G":
            case "<End>":
                ScrollBy(double.PositiveInfinity);
                return true;

            case "q":
                (DataContext as HelpViewModel)?.CloseCommand.Execute(null);
                return true;

            default:
                return false;
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _topLevel = TopLevel.GetTopLevel(this);

        if (_topLevel is not null)
        {
            _topLevel.SizeChanged += OnTopLevelSizeChanged;
        }

        // Posted: the dialog lays its card out around the help first; then the help fits it and takes the keys.
        Dispatcher.UIThread.Post(() =>
        {
            FitToCard();
            Scroller.Focus();
        }, DispatcherPriority.Loaded);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_topLevel is not null)
        {
            _topLevel.SizeChanged -= OnTopLevelSizeChanged;
            _topLevel = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void OnTopLevelSizeChanged(object? sender, SizeChangedEventArgs e) => FitToCard();

    private void FitToCard() => DialogFit.FitToCard(Scroller, TallestScroller, ShortestScroller);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if ((KeyStrokeMapper.TryMap(e.Key, e.KeyModifiers, e.KeySymbol) is { } stroke) && Handle(stroke))
        {
            e.Handled = true;
        }
    }

    private void ScrollBy(double delta)
    {
        double bottom = Math.Max(0, Scroller.Extent.Height - Scroller.Viewport.Height);
        Scroller.Offset = new Vector(Scroller.Offset.X, Math.Clamp(Scroller.Offset.Y + delta, 0, bottom));
    }
}

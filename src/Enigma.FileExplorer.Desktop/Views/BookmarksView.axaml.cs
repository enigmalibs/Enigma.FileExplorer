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
/// The bookmarks dialog's list, driven like the listing: <c>j</c>/<c>k</c> move, Enter or <c>l</c> opens,
/// <c>a</c> adds the current folder, <c>d</c> or Del removes, <c>q</c> or Esc closes.
/// </summary>
/// <remarks>
/// While a dialog is open the window gives no key to the keymap, so the list reads its own keys.
/// </remarks>
public partial class BookmarksView : UserControl
{
    /// <summary>
    /// The tallest the list grows.
    /// </summary>
    private const double TallestList = 400;

    /// <summary>
    /// The smallest it shrinks to in a short window.
    /// </summary>
    private const double ShortestList = 120;

    private TopLevel? _topLevel;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public BookmarksView()
    {
        InitializeComponent();

        // Tunnel: the dialog's keys win over the list's own arrow and letter handling.
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        List.DoubleTapped += (_, _) => ViewModel?.OpenCommand.Execute(null);
    }

    /// <summary>
    /// The list.
    /// </summary>
    internal ListBox ListControl => List;

    private BookmarksViewModel? ViewModel => DataContext as BookmarksViewModel;

    /// <summary>
    /// Runs a dialog key.
    /// </summary>
    /// <param name="stroke">The stroke.</param>
    /// <returns>Whether the dialog used it; Esc is left to the dialog, which closes on it.</returns>
    internal bool Handle(KeyStroke stroke)
    {
        if (ViewModel is not { } bookmarks)
        {
            return false;
        }

        switch (KeyNotation.Format(stroke))
        {
            case "j":
            case "<Down>":
                bookmarks.Move(1);
                break;

            case "k":
            case "<Up>":
                bookmarks.Move(-1);
                break;

            case "g":
            case "<Home>":
                bookmarks.SelectedIndex = 0;
                break;

            case "G":
            case "<End>":
                bookmarks.SelectedIndex = bookmarks.Items.Count - 1;
                break;

            case "<CR>":
            case "l":
                bookmarks.OpenCommand.Execute(null);
                return true;

            case "d":
            case "<Del>":
                bookmarks.RemoveCommand.Execute(null);
                break;

            case "a":
                bookmarks.AddCurrentCommand.Execute(null);

                // The first bookmark shows the list, which then takes the keys from the view.
                Dispatcher.UIThread.Post(() => List.Focus(), DispatcherPriority.Loaded);
                break;

            case "q":
                bookmarks.CloseCommand.Execute(null);
                return true;

            default:
                return false;
        }

        ShowSelection();
        return true;
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

        // Posted: the dialog lays its card out around the list first; then the list fits it and takes the keys.
        // With no bookmark the list is hidden, and the view itself takes the keys (a, q, Esc).
        Dispatcher.UIThread.Post(() =>
        {
            FitToCard();
            ShowSelection();

            if (!List.Focus())
            {
                Focus();
            }
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

    private void FitToCard() => DialogFit.FitToCard(List, TallestList, ShortestList);

    private void ShowSelection()
    {
        if (List.SelectedIndex >= 0)
        {
            List.ScrollIntoView(List.SelectedIndex);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if ((KeyStrokeMapper.TryMap(e.Key, e.KeyModifiers, e.KeySymbol) is { } stroke) && Handle(stroke))
        {
            e.Handled = true;
        }
    }
}

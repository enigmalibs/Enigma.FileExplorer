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
/// The content of the safety net's dialog. <c>q</c> and Enter close it, like the explorer's other dialogs.
/// </summary>
/// <remarks>
/// While a dialog is open the window gives no key to the keymap, so the view reads its own keys; it takes
/// the keyboard as it opens for that. Everything it shows comes from <see cref="UnhandledErrorViewModel"/>.
/// </remarks>
public partial class UnhandledErrorView : UserControl
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public UnhandledErrorView()
    {
        InitializeComponent();

        // Tunnel: q closes even while the details have the focus.
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Runs a dialog key.
    /// </summary>
    /// <param name="stroke">The stroke.</param>
    /// <param name="onButton">Whether a button has the focus: Enter is then that button's.</param>
    /// <returns>Whether the view used it; Esc is left to the dialog, which closes on it.</returns>
    internal bool Handle(KeyStroke stroke, bool onButton)
    {
        switch (KeyNotation.Format(stroke))
        {
            case "q":
            case "<CR>" when !onButton:
                (DataContext as UnhandledErrorViewModel)?.CloseCommand.Execute(null);
                return true;

            default:
                return false;
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Posted: the dialog lays its card out and settles its own focus first; then the view takes the keys.
        Dispatcher.UIThread.Post(() => Focus(), DispatcherPriority.Loaded);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // The expander's header is a toggle button: Enter there opens the details, and on *Copy details* it copies.
        bool onButton = e.Source is Button;

        if ((KeyStrokeMapper.TryMap(e.Key, e.KeyModifiers, e.KeySymbol) is { } stroke) && Handle(stroke, onButton))
        {
            e.Handled = true;
        }
    }
}

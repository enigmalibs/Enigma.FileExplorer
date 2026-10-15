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
/// The content of the About dialog shown by <c>:about</c>: the application's icon, what it is, which build
/// is running, and its copyright. <c>q</c> and Enter close it, like the explorer's other dialogs.
/// </summary>
/// <remarks>
/// While a dialog is open the window gives no key to the keymap, so the view reads its own keys; it takes
/// the keyboard as it opens for that. Everything it shows comes from <see cref="AboutViewModel"/>.
/// </remarks>
public partial class AboutView : UserControl
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public AboutView()
    {
        InitializeComponent();

        // Tunnel: q closes even while the build line's text has the focus.
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Runs a dialog key.
    /// </summary>
    /// <param name="stroke">The stroke.</param>
    /// <returns>Whether the view used it; Esc is left to the dialog, which closes on it.</returns>
    internal bool Handle(KeyStroke stroke)
    {
        switch (KeyNotation.Format(stroke))
        {
            case "q":
            case "<CR>":
                (DataContext as AboutViewModel)?.CloseCommand.Execute(null);
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
        if ((KeyStrokeMapper.TryMap(e.Key, e.KeyModifiers, e.KeySymbol) is { } stroke) && Handle(stroke))
        {
            e.Handled = true;
        }
    }
}

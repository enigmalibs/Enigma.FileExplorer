using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// The command line at the bottom of the window: its keys (Enter, Esc, history, completion) are
/// intercepted before the text box can give them their usual meaning.
/// </summary>
public partial class CommandLineView : UserControl
{
    private CommandLineViewModel? _commandLine;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public CommandLineView()
    {
        InitializeComponent();

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Input.LostFocus += OnInputLostFocus;
    }

    /// <summary>
    /// The text box, exposed for tests.
    /// </summary>
    internal TextBox InputBox => Input;

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_commandLine is not null)
        {
            _commandLine.PropertyChanged -= OnCommandLinePropertyChanged;
            _commandLine.TextReplaced -= OnTextReplaced;
        }

        _commandLine = DataContext as CommandLineViewModel;

        if (_commandLine is not null)
        {
            _commandLine.PropertyChanged += OnCommandLinePropertyChanged;
            _commandLine.TextReplaced += OnTextReplaced;
        }

        base.OnDataContextChanged(e);
    }

    private void OnCommandLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if ((e.PropertyName == nameof(CommandLineViewModel.IsOpen)) && (_commandLine is { IsOpen: true }))
        {
            // Posted: the box only becomes focusable once the visibility change has been laid out.
            Dispatcher.UIThread.Post(() => Input.Focus());
        }
    }

    private void OnTextReplaced(object? sender, TextSelection selection)
    {
        // After the binding has pushed the new text into the box, put the caret or selection where asked.
        Dispatcher.UIThread.Post(() =>
        {
            int length = Input.Text?.Length ?? 0;
            Input.SelectionStart = Math.Clamp(selection.Start, 0, length);
            Input.SelectionEnd = Math.Clamp(selection.End, 0, length);
            Input.CaretIndex = Math.Clamp(selection.End, 0, length);
        });
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_commandLine is not { IsOpen: true } commandLine)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                commandLine.Accept();
                break;

            case Key.Escape:
                commandLine.Cancel();
                break;

            case Key.Up:
                commandLine.HistoryOlder();
                break;

            case Key.Down:
                commandLine.HistoryNewer();
                break;

            case Key.Tab:
                commandLine.Complete(backwards: (e.KeyModifiers & KeyModifiers.Shift) != 0);
                break;

            default:
                return;
        }

        e.Handled = true;
    }

    private void OnInputLostFocus(object? sender, RoutedEventArgs e)
    {
        // Clicking away abandons the line, as Esc would; a prompt then answers nothing.
        if (_commandLine is { IsOpen: true })
        {
            _commandLine.Cancel();
        }
    }
}

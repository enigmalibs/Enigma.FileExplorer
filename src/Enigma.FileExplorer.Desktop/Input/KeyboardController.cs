using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Keymaps;
using Enigma.FileExplorer.Desktop.Commands;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Input;

/// <summary>
/// Normal mode: feeds keystrokes to the keymap and runs what they resolve to. Also what the status line
/// shows of the keyboard: the mode and the keys typed so far.
/// </summary>
public sealed class KeyboardController : ObservableObject
{
    private readonly KeySequenceResolver _resolver;
    private readonly ICommandDispatcher _dispatcher;
    private readonly WorkspaceViewModel _workspace;
    private PaneViewModel? _observedPane;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="catalog">The command catalog whose keymap is used.</param>
    /// <param name="dispatcher">Runs the commands.</param>
    /// <param name="workspace">The panes; the active one's visual mode is shown.</param>
    public KeyboardController(CommandCatalog catalog, ICommandDispatcher dispatcher, WorkspaceViewModel workspace)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(workspace);

        _resolver = new KeySequenceResolver(catalog.Keymap);
        _dispatcher = dispatcher;
        _workspace = workspace;
        _resolver.PendingChanged += (_, _) => OnPropertyChanged(nameof(PendingKeys));
        _workspace.PropertyChanged += OnWorkspacePropertyChanged;
        ObservePane(_workspace.ActivePane);
    }

    /// <summary>
    /// The mode, as vim's status line shows it.
    /// </summary>
    public string ModeText => _workspace.ActivePane.IsVisualMode ? "VISUAL" : "NORMAL";

    /// <summary>
    /// The count and keys typed so far (vim's <c>showcmd</c>).
    /// </summary>
    public string PendingKeys => _resolver.PendingText;

    /// <summary>
    /// Feeds one keystroke and runs the command it completes.
    /// </summary>
    /// <param name="stroke">The stroke.</param>
    /// <returns>
    /// Whether the keymap used the stroke; an unused one should keep its ordinary meaning in the UI.
    /// The command itself may still be running when this returns.
    /// </returns>
    public bool Handle(KeyStroke stroke)
    {
        KeyResolution resolution = _resolver.Feed(stroke);

        if (resolution is { Kind: KeyResolutionKind.Matched, Binding: { } binding })
        {
            LastCommand = RunAsync(new CommandInvocation(binding.CommandId, resolution.Count, binding.Argument));
        }

        return resolution.IsConsumed;
    }

    /// <summary>
    /// The command the last key started, for tests to await; the UI never waits on it.
    /// </summary>
    internal Task LastCommand { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// Abandons a pending sequence, as when focus leaves the listing.
    /// </summary>
    public void Reset() => _resolver.Reset();

    /// <summary>
    /// Runs a command the way a key does. Exposed so tests can await what <see cref="Handle"/> starts.
    /// </summary>
    /// <param name="invocation">The command.</param>
    /// <returns>A task that completes when the command has run.</returns>
    internal Task RunAsync(CommandInvocation invocation) => _dispatcher.ExecuteAsync(invocation);

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceViewModel.ActivePane))
        {
            _resolver.Reset();
            ObservePane(_workspace.ActivePane);
            OnPropertyChanged(nameof(ModeText));
        }
    }

    private void ObservePane(PaneViewModel pane)
    {
        if (_observedPane is not null)
        {
            _observedPane.PropertyChanged -= OnPanePropertyChanged;
        }

        _observedPane = pane;
        _observedPane.PropertyChanged += OnPanePropertyChanged;
    }

    private void OnPanePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PaneViewModel.IsVisualMode))
        {
            OnPropertyChanged(nameof(ModeText));
        }
    }
}

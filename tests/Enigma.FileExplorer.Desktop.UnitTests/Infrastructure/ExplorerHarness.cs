using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Keymaps;
using Enigma.FileExplorer.Desktop.Commands;
using Enigma.FileExplorer.Desktop.Input;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// The explorer without a window: a workspace over the in-memory file system, the real catalog, dispatcher
/// and command modules, and the keyboard controller, so a test can type keys and watch the panes.
/// </summary>
public sealed class ExplorerHarness
{
    /// <summary>
    /// Builds the harness; the home directory is <c>Root/home</c>.
    /// </summary>
    /// <param name="modules">Builds the command modules over the harness; navigation only when omitted.</param>
    public ExplorerHarness(Func<ExplorerHarness, IEnumerable<ICommandModule>>? modules = null)
    {
        Folders = new FakeKnownFolders(TestEntries.PathOf("home"));
        Operations = new FakeFileOperations(Reader);
        Trash = new FakeTrash(Reader);
        Clipboard = new FileClipboard(SystemClipboard);
        Engine = new FakeTransferEngine(Reader);
        Transfers = new TransferQueue(Engine, new FixedConflictResolver(), Status, ErrorReport);
        Workspace = new WorkspaceViewModel(new PaneFactory(Reader, Launcher, Status, Folders, Watchers, Search), Options, Clipboard);
        Places = new PlacesViewModel(PlacesProvider, NullLogger<PlacesViewModel>.Instance);
        CommandLine = new CommandLineViewModel(CommandCatalog.Default, Workspace, Folders, Status);
        Dispatcher = new CommandDispatcher(
            CommandCatalog.Default,
            Status,
            modules?.Invoke(this) ??
            [
                new NavigationCommands(Workspace, Places, Folders, Status),
                new CommandLineCommands(CommandLine, Workspace, Folders, Help, About, Status),
                new WindowCommands(Workspace, Folders, Shutdown, Status),
                new ViewCommands(Workspace, Options, Status),
                new FileCommands(Workspace, Operations, Trash, Prompt, Confirmation, Options, Status),
                new ClipboardCommands(Workspace, Clipboard, SystemClipboard, Transfers, Status),
                new SearchCommands(Workspace, Prompt, Status),
                new MenuCommands(Workspace),
                new ThemeCommands(Theme, Status),
                new BookmarkCommands(Workspace, Bookmarks, BookmarksDialog, Status),
            ],
            NullLogger<CommandDispatcher>.Instance);
        Keyboard = new KeyboardController(CommandCatalog.Default, Dispatcher, Workspace);
        Menus = new ContextMenus(CommandCatalog.Default, Dispatcher, Clipboard, Workspace);
        Drops = new FileDrops(Transfers, Workspace);
        Window = new MainWindowViewModel(Workspace, Status, Places, Keyboard, CommandLine, Dispatcher, Transfers, Menus, Drops, CommandCatalog.Default);
    }

    /// <summary>The command line (also the prompt service).</summary>
    public CommandLineViewModel CommandLine { get; }

    /// <summary>The scripted search.</summary>
    public FakeFileSearch Search { get; } = new();

    /// <summary>The system clipboard in memory.</summary>
    public FakeSystemClipboard SystemClipboard { get; } = new();

    /// <summary>The explorer's clipboard of files.</summary>
    public FileClipboard Clipboard { get; }

    /// <summary>The transfer engine over the in-memory file system.</summary>
    public FakeTransferEngine Engine { get; }

    /// <summary>The recorded error reports.</summary>
    public FakeTransferErrorReport ErrorReport { get; } = new();

    /// <summary>The real transfer queue over the fake engine.</summary>
    public TransferQueue Transfers { get; }

    /// <summary>File operations on the in-memory file system.</summary>
    public FakeFileOperations Operations { get; }

    /// <summary>The trash of the in-memory file system.</summary>
    public FakeTrash Trash { get; }

    /// <summary>The scripted name prompt (file commands ask through it).</summary>
    public ScriptedPrompt Prompt { get; } = new();

    /// <summary>The scripted confirmation.</summary>
    public ScriptedConfirmation Confirmation { get; } = new();

    /// <summary>The settings every pane shares.</summary>
    public ViewOptions Options { get; } = new();

    /// <summary>The recording shutdown.</summary>
    public FakeAppShutdown Shutdown { get; } = new();

    /// <summary>The recording help dialog.</summary>
    public FakeHelpDialog Help { get; } = new();

    /// <summary>The recording About dialog.</summary>
    public FakeAboutDialog About { get; } = new();

    /// <summary>The main window's ViewModel over the harness, which runs submitted command lines.</summary>
    public MainWindowViewModel Window { get; }

    /// <summary>The in-memory file system.</summary>
    public FakeDirectoryReader Reader { get; } = new();

    /// <summary>The recording launcher.</summary>
    public FakeShellLauncher Launcher { get; } = new();

    /// <summary>The real status line.</summary>
    public StatusMessages Status { get; } = new();

    /// <summary>The recording watcher factory.</summary>
    public FakeDirectoryWatcherFactory Watchers { get; } = new();

    /// <summary>The known folders.</summary>
    public FakeKnownFolders Folders { get; }

    /// <summary>The places the sidebar lists.</summary>
    public FakePlacesProvider PlacesProvider { get; } = new();

    /// <summary>The panes.</summary>
    public WorkspaceViewModel Workspace { get; }

    /// <summary>The places sidebar.</summary>
    public PlacesViewModel Places { get; }

    /// <summary>The dispatcher with the modules registered.</summary>
    public CommandDispatcher Dispatcher { get; }

    /// <summary>The bookmarked folders.</summary>
    public Bookmarks Bookmarks { get; } = new();

    /// <summary>The scripted bookmarks dialog.</summary>
    public FakeBookmarksDialog BookmarksDialog { get; } = new();

    /// <summary>The recording theme.</summary>
    public FakeThemeService Theme { get; } = new();

    /// <summary>The panes' context menus.</summary>
    public ContextMenus Menus { get; }

    /// <summary>Drops of files.</summary>
    public FileDrops Drops { get; }

    /// <summary>Normal mode.</summary>
    public KeyboardController Keyboard { get; }

    /// <summary>The active pane.</summary>
    public PaneViewModel Pane => Workspace.ActivePane;

    /// <summary>
    /// Types an ex command line and presses Enter.
    /// </summary>
    /// <param name="line">The line, without its <c>:</c>.</param>
    public void Ex(string line)
    {
        CommandLine.OpenCommand();
        CommandLine.Text = line;
        CommandLine.Accept();
    }

    /// <summary>
    /// Types keys and waits for the command the last one started.
    /// </summary>
    /// <param name="notation">The keys.</param>
    /// <returns>A task that completes when the command has run.</returns>
    public async Task PressAsync(string notation)
    {
        Press(notation);
        await Keyboard.LastCommand;
    }

    /// <summary>
    /// Runs a command the way a key or the command line would, and waits for it.
    /// </summary>
    /// <param name="commandId">The command.</param>
    /// <param name="argument">Its argument.</param>
    /// <param name="bang">Whether it was typed with <c>!</c>.</param>
    /// <returns>A task that completes when the command has run.</returns>
    public Task RunAsync(string commandId, string? argument = null, bool bang = false)
        => Dispatcher.ExecuteAsync(new CommandInvocation(commandId, null, argument, bang));

    /// <summary>
    /// Types keys written in vim notation, one stroke at a time.
    /// </summary>
    /// <param name="notation">The keys, e.g. <c>5j</c> or <c>&lt;C-w&gt;v</c>.</param>
    /// <returns>Whether the last stroke was consumed.</returns>
    public bool Press(string notation)
    {
        bool consumed = false;

        foreach (KeyStroke stroke in KeyNotation.Parse(notation))
        {
            consumed = Keyboard.Handle(stroke);
        }

        return consumed;
    }
}

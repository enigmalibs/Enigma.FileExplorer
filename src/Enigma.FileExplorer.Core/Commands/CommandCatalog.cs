using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Enigma.FileExplorer.Core.Diagnostics;
using Enigma.FileExplorer.Core.Keymaps;

namespace Enigma.FileExplorer.Core.Commands;

/// <summary>
/// The single source of truth for commands: what each one is, the keys bound to it and the ex command
/// that runs it. Help, menus, the keyboard and the command line all read from here.
/// </summary>
public sealed class CommandCatalog
{
    /// <summary>
    /// Help category: moving around.
    /// </summary>
    public const string NavigationCategory = "Navigation";

    /// <summary>
    /// Help category: marking.
    /// </summary>
    public const string MarksCategory = "Marks";

    /// <summary>
    /// Help category: split groups.
    /// </summary>
    public const string WindowsCategory = "Split groups";

    /// <summary>
    /// Help category: view modes and sorting.
    /// </summary>
    public const string ViewCategory = "View";

    /// <summary>
    /// Help category: file operations.
    /// </summary>
    public const string FilesCategory = "Files";

    /// <summary>
    /// Help category: searching.
    /// </summary>
    public const string SearchCategory = "Search";

    /// <summary>
    /// Help category: bookmarked folders.
    /// </summary>
    public const string BookmarksCategory = "Bookmarks";

    /// <summary>
    /// Help category: the command line and the application.
    /// </summary>
    public const string GeneralCategory = "General";

    /// <summary>
    /// The leader key the split-group commands start with, unless the settings choose another.
    /// </summary>
    public static KeyStroke DefaultLeader { get; } = new(SpecialKeys.Space);

    private readonly FrozenDictionary<string, CommandDescriptor> _byId;

    /// <summary>
    /// Builds a catalog.
    /// </summary>
    /// <param name="commands">Every command.</param>
    /// <param name="keymap">The key bindings.</param>
    /// <param name="exCommands">The ex commands.</param>
    /// <param name="leader">The leader key the keymap was built with.</param>
    public CommandCatalog(IEnumerable<CommandDescriptor> commands, Keymap keymap, IEnumerable<ExCommandDefinition> exCommands, KeyStroke leader)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(keymap);
        ArgumentNullException.ThrowIfNull(exCommands);

        Commands = [.. commands];
        Keymap = keymap;
        ExCommands = [.. exCommands];
        Leader = leader;
        _byId = Commands.ToFrozenDictionary(command => command.Id, StringComparer.Ordinal);
        Parser = new ExCommandParser(ExCommands);
    }

    /// <summary>
    /// The explorer's own catalog, with the default leader.
    /// </summary>
    public static CommandCatalog Default { get; } = Create(DefaultLeader);

    /// <summary>
    /// The leader key: the first stroke of the split-group commands (<c>&lt;Space&gt;v</c>).
    /// </summary>
    public KeyStroke Leader { get; }

    /// <summary>
    /// Every command, in help order.
    /// </summary>
    public IReadOnlyList<CommandDescriptor> Commands { get; }

    /// <summary>
    /// The key bindings.
    /// </summary>
    public Keymap Keymap { get; }

    /// <summary>
    /// The ex commands.
    /// </summary>
    public IReadOnlyList<ExCommandDefinition> ExCommands { get; }

    /// <summary>
    /// A parser over <see cref="ExCommands"/>.
    /// </summary>
    public ExCommandParser Parser { get; }

    /// <summary>
    /// The descriptor of a command.
    /// </summary>
    /// <param name="commandId">The command.</param>
    /// <returns>The descriptor, or <see langword="null"/> for an unknown id.</returns>
    public CommandDescriptor? Find(string commandId)
        => _byId.TryGetValue(commandId, out CommandDescriptor? descriptor) ? descriptor : null;

    /// <summary>
    /// The primary key of a command as people read it (the first binding), for menus.
    /// </summary>
    /// <param name="commandId">The command.</param>
    /// <param name="argument">The fixed argument to match, when the command takes one.</param>
    /// <returns>The description, or <see langword="null"/> when no key is bound.</returns>
    public string? PrimaryGesture(string commandId, string? argument = null)
        => Keymap.BindingsFor(commandId, argument) is [{ } first, ..] ? first.Description : null;

    /// <summary>
    /// The explorer's own catalog, with a leader key of the user's choosing.
    /// </summary>
    /// <param name="leaderNotation">The leader key in vim notation (<c>&lt;Space&gt;</c>, <c>,</c>).</param>
    /// <param name="catalog">The catalog: with that leader, or with <see cref="DefaultLeader"/> when it is unusable.</param>
    /// <param name="error">Why the leader is unusable; <see langword="null"/> when it was used.</param>
    /// <returns>Whether the leader was used.</returns>
    public static bool TryCreate(string? leaderNotation, out CommandCatalog catalog, out string? error)
    {
        catalog = Default;
        error = null;

        if (string.IsNullOrEmpty(leaderNotation))
        {
            error = "The leader key is empty.";
            return false;
        }

        IReadOnlyList<KeyStroke> strokes;

        try
        {
            strokes = KeyNotation.Parse(leaderNotation);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            error = $"The leader key '{leaderNotation}' cannot be read: {exception.Message}";
            return false;
        }

        if (strokes is not [KeyStroke leader])
        {
            error = $"The leader key '{leaderNotation}' must be a single key.";
            return false;
        }

        if (leader.IsDigit)
        {
            error = $"The leader key '{leaderNotation}' is a digit, which starts a count.";
            return false;
        }

        if (leader == DefaultLeader)
        {
            return true;
        }

        try
        {
            catalog = Create(leader);
            return true;
        }
        catch (ArgumentException exception)
        {
            // The keymap is prefix-free: a leader that is already a key, or starts other keys, is refused.
            error = $"The leader key '{leaderNotation}' is already used by other keys: {exception.Message}";
            return false;
        }
    }

    /// <summary>
    /// The explorer's own catalog with a given leader key.
    /// </summary>
    /// <param name="leader">The leader key.</param>
    /// <returns>The catalog.</returns>
    /// <exception cref="ArgumentException">The leader clashes with another key of the keymap.</exception>
    public static CommandCatalog Create(KeyStroke leader)
    {
        List<CommandDescriptor> commands = [];
        List<KeyBinding> bindings = [];
        string lead = KeyNotation.Format(leader);

        void Command(string id, string title, string category, params string[] keys)
        {
            commands.Add(new CommandDescriptor(id, title, category));

            foreach (string key in keys)
            {
                bindings.Add(KeyBinding.Parse(key, id));
            }
        }

        // Navigation.
        Command(CommandIds.CursorDown, "Move down", NavigationCategory, "j", "<Down>");
        Command(CommandIds.CursorUp, "Move up", NavigationCategory, "k", "<Up>");
        Command(CommandIds.Open, "Open (enter folder, open file)", NavigationCategory, "l", "<CR>");
        Command(CommandIds.Parent, "Go to parent folder", NavigationCategory, "h", "<BS>", "<A-Up>");
        Command(CommandIds.CursorLeft, "Left (parent; previous item in icon views)", NavigationCategory, "<Left>");
        Command(CommandIds.CursorRight, "Right (open; next item in icon views)", NavigationCategory, "<Right>");
        Command(CommandIds.CursorFirst, "Go to first item (count: n-th)", NavigationCategory, "gg", "<Home>");
        Command(CommandIds.CursorLast, "Go to last item (count: n-th)", NavigationCategory, "G", "<End>");
        Command(CommandIds.CursorHalfPageDown, "Half page down", NavigationCategory, "<C-d>");
        Command(CommandIds.CursorHalfPageUp, "Half page up", NavigationCategory, "<C-u>");
        Command(CommandIds.CursorPageDown, "Page down", NavigationCategory, "<PageDown>");
        Command(CommandIds.CursorPageUp, "Page up", NavigationCategory, "<PageUp>");
        Command(CommandIds.Back, "Go back", NavigationCategory, "H", "<A-Left>");
        Command(CommandIds.Forward, "Go forward", NavigationCategory, "L", "<A-Right>");
        Command(CommandIds.Home, "Go to home folder", NavigationCategory, "gH");
        Command(CommandIds.Root, "Go to the root of the volume", NavigationCategory, "g/");
        Command(CommandIds.Place, "Go to place n of the sidebar", NavigationCategory);

        for (int place = 1; place <= 9; place++)
        {
            string number = place.ToString(CultureInfo.InvariantCulture);
            bindings.Add(KeyBinding.Parse("g" + number, CommandIds.Place, number));
        }

        Command(CommandIds.ChangeDirectory, "Change directory", NavigationCategory);
        Command(CommandIds.EditPath, "Edit the path", NavigationCategory, "<C-l>");
        Command(CommandIds.Refresh, "Refresh", NavigationCategory, "R", "<F5>");

        // Marks.
        Command(CommandIds.ToggleMark, "Toggle mark and move down", MarksCategory, "m", "<Insert>");
        Command(CommandIds.VisualMode, "Visual mode (mark a range)", MarksCategory, "v");
        Command(CommandIds.MarkAll, "Mark all", MarksCategory, "<C-a>");
        Command(CommandIds.Escape, "Cancel: leave visual mode, clear the highlight, then the marks", MarksCategory, "<Esc>");

        // Split groups: the leader and a letter, and g with a direction to move between panes.
        Command(CommandIds.SplitVertical, "Split side by side", WindowsCategory, lead + "v");
        Command(CommandIds.SplitHorizontal, "Split top and bottom", WindowsCategory, lead + "s");
        Command(CommandIds.FocusLeft, "Focus the pane to the left", WindowsCategory, "gh");
        Command(CommandIds.FocusDown, "Focus the pane below", WindowsCategory, "gj");
        Command(CommandIds.FocusUp, "Focus the pane above", WindowsCategory, "gk");
        Command(CommandIds.FocusRight, "Focus the pane to the right", WindowsCategory, "gl");
        Command(CommandIds.FocusNext, "Focus the next pane", WindowsCategory, lead + "w");
        Command(CommandIds.FocusPrevious, "Focus the previous pane", WindowsCategory, lead + "W");
        Command(CommandIds.ClosePane, "Close the pane", WindowsCategory, lead + "c", lead + "q");
        Command(CommandIds.OnlyPane, "Close every other pane", WindowsCategory, lead + "o");
        Command(CommandIds.Equalize, "Make all panes equal", WindowsCategory, lead + "=");
        Command(CommandIds.Taller, "Make the pane taller", WindowsCategory, lead + "+");
        Command(CommandIds.Shorter, "Make the pane shorter", WindowsCategory, lead + "-");
        Command(CommandIds.Wider, "Make the pane wider", WindowsCategory, lead + ">");
        Command(CommandIds.Narrower, "Make the pane narrower", WindowsCategory, lead + "<lt>");

        // View.
        Command(CommandIds.ViewDetails, "Details view", ViewCategory, "zd", "<C-S-6>");
        Command(CommandIds.ViewMediumIcons, "Medium icons", ViewCategory, "zm", "<C-S-3>");
        Command(CommandIds.ViewLargeIcons, "Large icons", ViewCategory, "zl", "<C-S-2>");
        Command(CommandIds.View, "Set the view mode", ViewCategory);
        Command(CommandIds.ToggleHidden, "Show or hide hidden files", ViewCategory, "zh");
        Command(CommandIds.Set, "Set an option", ViewCategory);
        Command(CommandIds.SortName, "Sort by name", ViewCategory, "on");
        Command(CommandIds.SortModified, "Sort by date modified", ViewCategory, "om");
        Command(CommandIds.SortSize, "Sort by size", ViewCategory, "os");
        Command(CommandIds.SortType, "Sort by type", ViewCategory, "ot");
        Command(CommandIds.SortReverse, "Reverse the sort", ViewCategory, "or");
        Command(CommandIds.Sort, "Sort", ViewCategory);

        // Files.
        Command(CommandIds.NewFile, "New file (end the name with / for a folder)", FilesCategory, "a");
        Command(CommandIds.NewDirectory, "New folder", FilesCategory, "A", "<C-S-n>");
        Command(CommandIds.Rename, "Rename", FilesCategory, "r", "cw", "<F2>");
        Command(CommandIds.Copy, "Copy", FilesCategory, "yy", "<C-c>");
        Command(CommandIds.Cut, "Cut", FilesCategory, "x", "<C-x>");
        Command(CommandIds.Paste, "Paste", FilesCategory, "p", "<C-v>");
        Command(CommandIds.CopyPath, "Copy path", FilesCategory, "yp");
        Command(CommandIds.Trash, "Move to trash", FilesCategory, "d", "<Del>");
        Command(CommandIds.DeletePermanently, "Delete permanently", FilesCategory, "D", "<S-Del>");
        Command(CommandIds.CancelTransfer, "Cancel the running transfer", FilesCategory);

        // Search.
        Command(CommandIds.JumpStart, "Find in this folder (quick-jump)", SearchCategory, "/");
        Command(CommandIds.JumpNext, "Next quick-jump match", SearchCategory, "n");
        Command(CommandIds.JumpPrevious, "Previous quick-jump match", SearchCategory, "N");
        Command(CommandIds.ClearHighlight, "Clear the quick-jump highlight", SearchCategory);
        Command(CommandIds.FindName, "Search file names", SearchCategory, "f", "<C-f>");
        Command(CommandIds.FindContent, "Search file contents", SearchCategory, "F", "<C-S-f>");
        Command(CommandIds.Reveal, "Reveal a search hit in its folder", SearchCategory, "gf");

        // Bookmarks.
        Command(CommandIds.Bookmarks, "Bookmarks: open one in this pane, add, remove", BookmarksCategory, "b");
        Command(CommandIds.BookmarkAdd, "Bookmark this folder", BookmarksCategory, "B");

        // General.
        Command(CommandIds.CommandLine, "Command line", GeneralCategory, ":");
        Command(CommandIds.ContextMenu, "Context menu", GeneralCategory, "<Apps>", "<S-F10>");
        Command(CommandIds.Help, "Key bindings", GeneralCategory, "?", "<F1>");
        Command(CommandIds.About, "About " + ProductInformation.DisplayName, GeneralCategory);
        Command(CommandIds.Theme, "Set the theme", GeneralCategory);
        Command(CommandIds.Quit, "Close the pane (quit with the last one)", GeneralCategory);
        Command(CommandIds.QuitAll, "Quit", GeneralCategory);

        ExCommandDefinition[] exCommands =
        [
            new("cd", 2, CommandIds.ChangeDirectory, ExArgumentKind.Path),
            new("split", 2, CommandIds.SplitHorizontal, ExArgumentKind.Path),
            new("vsplit", 2, CommandIds.SplitVertical, ExArgumentKind.Path),
            new("close", 3, CommandIds.ClosePane),
            new("only", 2, CommandIds.OnlyPane),
            new("quit", 1, CommandIds.Quit),
            new("qall", 2, CommandIds.QuitAll),
            new("mkdir", 2, CommandIds.NewDirectory, ExArgumentKind.Optional),
            new("touch", 2, CommandIds.NewFile, ExArgumentKind.Optional),
            new("rename", 3, CommandIds.Rename, ExArgumentKind.Optional),
            new("delete", 3, CommandIds.Trash, AllowsBang: true, BangCommandId: CommandIds.DeletePermanently),
            new("find", 3, CommandIds.FindName, ExArgumentKind.Optional),
            new("grep", 2, CommandIds.FindContent, ExArgumentKind.Optional),
            new("view", 2, CommandIds.View, ExArgumentKind.Required, Values: ["details", "medium", "large"]),
            new("sort", 3, CommandIds.Sort, ExArgumentKind.Required, AllowsBang: true, Values: ["name", "modified", "size", "type"]),
            new("set", 2, CommandIds.Set, ExArgumentKind.Required, Values: ["hidden", "nohidden", "hidden!", "confirmtrash", "noconfirmtrash", "confirmtrash!"]),
            new("theme", 2, CommandIds.Theme, ExArgumentKind.Required, Values: ["dark", "light", "system"]),
            new("refresh", 3, CommandIds.Refresh),
            new("nohlsearch", 3, CommandIds.ClearHighlight),
            new("help", 1, CommandIds.Help),
            new("about", 2, CommandIds.About),
            new("bookmarks", 2, CommandIds.Bookmarks),
            new("cancel", 3, CommandIds.CancelTransfer),
        ];

        return new CommandCatalog(commands, new Keymap(bindings), exCommands, leader);
    }
}

namespace Enigma.FileExplorer.Core.Commands;

/// <summary>
/// The identifiers of every command the explorer knows. Keys, ex commands, menus and help all refer to
/// commands by these ids, so they can never disagree about what a command is.
/// </summary>
public static class CommandIds
{
    /// <summary>Moves the cursor down (count: by that many).</summary>
    public const string CursorDown = "cursor.down";

    /// <summary>Moves the cursor up (count: by that many).</summary>
    public const string CursorUp = "cursor.up";

    /// <summary>Left arrow: parent in the details view, previous item in the icon views.</summary>
    public const string CursorLeft = "cursor.left";

    /// <summary>Right arrow: open in the details view, next item in the icon views.</summary>
    public const string CursorRight = "cursor.right";

    /// <summary>Moves the cursor to the first item (count: to that item).</summary>
    public const string CursorFirst = "cursor.first";

    /// <summary>Moves the cursor to the last item (count: to that item).</summary>
    public const string CursorLast = "cursor.last";

    /// <summary>Moves the cursor half a page down.</summary>
    public const string CursorHalfPageDown = "cursor.halfPageDown";

    /// <summary>Moves the cursor half a page up.</summary>
    public const string CursorHalfPageUp = "cursor.halfPageUp";

    /// <summary>Moves the cursor a page down.</summary>
    public const string CursorPageDown = "cursor.pageDown";

    /// <summary>Moves the cursor a page up.</summary>
    public const string CursorPageUp = "cursor.pageUp";

    /// <summary>Opens the cursor item: enters a directory, opens a file.</summary>
    public const string Open = "nav.open";

    /// <summary>Goes to the parent directory.</summary>
    public const string Parent = "nav.parent";

    /// <summary>Goes back in history.</summary>
    public const string Back = "nav.back";

    /// <summary>Goes forward in history.</summary>
    public const string Forward = "nav.forward";

    /// <summary>Goes to the home directory.</summary>
    public const string Home = "nav.home";

    /// <summary>Goes to the root of the current volume.</summary>
    public const string Root = "nav.root";

    /// <summary>Goes to a place of the sidebar (argument: its number, from 1).</summary>
    public const string Place = "nav.place";

    /// <summary>Changes directory (argument: the path).</summary>
    public const string ChangeDirectory = "nav.cd";

    /// <summary>Reads the directory again.</summary>
    public const string Refresh = "nav.refresh";

    /// <summary>Edits the path bar.</summary>
    public const string EditPath = "nav.editPath";

    /// <summary>Toggles the mark on the cursor item and moves down.</summary>
    public const string ToggleMark = "mark.toggle";

    /// <summary>Starts or ends visual (range) marking.</summary>
    public const string VisualMode = "mark.visual";

    /// <summary>Marks every item.</summary>
    public const string MarkAll = "mark.all";

    /// <summary>Cancels: leaves visual mode, clears the marks.</summary>
    public const string Escape = "mark.escape";

    /// <summary>Opens the command line.</summary>
    public const string CommandLine = "cmdline.open";

    /// <summary>Starts a quick-jump to a name in the listing.</summary>
    public const string JumpStart = "jump.start";

    /// <summary>Jumps to the next quick-jump match.</summary>
    public const string JumpNext = "jump.next";

    /// <summary>Jumps to the previous quick-jump match.</summary>
    public const string JumpPrevious = "jump.previous";

    /// <summary>Clears the quick-jump highlight (vim's <c>:nohlsearch</c>).</summary>
    public const string ClearHighlight = "jump.clearHighlight";

    /// <summary>Shows the key-binding help.</summary>
    public const string Help = "app.help";

    /// <summary>Shows what the application is and which build is running.</summary>
    public const string About = "app.about";

    /// <summary>Closes the active pane, or quits with the last one.</summary>
    public const string Quit = "app.quit";

    /// <summary>Quits the application.</summary>
    public const string QuitAll = "app.quitAll";

    /// <summary>Sets the theme (argument: dark, light or system).</summary>
    public const string Theme = "app.theme";

    /// <summary>Splits the active pane side by side.</summary>
    public const string SplitVertical = "window.splitVertical";

    /// <summary>Splits the active pane top and bottom.</summary>
    public const string SplitHorizontal = "window.splitHorizontal";

    /// <summary>Focuses the pane to the left.</summary>
    public const string FocusLeft = "window.focusLeft";

    /// <summary>Focuses the pane below.</summary>
    public const string FocusDown = "window.focusDown";

    /// <summary>Focuses the pane above.</summary>
    public const string FocusUp = "window.focusUp";

    /// <summary>Focuses the pane to the right.</summary>
    public const string FocusRight = "window.focusRight";

    /// <summary>Focuses the next pane.</summary>
    public const string FocusNext = "window.focusNext";

    /// <summary>Focuses the previous pane.</summary>
    public const string FocusPrevious = "window.focusPrevious";

    /// <summary>Closes the active pane.</summary>
    public const string ClosePane = "window.close";

    /// <summary>Closes every pane but the active one.</summary>
    public const string OnlyPane = "window.only";

    /// <summary>Gives every pane an equal share.</summary>
    public const string Equalize = "window.equalize";

    /// <summary>Makes the active pane taller.</summary>
    public const string Taller = "window.taller";

    /// <summary>Makes the active pane shorter.</summary>
    public const string Shorter = "window.shorter";

    /// <summary>Makes the active pane wider.</summary>
    public const string Wider = "window.wider";

    /// <summary>Makes the active pane narrower.</summary>
    public const string Narrower = "window.narrower";

    /// <summary>Switches to the details view.</summary>
    public const string ViewDetails = "view.details";

    /// <summary>Switches to medium icons.</summary>
    public const string ViewMediumIcons = "view.medium";

    /// <summary>Switches to large icons.</summary>
    public const string ViewLargeIcons = "view.large";

    /// <summary>Sets the view mode (argument: details, medium or large).</summary>
    public const string View = "view.set";

    /// <summary>Shows or hides hidden files.</summary>
    public const string ToggleHidden = "view.toggleHidden";

    /// <summary>Sets an option (argument: hidden, nohidden, hidden!, confirmtrash, noconfirmtrash, confirmtrash!).</summary>
    public const string Set = "view.option";

    /// <summary>Sorts by name.</summary>
    public const string SortName = "view.sortName";

    /// <summary>Sorts by modification time.</summary>
    public const string SortModified = "view.sortModified";

    /// <summary>Sorts by size.</summary>
    public const string SortSize = "view.sortSize";

    /// <summary>Sorts by type.</summary>
    public const string SortType = "view.sortType";

    /// <summary>Reverses the sort.</summary>
    public const string SortReverse = "view.sortReverse";

    /// <summary>Sorts (argument: name, modified, size or type; bang: descending).</summary>
    public const string Sort = "view.sort";

    /// <summary>Creates a file (argument: its name, else prompted).</summary>
    public const string NewFile = "file.newFile";

    /// <summary>Creates a directory (argument: its name, else prompted).</summary>
    public const string NewDirectory = "file.newDirectory";

    /// <summary>Renames the cursor item (argument: the new name, else prompted).</summary>
    public const string Rename = "file.rename";

    /// <summary>Moves the targets to the trash.</summary>
    public const string Trash = "file.trash";

    /// <summary>Deletes the targets permanently.</summary>
    public const string DeletePermanently = "file.deletePermanently";

    /// <summary>Copies the targets to the clipboard.</summary>
    public const string Copy = "file.copy";

    /// <summary>Cuts the targets to the clipboard.</summary>
    public const string Cut = "file.cut";

    /// <summary>Pastes the clipboard into the current directory.</summary>
    public const string Paste = "file.paste";

    /// <summary>Copies the targets' full paths as text.</summary>
    public const string CopyPath = "file.copyPath";

    /// <summary>Cancels the running transfer.</summary>
    public const string CancelTransfer = "file.cancelTransfer";

    /// <summary>Searches file names below the current directory (argument: the pattern, else prompted).</summary>
    public const string FindName = "search.name";

    /// <summary>Searches file contents below the current directory (argument: the text, else prompted).</summary>
    public const string FindContent = "search.content";

    /// <summary>Reveals a search hit in its folder.</summary>
    public const string Reveal = "search.reveal";

    /// <summary>Bookmarks the current folder.</summary>
    public const string BookmarkAdd = "bookmark.add";

    /// <summary>Shows the bookmarks, to open or manage them.</summary>
    public const string Bookmarks = "bookmark.list";

    /// <summary>Opens the context menu of the cursor item.</summary>
    public const string ContextMenu = "menu.context";
}

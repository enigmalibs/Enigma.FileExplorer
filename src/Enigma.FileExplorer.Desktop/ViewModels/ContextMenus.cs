using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Desktop.Commands;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// The pane's context menus, built from the command catalog: the item menu (a right-click on an entry, the
/// Menu key) and the background menu (a right-click on the empty area). Every entry runs a catalog command
/// through the dispatcher and shows the key that does the same, so the menus teach the keys.
/// </summary>
/// <param name="catalog">Where the keys shown come from.</param>
/// <param name="dispatcher">Runs the entry chosen.</param>
/// <param name="clipboard">Whether there is anything to paste.</param>
/// <param name="workspace">The panes, for whether this one can be closed.</param>
public sealed class ContextMenus(CommandCatalog catalog, ICommandDispatcher dispatcher, IFileClipboard clipboard, WorkspaceViewModel workspace)
{
    /// <summary>
    /// The menu for the entries a command would act on: the marked ones, or the cursor entry.
    /// </summary>
    /// <param name="pane">The pane; its cursor is on the entry the menu was opened for.</param>
    /// <returns>The entries; empty when the cursor is on nothing.</returns>
    public async Task<IReadOnlyList<MenuEntry>> ForItemsAsync(PaneViewModel pane)
    {
        ArgumentNullException.ThrowIfNull(pane);

        if (pane.CursorItem is not { } item)
        {
            return [];
        }

        bool canPaste = await clipboard.CanPasteAsync();
        List<MenuEntry> entries = [Command(CommandIds.Open, "Open")];

        if (item.IsDirectory)
        {
            // The new pane takes the second half of the split: to the right, or below.
            entries.Add(OnFolder(CommandIds.SplitVertical, "Open in a split to the right", item.FullPath));
            entries.Add(OnFolder(CommandIds.SplitHorizontal, "Open in a split below", item.FullPath));
        }

        if (pane.IsSearchResults)
        {
            entries.Add(Command(CommandIds.Reveal, "Reveal in folder"));
        }

        entries.Add(MenuEntry.Separator);
        entries.Add(Command(CommandIds.Cut, "Cut"));
        entries.Add(Command(CommandIds.Copy, "Copy"));

        if (item.IsDirectory)
        {
            entries.Add(OnFolder(CommandIds.Paste, "Paste into folder", item.FullPath, canPaste));
        }

        entries.Add(Command(CommandIds.CopyPath, pane.GetTargets().Count > 1 ? "Copy paths" : "Copy path"));
        entries.Add(MenuEntry.Separator);
        entries.Add(Command(CommandIds.Rename, "Rename"));
        entries.Add(Command(CommandIds.Trash, "Move to trash"));
        entries.Add(Command(CommandIds.DeletePermanently, "Delete permanently"));
        return entries;
    }

    /// <summary>
    /// The menu for the pane itself: creating, pasting, how it shows its entries, and its split group.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <returns>The entries.</returns>
    public async Task<IReadOnlyList<MenuEntry>> ForBackgroundAsync(PaneViewModel pane)
    {
        ArgumentNullException.ThrowIfNull(pane);

        bool hasDirectory = pane.CurrentPath.Length > 0;
        bool canPaste = hasDirectory && await clipboard.CanPasteAsync();
        EntrySort sort = pane.Sort;

        return
        [
            Command(CommandIds.NewDirectory, "New folder", hasDirectory),
            Command(CommandIds.NewFile, "New file", hasDirectory),
            Command(CommandIds.Paste, "Paste", canPaste),
            MenuEntry.Separator,
            Submenu(
                "View",
                [
                    Choice(CommandIds.ViewDetails, "Details", pane.ViewMode == ViewMode.Details),
                    Choice(CommandIds.ViewMediumIcons, "Medium icons", pane.ViewMode == ViewMode.MediumIcons),
                    Choice(CommandIds.ViewLargeIcons, "Large icons", pane.ViewMode == ViewMode.LargeIcons),
                ]),
            Submenu(
                "Sort by",
                [
                    Choice(CommandIds.SortName, "Name", sort.Key == SortKey.Name),
                    Choice(CommandIds.SortModified, "Date modified", sort.Key == SortKey.Modified),
                    Choice(CommandIds.SortSize, "Size", sort.Key == SortKey.Size),
                    Choice(CommandIds.SortType, "Type", sort.Key == SortKey.Type),
                    MenuEntry.Separator,

                    // Reversed means against the key's natural direction (newest and largest come first).
                    Choice(CommandIds.SortReverse, "Reverse", sort != EntrySort.For(sort.Key), MenuToggle.Check),
                ]),
            Choice(CommandIds.ToggleHidden, "Show hidden files", pane.ShowHidden, MenuToggle.Check),
            Command(CommandIds.Refresh, "Refresh", hasDirectory),
            MenuEntry.Separator,
            Submenu(
                "Split",
                [
                    Command(CommandIds.SplitVertical, "Side by side"),
                    Command(CommandIds.SplitHorizontal, "Top and bottom"),
                ]),
            Command(CommandIds.ClosePane, "Close pane", workspace.Panes.Count > 1),
        ];
    }

    /// <summary>
    /// Runs the command of an entry, as its key would.
    /// </summary>
    /// <param name="entry">The entry chosen.</param>
    /// <returns>A task that completes when the command has run.</returns>
    public Task RunAsync(MenuEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return entry is { IsEnabled: true, Invocation: { } invocation } ? dispatcher.ExecuteAsync(invocation) : Task.CompletedTask;
    }

    private static MenuEntry OnFolder(string commandId, string title, string folder, bool enabled = true)
    {
        // No key acts on a folder other than the pane's own, so the entry shows none.
        return new MenuEntry { Title = title, Invocation = new CommandInvocation(commandId, Argument: folder), IsEnabled = enabled };
    }

    private static MenuEntry Submenu(string title, IReadOnlyList<MenuEntry> children) => new() { Title = title, Children = children };

    private MenuEntry Command(string commandId, string title, bool enabled = true)
        => new() { Title = title, Invocation = new CommandInvocation(commandId), Gesture = catalog.PrimaryGesture(commandId), IsEnabled = enabled };

    private MenuEntry Choice(string commandId, string title, bool isChecked, MenuToggle toggle = MenuToggle.Radio)
        => Command(commandId, title) with { Toggle = toggle, IsChecked = isChecked };
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// Creating, renaming and deleting: <c>a</c>/<c>A</c>, <c>r</c>/<c>cw</c>/F2, <c>d</c>/Del (trash),
/// <c>D</c>/Shift+Del (permanently), and <c>:touch</c>, <c>:mkdir</c>, <c>:rename</c>, <c>:delete[!]</c>.
/// </summary>
/// <param name="workspace">The panes; operations act on the active one.</param>
/// <param name="operations">Creates, renames and deletes on disk.</param>
/// <param name="trash">The platform's trash.</param>
/// <param name="prompt">Asks for names on the command line.</param>
/// <param name="confirmation">Confirms deletions.</param>
/// <param name="options">Whether moving to the trash asks first.</param>
/// <param name="status">The status line.</param>
public sealed class FileCommands(
    WorkspaceViewModel workspace,
    IFileOperations operations,
    ITrash trash,
    IPromptService prompt,
    IConfirmationService confirmation,
    ViewOptions options,
    IStatusMessages status) : ICommandModule
{
    /// <summary>
    /// How many failed names an error message lists before summarizing the rest.
    /// </summary>
    private const int NamesShown = 3;

    private PaneViewModel Pane => workspace.ActivePane;

    /// <inheritdoc />
    public void Register(ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(CommandIds.NewFile, invocation => CreateAsync(invocation.Argument, directory: false));
        registry.Register(CommandIds.NewDirectory, invocation => CreateAsync(invocation.Argument, directory: true));
        registry.Register(CommandIds.Rename, invocation => RenameAsync(invocation.Argument));
        registry.Register(CommandIds.Trash, _ => TrashAsync());
        registry.Register(CommandIds.DeletePermanently, _ => DeletePermanentlyAsync());
    }

    private static string List(IReadOnlyList<string> names)
        => names.Count <= NamesShown
            ? string.Join(", ", names)
            : $"{string.Join(", ", names.Take(NamesShown))} and {names.Count - NamesShown} more";

    private static string Count(int count) => count == 1 ? "1 item" : $"{count} items";

    private async Task CreateAsync(string? argument, bool directory)
    {
        PaneViewModel pane = Pane;

        if (pane.CurrentPath.Length == 0)
        {
            return;
        }

        string label = directory ? "New folder: " : "New file (end with / for a folder): ";
        string? typed = argument ?? await prompt.PromptAsync(label);

        // Each failure asks again with what was typed; cancelling that question ends the command.
        while (typed is not null)
        {
            // A trailing separator asks for a folder, as in yazi and lf.
            bool asDirectory = directory || typed.EndsWith('/') || typed.EndsWith('\\');
            string name = typed.TrimEnd('/', '\\');

            try
            {
                string parent = pane.CurrentPath;
                string created = await Task.Run(() => asDirectory ? operations.CreateDirectory(parent, name) : operations.CreateFile(parent, name));
                await pane.RefreshAsync(Path.GetFileName(created), keepMarks: true);
                status.Info($"Created {(asDirectory ? "folder" : "file")} '{name}'");
                return;
            }
            catch (FileOperationException exception)
            {
                status.Error(exception.Message);

                // Asked again with what was typed, so the mistake is a correction, not a retype.
                typed = await prompt.PromptAsync(label, typed);
            }
        }
    }

    private async Task RenameAsync(string? argument)
    {
        PaneViewModel pane = Pane;

        if (pane.CursorItem is not { } item)
        {
            return;
        }

        (string stem, _) = UniqueNameGenerator.Split(item.Name, item.IsDirectory);

        // The stem is selected, so typing replaces the name and keeps the extension.
        string? typed = argument ?? await prompt.PromptAsync("Rename: ", item.Name, 0, stem.Length);

        while ((typed is not null) && (typed != item.Name))
        {
            try
            {
                string path = item.FullPath;
                string newName = typed;
                string renamed = await Task.Run(() => operations.Rename(path, newName));
                await pane.RefreshAsync(Path.GetFileName(renamed), keepMarks: true);
                status.Info($"Renamed '{item.Name}' to '{newName}'");
                return;
            }
            catch (FileOperationException exception)
            {
                status.Error(exception.Message);
                typed = await prompt.PromptAsync("Rename: ", typed, 0, typed.Length);
            }
        }
    }

    private async Task TrashAsync()
    {
        PaneViewModel pane = Pane;
        IReadOnlyList<EntryViewModel> targets = pane.GetTargets();

        if (targets.Count == 0)
        {
            return;
        }

        // The trash can be undone, so Enter confirms; the question can be switched off (:set noconfirmtrash).
        if (options.ConfirmTrash
            && !await confirmation.ConfirmAsync("Move to trash", $"Move {Describe(targets)} to the trash?", "Move to trash", defaultToConfirm: true))
        {
            return;
        }

        string? focus = Survivor(pane, targets);
        List<EntryViewModel> trashable = [.. targets.Where(item => trash.CanTrash(item.FullPath))];
        List<EntryViewModel> notTrashable = [.. targets.Except(trashable)];

        // Where there is no trash, deleting means deleting for good: that needs a yes.
        if ((notTrashable.Count > 0) && !await confirmation.ConfirmAsync(
                "No trash here",
                $"{Describe(notTrashable)} cannot go to the trash on this drive. Delete permanently instead? This cannot be undone.",
                "Delete permanently"))
        {
            notTrashable.Clear();
        }

        if ((trashable.Count == 0) && (notTrashable.Count == 0))
        {
            // Declined: nothing happens, and the cursor stays where it was.
            return;
        }

        List<string> failed = [];

        await Task.Run(() =>
        {
            foreach (EntryViewModel item in trashable)
            {
                Try(() => trash.MoveToTrash(item.FullPath), item, failed);
            }

            foreach (EntryViewModel item in notTrashable)
            {
                Try(() => operations.DeletePermanently(item.FullPath), item, failed);
            }
        });

        pane.EndVisual(keepMarks: true);
        await pane.RefreshAsync(focus, keepMarks: false);
        Report(trashable.Count + notTrashable.Count - failed.Count, failed, done => $"Moved {done} to the trash");
    }

    private async Task DeletePermanentlyAsync()
    {
        PaneViewModel pane = Pane;
        IReadOnlyList<EntryViewModel> targets = pane.GetTargets();

        if (targets.Count == 0)
        {
            return;
        }

        if (!await confirmation.ConfirmAsync("Delete permanently", $"Delete {Describe(targets)} permanently? This cannot be undone.", "Delete"))
        {
            return;
        }

        string? focus = Survivor(pane, targets);
        List<string> failed = [];

        await Task.Run(() =>
        {
            foreach (EntryViewModel item in targets)
            {
                Try(() => operations.DeletePermanently(item.FullPath), item, failed);
            }
        });

        pane.EndVisual(keepMarks: true);
        await pane.RefreshAsync(focus, keepMarks: false);
        Report(targets.Count - failed.Count, failed, done => $"Deleted {done} permanently");
    }

    private static string Describe(IReadOnlyList<EntryViewModel> items) => items.Count == 1 ? $"'{items[0].Name}'" : Count(items.Count);

    private static void Try(Action action, EntryViewModel item, List<string> failed)
    {
        try
        {
            action();
        }
        catch (FileOperationException)
        {
            lock (failed)
            {
                failed.Add(item.Name);
            }
        }
    }

    /// <summary>
    /// The entry the cursor should land on once the targets are gone: the first one after them, else the
    /// last one before them.
    /// </summary>
    private static string? Survivor(PaneViewModel pane, IReadOnlyList<EntryViewModel> targets)
    {
        HashSet<EntryViewModel> gone = [.. targets];
        List<EntryViewModel> items = [.. pane.Items];
        int last = targets.Max(items.IndexOf);
        int first = targets.Min(items.IndexOf);

        return items.Skip(last + 1).FirstOrDefault(item => !gone.Contains(item))?.Name
            ?? items.Take(first).LastOrDefault(item => !gone.Contains(item))?.Name;
    }

    private void Report(int done, List<string> failed, Func<string, string> success)
    {
        if (failed.Count > 0)
        {
            status.Error($"Could not delete {List(failed)}");
        }
        else
        {
            status.Info(success(Count(done)));
        }
    }
}

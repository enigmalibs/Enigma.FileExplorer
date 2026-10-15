using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// Copy, cut and paste of files (<c>yy</c>, <c>x</c>, <c>p</c> and Ctrl+C/X/V), copying paths as text
/// (<c>yp</c>) and cancelling the running transfer (<c>:cancel</c>).
/// </summary>
/// <param name="workspace">The panes; the active one is copied from and pasted into.</param>
/// <param name="clipboard">The explorer's clipboard of files.</param>
/// <param name="system">The system clipboard, for paths as text.</param>
/// <param name="transfers">Runs the pastes.</param>
/// <param name="status">The status line.</param>
public sealed class ClipboardCommands(
    WorkspaceViewModel workspace,
    IFileClipboard clipboard,
    ISystemClipboard system,
    ITransferQueue transfers,
    IStatusMessages status) : ICommandModule
{
    private PaneViewModel Pane => workspace.ActivePane;

    /// <inheritdoc />
    public void Register(ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(CommandIds.Copy, _ => PutAsync(TransferMode.Copy));
        registry.Register(CommandIds.Cut, _ => PutAsync(TransferMode.Move));
        registry.Register(CommandIds.Paste, invocation => PasteAsync(invocation.Argument));
        registry.Register(CommandIds.CopyPath, _ => CopyPathsAsync());
        registry.Register(CommandIds.CancelTransfer, _ =>
        {
            if (!transfers.CancelCurrent())
            {
                status.Info("No transfer is running");
            }

            return Task.CompletedTask;
        });
    }

    private static string Count(int count) => count == 1 ? "1 item" : $"{count} items";

    private async Task PutAsync(TransferMode mode)
    {
        PaneViewModel pane = Pane;
        IReadOnlyList<EntryViewModel> targets = pane.GetTargets();

        if (targets.Count == 0)
        {
            return;
        }

        await clipboard.SetAsync([.. targets.Select(item => item.FullPath)], mode);

        // The marks did their job, as a yank ends visual mode in vim.
        pane.EndVisual(keepMarks: true);
        pane.ClearMarks();
        status.Info(targets.Count == 1
            ? $"{(mode == TransferMode.Copy ? "Copied" : "Cut")} '{targets[0].Name}'"
            : $"{(mode == TransferMode.Copy ? "Copied" : "Cut")} {Count(targets.Count)}");
    }

    private async Task PasteAsync(string? targetDirectory)
    {
        PaneViewModel pane = Pane;
        string target = targetDirectory ?? pane.CurrentPath;

        if (target.Length == 0)
        {
            return;
        }

        (IReadOnlyList<string> paths, TransferMode mode) = await clipboard.GetForPasteAsync();

        if (paths.Count == 0)
        {
            status.Info("Nothing to paste");
            return;
        }

        string title = $"{(mode == TransferMode.Copy ? "Copying" : "Moving")} {Count(paths.Count)}";
        TransferResult? result = await transfers.RunAsync(new TransferRequest(paths, target, mode), title);

        if (result is null)
        {
            return;
        }

        // What was cut now lives here; pasting it again would be a move of nothing.
        if ((mode == TransferMode.Move) && (result.Created.Count > 0))
        {
            clipboard.Clear();
        }

        await workspace.RefreshAfterTransferAsync(pane, target, paths, result);
    }

    private async Task CopyPathsAsync()
    {
        IReadOnlyList<EntryViewModel> targets = Pane.GetTargets();

        if (targets.Count == 0)
        {
            return;
        }

        await system.SetTextAsync(string.Join(Environment.NewLine, targets.Select(item => item.FullPath)));
        status.Info(targets.Count == 1 ? $"Copied the path of '{targets[0].Name}'" : $"Copied {targets.Count} paths");
    }
}

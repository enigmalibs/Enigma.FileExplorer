using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// Dropping files, apart from the drag itself: where a drop may land, what the modifiers held at the drop
/// ask for (KDE's rules), and running the copy or move through the transfer queue.
/// </summary>
/// <param name="transfers">Runs the copies and moves.</param>
/// <param name="workspace">The panes, refreshed once a drop is done.</param>
public sealed class FileDrops(ITransferQueue transfers, WorkspaceViewModel workspace)
{
    /// <summary>
    /// What the modifiers held at the drop ask for: Ctrl copies, Shift moves, and nothing (or both) asks.
    /// </summary>
    /// <param name="control">Whether Ctrl is held.</param>
    /// <param name="shift">Whether Shift is held.</param>
    /// <returns><see cref="DropAction.Copy"/>, <see cref="DropAction.Move"/> or <see cref="DropAction.Ask"/>.</returns>
    public static DropAction ActionFor(bool control, bool shift) => (control, shift) switch
    {
        (true, false) => DropAction.Copy,
        (false, true) => DropAction.Move,
        _ => DropAction.Ask,
    };

    /// <summary>
    /// The directory a drop lands in, if it may land there at all.
    /// </summary>
    /// <param name="sources">What is dragged.</param>
    /// <param name="directory">The directory under the pointer; <see langword="null"/> where there is none
    /// (search results).</param>
    /// <returns>The directory, or <see langword="null"/> when nothing is dragged, there is no directory, or a
    /// folder would land in itself or in one of its own subfolders.</returns>
    public static string? TargetFor(IReadOnlyList<string> sources, string? directory)
    {
        ArgumentNullException.ThrowIfNull(sources);

        if ((sources.Count == 0) || string.IsNullOrEmpty(directory))
        {
            return null;
        }

        return sources.Any(source => PathUtilities.IsSameOrDescendant(directory, source)) ? null : directory;
    }

    /// <summary>
    /// What a drop does once its target is known: entries dropped where they already are stay there, unless
    /// Ctrl asks for a copy, so a drag that ends where it started does nothing and asks nothing.
    /// </summary>
    /// <param name="sources">What is dropped.</param>
    /// <param name="target">Where it lands (see <see cref="TargetFor"/>).</param>
    /// <param name="requested">What the modifiers ask for (see <see cref="ActionFor"/>).</param>
    /// <returns>The action.</returns>
    public static DropAction Decide(IReadOnlyList<string> sources, string target, DropAction requested)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(target);

        bool alreadyThere = sources.All(source => PathUtilities.GetParent(source) is { } parent && PathUtilities.AreSame(parent, target));
        return alreadyThere && (requested != DropAction.Copy) ? DropAction.None : requested;
    }

    /// <summary>
    /// Copies or moves the dropped entries, then refreshes the panes that show a changed directory.
    /// </summary>
    /// <param name="sources">What was dropped.</param>
    /// <param name="target">Where it landed.</param>
    /// <param name="mode">Copy or move.</param>
    /// <param name="into">The pane dropped into, whose cursor goes to the first new entry; <see langword="null"/>
    /// for a drop on a folder or a place.</param>
    /// <returns>A task that completes when the transfer has run and the panes are refreshed.</returns>
    public async Task DropAsync(IReadOnlyList<string> sources, string target, TransferMode mode, PaneViewModel? into)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(target);

        string count = sources.Count == 1 ? "1 item" : $"{sources.Count} items";
        TransferResult? result = await transfers.RunAsync(new TransferRequest(sources, target, mode), $"{(mode == TransferMode.Copy ? "Copying" : "Moving")} {count}");

        if (result is not null)
        {
            await workspace.RefreshAfterTransferAsync(into, target, sources, result);
        }
    }
}

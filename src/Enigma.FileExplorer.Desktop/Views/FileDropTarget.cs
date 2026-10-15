using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// The view side of a drop, shared by the panes and the places sidebar: reading the drag, the effect shown
/// while hovering and the one reported to the source, and the KDE-style menu that asks copy or move.
/// </summary>
internal static class FileDropTarget
{
    /// <summary>
    /// Works out what dropping here would do.
    /// </summary>
    /// <param name="e">The drag event.</param>
    /// <param name="directory">The directory under the pointer; <see langword="null"/> where there is none.</param>
    /// <returns>The plan, or <see langword="null"/> when a drop here would do nothing.</returns>
    public static DropPlan? Plan(DragEventArgs e, string? directory)
    {
        IReadOnlyList<string> sources = FileDragData.ReadPaths(e.DataTransfer);

        if (FileDrops.TargetFor(sources, directory) is not { } target)
        {
            return null;
        }

        DropAction requested = FileDrops.ActionFor(
            control: (e.KeyModifiers & KeyModifiers.Control) != 0,
            shift: (e.KeyModifiers & KeyModifiers.Shift) != 0);
        DropAction action = FileDrops.Decide(sources, target, requested);
        return action == DropAction.None ? null : new DropPlan(sources, target, action);
    }

    /// <summary>
    /// The effect the cursor shows while hovering: a move when Shift asks for one, a copy otherwise, within
    /// what the source allows.
    /// </summary>
    /// <param name="plan">What a drop would do; <see langword="null"/> when nothing.</param>
    /// <param name="allowed">The effects the source allows.</param>
    /// <returns>The effect.</returns>
    public static DragDropEffects HoverEffect(DropPlan? plan, DragDropEffects allowed)
    {
        if (plan is null)
        {
            return DragDropEffects.None;
        }

        DragDropEffects wanted = plan.Action == DropAction.Move ? DragDropEffects.Move : DragDropEffects.Copy;
        return (allowed & wanted) != 0 ? wanted : allowed & (DragDropEffects.Copy | DragDropEffects.Move);
    }

    /// <summary>
    /// The effect reported to the source when the drop is accepted: always a copy. A source such as a file
    /// manager deletes its originals after a reported move; a move chosen here is done by the explorer.
    /// </summary>
    /// <param name="plan">What the drop does; <see langword="null"/> when nothing.</param>
    /// <param name="allowed">The effects the source allows.</param>
    /// <returns><see cref="DragDropEffects.Copy"/> or <see cref="DragDropEffects.None"/>.</returns>
    public static DragDropEffects DropEffect(DropPlan? plan, DragDropEffects allowed)
        => (plan is not null) && ((allowed & DragDropEffects.Copy) != 0) ? DragDropEffects.Copy : DragDropEffects.None;

    /// <summary>
    /// Builds the drop menu: Copy here, Move here, Cancel.
    /// </summary>
    /// <returns>The menu, closed, and the choice made in it: <see langword="null"/> when cancelled.</returns>
    public static (ContextMenu Menu, Task<TransferMode?> Choice) BuildMenu()
    {
        TaskCompletionSource<TransferMode?> choice = new();

        // The keys say what the modifiers do at the drop, as KDE's menu does.
        MenuEntry copy = new() { Title = "Copy here", Gesture = "Ctrl" };
        MenuEntry move = new() { Title = "Move here", Gesture = "Shift" };
        MenuEntry cancel = new() { Title = "Cancel", Gesture = "Esc" };

        ContextMenu menu = ContextMenuFactory.Build([copy, move, MenuEntry.Separator, cancel], entry =>
            choice.TrySetResult(ReferenceEquals(entry, copy) ? TransferMode.Copy : ReferenceEquals(entry, move) ? TransferMode.Move : null));

        // Closing by any other way (Esc, a click elsewhere) is a cancel; a choice made first wins.
        menu.Closed += (_, _) => choice.TrySetResult(null);
        return (menu, choice.Task);
    }

    /// <summary>
    /// Finishes a drop once the drag is over: asks when needed, then copies or moves.
    /// </summary>
    /// <param name="drops">Runs the transfer.</param>
    /// <param name="plan">What the drop does.</param>
    /// <param name="ask">Opens the drop menu and returns the choice.</param>
    /// <param name="into">The pane dropped into, if the drop landed on its own directory.</param>
    /// <returns>A task that completes when the drop is done or cancelled.</returns>
    public static async Task CompleteAsync(FileDrops drops, DropPlan plan, Func<Task<TransferMode?>> ask, PaneViewModel? into)
    {
        TransferMode? mode = plan.Action switch
        {
            DropAction.Copy => TransferMode.Copy,
            DropAction.Move => TransferMode.Move,
            _ => await ask(),
        };

        if (mode is { } chosen)
        {
            await drops.DropAsync(plan.Sources, plan.Target, chosen, into);
        }
    }
}

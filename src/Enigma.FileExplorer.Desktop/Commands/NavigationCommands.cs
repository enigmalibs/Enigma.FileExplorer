using System;
using System.Globalization;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// Moving the cursor, navigating, and marking, on the active pane.
/// </summary>
/// <param name="workspace">The panes.</param>
/// <param name="places">The places sidebar, for <c>g1</c>…<c>g9</c>.</param>
/// <param name="folders">The user's well-known folders, for <c>gH</c>.</param>
/// <param name="status">The status line.</param>
public sealed class NavigationCommands(
    WorkspaceViewModel workspace,
    PlacesViewModel places,
    IKnownFolders folders,
    IStatusMessages status) : ICommandModule
{
    private PaneViewModel Pane => workspace.ActivePane;

    /// <inheritdoc />
    public void Register(ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        // j and k move a row: one entry in the details view, a row of tiles in the icon views.
        registry.Register(CommandIds.CursorDown, invocation => Run(() => Pane.MoveCursor(Pane.RowStep * invocation.CountOrOne)));
        registry.Register(CommandIds.CursorUp, invocation => Run(() => Pane.MoveCursor(-Pane.RowStep * invocation.CountOrOne)));
        registry.Register(CommandIds.CursorFirst, invocation => Run(() => Pane.MoveCursorTo((invocation.Count ?? 1) - 1)));
        registry.Register(CommandIds.CursorLast, invocation => Run(() => Pane.MoveCursorTo(invocation.Count is { } n ? n - 1 : Pane.Items.Count - 1)));
        registry.Register(CommandIds.CursorHalfPageDown, invocation => Run(() => Pane.MoveCursor(Math.Max(1, Pane.PageSize / 2) * invocation.CountOrOne)));
        registry.Register(CommandIds.CursorHalfPageUp, invocation => Run(() => Pane.MoveCursor(-Math.Max(1, Pane.PageSize / 2) * invocation.CountOrOne)));
        registry.Register(CommandIds.CursorPageDown, invocation => Run(() => Pane.MoveCursor(Pane.PageSize * invocation.CountOrOne)));
        registry.Register(CommandIds.CursorPageUp, invocation => Run(() => Pane.MoveCursor(-Pane.PageSize * invocation.CountOrOne)));

        // In the details view the arrows move through the tree like h and l; in a grid of tiles they
        // move from tile to tile.
        registry.Register(CommandIds.CursorLeft, invocation => Pane.IsListLayout ? Pane.GoParentAsync() : Run(() => Pane.MoveCursor(-invocation.CountOrOne)));
        registry.Register(CommandIds.CursorRight, invocation => Pane.IsListLayout ? Pane.OpenCursorAsync() : Run(() => Pane.MoveCursor(invocation.CountOrOne)));

        registry.Register(CommandIds.Open, _ => Pane.OpenCursorAsync());
        registry.Register(CommandIds.Parent, _ => Pane.GoParentAsync());
        registry.Register(CommandIds.Back, _ => Pane.GoBackAsync());
        registry.Register(CommandIds.Forward, _ => Pane.GoForwardAsync());
        registry.Register(CommandIds.Home, _ => Pane.NavigateAsync(folders.Home));
        registry.Register(CommandIds.Root, _ => GoToRootAsync());
        registry.Register(CommandIds.Place, GoToPlaceAsync);
        registry.Register(CommandIds.Refresh, _ => Pane.RefreshAsync());
        registry.Register(CommandIds.EditPath, _ => Run(Pane.BeginEditPath));

        registry.Register(CommandIds.ToggleMark, invocation => Run(() => ToggleMarks(invocation.CountOrOne)));
        registry.Register(CommandIds.VisualMode, _ => Run(ToggleVisual));
        registry.Register(CommandIds.MarkAll, _ => Run(Pane.MarkAll));
        registry.Register(CommandIds.Escape, _ => EscapeAsync());
    }

    private static Task Run(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    private Task GoToRootAsync()
        => Pane.CurrentPath.Length == 0 ? Task.CompletedTask : Pane.NavigateAsync(PathUtilities.GetVolumeRoot(Pane.CurrentPath));

    private Task GoToPlaceAsync(CommandInvocation invocation)
    {
        if (!int.TryParse(invocation.Argument, NumberStyles.None, CultureInfo.InvariantCulture, out int number)
            || (number < 1) || (number > places.All.Count))
        {
            status.Error($"No place {invocation.Argument}");
            return Task.CompletedTask;
        }

        return Pane.NavigateAsync(places.All[number - 1].Path);
    }

    private void ToggleMarks(int count)
    {
        // m marks and moves on, so a run of presses marks a run of entries, as Space does in ranger.
        for (int i = 0; (i < count) && (Pane.CursorItem is { } item); i++)
        {
            Pane.ToggleMark(item);
            Pane.MoveCursor(1);
        }
    }

    private void ToggleVisual()
    {
        if (Pane.IsVisualMode)
        {
            Pane.EndVisual(keepMarks: true);
        }
        else
        {
            Pane.StartVisual();
        }
    }

    private async Task EscapeAsync()
    {
        status.Clear();

        // One thing at a time, most transient first: a running search, visual mode, the quick-jump highlight,
        // the marks, and only then the search results themselves.
        if (Pane.CancelSearch())
        {
            return;
        }

        if (Pane.IsVisualMode)
        {
            Pane.EndVisual(keepMarks: false);
        }
        else if (Pane.HighlightPattern is not null)
        {
            Pane.HighlightPattern = null;
        }
        else if (Pane.MarkedCount > 0)
        {
            Pane.ClearMarks();
        }
        else if (Pane.IsSearchResults)
        {
            await Pane.GoParentAsync();
        }
    }
}

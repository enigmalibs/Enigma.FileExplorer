using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// View modes, sorting and hidden files: <c>zd</c>/<c>zm</c>/<c>zl</c>, <c>on</c>/<c>om</c>/<c>os</c>/<c>ot</c>/<c>or</c>,
/// <c>zh</c>, <c>:view</c>, <c>:sort</c> and <c>:set</c> (hidden files, trash confirmation).
/// </summary>
/// <param name="workspace">The panes; view mode and sort apply to the active one.</param>
/// <param name="options">The options every pane shares (hidden files, trash confirmation).</param>
/// <param name="status">The status line.</param>
public sealed class ViewCommands(WorkspaceViewModel workspace, ViewOptions options, IStatusMessages status) : ICommandModule
{
    private static readonly Dictionary<string, ViewMode> ViewModes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["details"] = ViewMode.Details,
        ["medium"] = ViewMode.MediumIcons,
        ["large"] = ViewMode.LargeIcons,
    };

    private static readonly Dictionary<string, SortKey> SortKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = SortKey.Name,
        ["modified"] = SortKey.Modified,
        ["size"] = SortKey.Size,
        ["type"] = SortKey.Type,
    };

    private PaneViewModel Pane => workspace.ActivePane;

    /// <inheritdoc />
    public void Register(ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(CommandIds.ViewDetails, _ => Run(() => Pane.ViewMode = ViewMode.Details));
        registry.Register(CommandIds.ViewMediumIcons, _ => Run(() => Pane.ViewMode = ViewMode.MediumIcons));
        registry.Register(CommandIds.ViewLargeIcons, _ => Run(() => Pane.ViewMode = ViewMode.LargeIcons));
        registry.Register(CommandIds.View, invocation => Run(() => SetView(invocation.Argument)));

        registry.Register(CommandIds.SortName, _ => Run(() => Pane.Sort = EntrySort.For(SortKey.Name)));
        registry.Register(CommandIds.SortModified, _ => Run(() => Pane.Sort = EntrySort.For(SortKey.Modified)));
        registry.Register(CommandIds.SortSize, _ => Run(() => Pane.Sort = EntrySort.For(SortKey.Size)));
        registry.Register(CommandIds.SortType, _ => Run(() => Pane.Sort = EntrySort.For(SortKey.Type)));
        registry.Register(CommandIds.SortReverse, _ => Run(() => Pane.Sort = Pane.Sort.Reversed()));
        registry.Register(CommandIds.Sort, invocation => Run(() => SetSort(invocation.Argument, invocation.Bang)));

        registry.Register(CommandIds.ToggleHidden, _ => Run(() => SetHidden(!options.ShowHidden)));
        registry.Register(CommandIds.Set, invocation => Run(() => SetOption(invocation.Argument)));
    }

    private static Task Run(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    private static T? Lookup<T>(Dictionary<string, T> values, string? typed)
        where T : struct
    {
        if (string.IsNullOrWhiteSpace(typed))
        {
            return null;
        }

        // Unambiguous prefixes are enough, as for command names: ":view l", ":sort mod".
        string word = typed.Trim();
        List<KeyValuePair<string, T>> matches = [.. values.Where(pair => pair.Key.StartsWith(word, StringComparison.OrdinalIgnoreCase))];
        return matches.Count == 1 ? matches[0].Value : null;
    }

    private void SetView(string? argument)
    {
        if (Lookup(ViewModes, argument) is { } mode)
        {
            Pane.ViewMode = mode;
        }
        else
        {
            status.Error($"Unknown view: {argument} (details, medium, large)");
        }
    }

    private void SetSort(string? argument, bool reverse)
    {
        if (Lookup(SortKeys, argument) is not { } key)
        {
            status.Error($"Unknown sort: {argument} (name, modified, size, type)");
            return;
        }

        EntrySort sort = EntrySort.For(key);
        Pane.Sort = reverse ? sort.Reversed() : sort;
    }

    private void SetOption(string? argument)
    {
        switch (argument?.Trim().ToLowerInvariant())
        {
            case "hidden":
                SetHidden(true);
                break;

            case "nohidden":
                SetHidden(false);
                break;

            case "hidden!":
            case "invhidden":
                SetHidden(!options.ShowHidden);
                break;

            case "confirmtrash":
                SetConfirmTrash(true);
                break;

            case "noconfirmtrash":
                SetConfirmTrash(false);
                break;

            case "confirmtrash!":
            case "invconfirmtrash":
                SetConfirmTrash(!options.ConfirmTrash);
                break;

            default:
                status.Error($"Unknown option: {argument}");
                break;
        }
    }

    private void SetHidden(bool show)
    {
        options.ShowHidden = show;
        status.Info(show ? "Hidden files shown" : "Hidden files hidden");
    }

    private void SetConfirmTrash(bool confirm)
    {
        options.ConfirmTrash = confirm;
        status.Info(confirm ? "Moving to the trash asks first" : "Moving to the trash no longer asks");
    }
}

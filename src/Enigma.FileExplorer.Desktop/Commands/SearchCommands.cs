using System;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Search;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// Searching below the active pane's directory: names (<c>f</c>, Ctrl+F, <c>:find</c>) and contents (<c>F</c>,
/// Ctrl+Shift+F, <c>:grep</c>), and revealing a hit in its folder (<c>gf</c>).
/// </summary>
/// <param name="workspace">The panes; searches run in the active one.</param>
/// <param name="prompt">Asks for the pattern when none was given.</param>
/// <param name="status">The status line.</param>
public sealed class SearchCommands(WorkspaceViewModel workspace, IPromptService prompt, IStatusMessages status) : ICommandModule
{
    /// <inheritdoc />
    public void Register(ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(CommandIds.FindName, invocation => SearchAsync(SearchKind.Name, invocation.Argument, "Find name: "));
        registry.Register(CommandIds.FindContent, invocation => SearchAsync(SearchKind.Content, invocation.Argument, "Find in files: "));
        registry.Register(CommandIds.Reveal, _ => RevealAsync());
    }

    private async Task SearchAsync(SearchKind kind, string? argument, string label)
    {
        PaneViewModel pane = workspace.ActivePane;
        string? pattern = argument ?? await prompt.PromptAsync(label);

        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        await pane.SearchAsync(kind, pattern.Trim());
    }

    private Task RevealAsync()
    {
        PaneViewModel pane = workspace.ActivePane;

        if (!pane.IsSearchResults)
        {
            status.Info("gf shows a search result in its folder");
            return Task.CompletedTask;
        }

        return pane.CursorItem is { } item ? pane.RevealAsync(item) : Task.CompletedTask;
    }
}

using System;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// The context menu from the keyboard: the Menu key and Shift+F10 open the active pane's menu at its cursor.
/// </summary>
/// <param name="workspace">The panes; the active one shows the menu.</param>
public sealed class MenuCommands(WorkspaceViewModel workspace) : ICommandModule
{
    /// <inheritdoc />
    public void Register(ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(CommandIds.ContextMenu, _ =>
        {
            workspace.ActivePane.RequestContextMenu();
            return Task.CompletedTask;
        });
    }
}

using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// The command line, quick-jump and its highlight (<c>:noh</c>), <c>:cd</c>, the help and <c>:about</c>.
/// </summary>
/// <param name="commandLine">The command line.</param>
/// <param name="workspace">The panes.</param>
/// <param name="folders">What <c>~</c> means.</param>
/// <param name="help">Shows the key-binding help.</param>
/// <param name="about">Shows the About dialog.</param>
/// <param name="status">The status line.</param>
public sealed class CommandLineCommands(
    CommandLineViewModel commandLine,
    WorkspaceViewModel workspace,
    IKnownFolders folders,
    IHelpDialogService help,
    IAboutDialogService about,
    IStatusMessages status) : ICommandModule
{
    /// <inheritdoc />
    public void Register(ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(CommandIds.CommandLine, _ => Run(() => commandLine.OpenCommand()));
        registry.Register(CommandIds.JumpStart, _ => Run(commandLine.OpenJump));
        registry.Register(CommandIds.JumpNext, _ => Run(() => commandLine.RepeatJump(forward: true)));
        registry.Register(CommandIds.JumpPrevious, _ => Run(() => commandLine.RepeatJump(forward: false)));
        registry.Register(CommandIds.ClearHighlight, _ => Run(() => workspace.ActivePane.HighlightPattern = null));
        registry.Register(CommandIds.Help, _ => help.ShowAsync());
        registry.Register(CommandIds.About, _ => about.ShowAsync());
        registry.Register(CommandIds.ChangeDirectory, ChangeDirectoryAsync);
    }

    private static Task Run(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    private Task ChangeDirectoryAsync(CommandInvocation invocation)
    {
        PaneViewModel pane = workspace.ActivePane;
        string from = pane.CurrentPath.Length > 0 ? pane.CurrentPath : folders.Home;
        string target;

        try
        {
            // ":cd" alone goes home, as in vim on Unix.
            target = PathUtilities.Resolve(invocation.Argument ?? "~", from, folders.Home);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or SecurityException)
        {
            status.Error($"Invalid path: {invocation.Argument}");
            return Task.CompletedTask;
        }

        return pane.NavigateAsync(target);
    }
}

using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// vim's window commands on split groups: the leader keys (<c>&lt;Space&gt;v</c>, <c>&lt;Space&gt;c</c>…),
/// <c>gh</c>/<c>gj</c>/<c>gk</c>/<c>gl</c> to move between panes, <c>:split</c>, <c>:vsplit</c>,
/// <c>:close</c>, <c>:only</c>, <c>:q</c> and <c>:qa</c>.
/// </summary>
/// <param name="workspace">The split groups.</param>
/// <param name="folders">What <c>~</c> means in a split's path.</param>
/// <param name="shutdown">Ends the application.</param>
/// <param name="status">The status line.</param>
public sealed class WindowCommands(
    WorkspaceViewModel workspace,
    IKnownFolders folders,
    IAppShutdown shutdown,
    IStatusMessages status) : ICommandModule
{
    /// <summary>
    /// How much one resize key moves a split, as a fraction of its space.
    /// </summary>
    public const double ResizeStep = 0.05;

    /// <inheritdoc />
    public void Register(ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(CommandIds.SplitVertical, invocation => SplitAsync(SplitAxis.Columns, invocation.Argument));
        registry.Register(CommandIds.SplitHorizontal, invocation => SplitAsync(SplitAxis.Rows, invocation.Argument));
        registry.Register(CommandIds.FocusLeft, _ => Run(() => workspace.Focus(FocusDirection.Left)));
        registry.Register(CommandIds.FocusDown, _ => Run(() => workspace.Focus(FocusDirection.Down)));
        registry.Register(CommandIds.FocusUp, _ => Run(() => workspace.Focus(FocusDirection.Up)));
        registry.Register(CommandIds.FocusRight, _ => Run(() => workspace.Focus(FocusDirection.Right)));
        registry.Register(CommandIds.FocusNext, invocation => Run(() => Repeat(invocation, workspace.FocusNext)));
        registry.Register(CommandIds.FocusPrevious, invocation => Run(() => Repeat(invocation, workspace.FocusPrevious)));
        registry.Register(CommandIds.ClosePane, _ => Run(ClosePane));
        registry.Register(CommandIds.OnlyPane, _ => Run(workspace.Only));
        registry.Register(CommandIds.Equalize, _ => Run(workspace.Equalize));
        registry.Register(CommandIds.Taller, invocation => Run(() => workspace.Resize(SplitAxis.Rows, ResizeStep * invocation.CountOrOne)));
        registry.Register(CommandIds.Shorter, invocation => Run(() => workspace.Resize(SplitAxis.Rows, -ResizeStep * invocation.CountOrOne)));
        registry.Register(CommandIds.Wider, invocation => Run(() => workspace.Resize(SplitAxis.Columns, ResizeStep * invocation.CountOrOne)));
        registry.Register(CommandIds.Narrower, invocation => Run(() => workspace.Resize(SplitAxis.Columns, -ResizeStep * invocation.CountOrOne)));
        registry.Register(CommandIds.Quit, _ => Run(Quit));
        registry.Register(CommandIds.QuitAll, _ => Run(shutdown.Shutdown));
    }

    private static Task Run(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    private static Task Run(Func<bool> action)
    {
        action();
        return Task.CompletedTask;
    }

    private static void Repeat(CommandInvocation invocation, Action action)
    {
        for (int i = 0; i < invocation.CountOrOne; i++)
        {
            action();
        }
    }

    private async Task SplitAsync(SplitAxis axis, string? argument)
    {
        string? path = null;

        if (argument is not null)
        {
            PaneViewModel pane = workspace.ActivePane;

            try
            {
                path = PathUtilities.Resolve(argument, pane.CurrentPath.Length > 0 ? pane.CurrentPath : folders.Home, folders.Home);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or SecurityException)
            {
                status.Error($"Invalid path: {argument}");
                return;
            }
        }

        await workspace.SplitAsync(axis, path);
    }

    private void ClosePane()
    {
        if (!workspace.Close())
        {
            status.Error("Cannot close the last pane");
        }
    }

    private void Quit()
    {
        // vim's :q: close the window, and with the last one, quit.
        if (!workspace.Close())
        {
            shutdown.Shutdown();
        }
    }
}

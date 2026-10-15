using System;
using System.Threading.Tasks;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// Where command modules register their handlers.
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// Registers the handler of a command.
    /// </summary>
    /// <param name="commandId">The command.</param>
    /// <param name="handler">What running it does.</param>
    /// <exception cref="InvalidOperationException">The command already has a handler.</exception>
    void Register(string commandId, Func<CommandInvocation, Task> handler);
}

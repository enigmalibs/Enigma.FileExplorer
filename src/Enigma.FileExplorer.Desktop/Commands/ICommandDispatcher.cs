using System.Threading.Tasks;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// Runs commands by id, whoever asks: the keyboard, the command line, a menu or a toolbar button.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Whether a command has a handler.
    /// </summary>
    /// <param name="commandId">The command.</param>
    /// <returns><see langword="true"/> when it can be run.</returns>
    bool IsAvailable(string commandId);

    /// <summary>
    /// Runs a command. A command without a handler is reported on the status line, never thrown.
    /// </summary>
    /// <param name="invocation">The command and its count and argument.</param>
    /// <returns><see langword="true"/> when a handler ran.</returns>
    Task<bool> ExecuteAsync(CommandInvocation invocation);
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Desktop.Services;
using Microsoft.Extensions.Logging;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// The command dispatcher: one handler per command id, contributed by the registered modules.
/// </summary>
public sealed class CommandDispatcher : ICommandDispatcher, ICommandRegistry
{
    private readonly Dictionary<string, Func<CommandInvocation, Task>> _handlers = new(StringComparer.Ordinal);
    private readonly CommandCatalog _catalog;
    private readonly IStatusMessages _status;
    private readonly ILogger<CommandDispatcher> _logger;

    /// <summary>
    /// Builds the dispatcher and lets every module register its handlers.
    /// </summary>
    /// <param name="catalog">The command catalog, for titles.</param>
    /// <param name="status">The status line, where unavailable commands are reported.</param>
    /// <param name="modules">The command modules.</param>
    /// <param name="logger">Where handler failures are recorded.</param>
    public CommandDispatcher(CommandCatalog catalog, IStatusMessages status, IEnumerable<ICommandModule> modules, ILogger<CommandDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(logger);

        _catalog = catalog;
        _status = status;
        _logger = logger;

        foreach (ICommandModule module in modules)
        {
            module.Register(this);
        }
    }

    /// <inheritdoc />
    public void Register(string commandId, Func<CommandInvocation, Task> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(commandId);
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.TryAdd(commandId, handler))
        {
            throw new InvalidOperationException($"The command '{commandId}' already has a handler.");
        }
    }

    /// <inheritdoc />
    public bool IsAvailable(string commandId) => _handlers.ContainsKey(commandId);

    /// <inheritdoc />
    public async Task<bool> ExecuteAsync(CommandInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        if (!_handlers.TryGetValue(invocation.CommandId, out Func<CommandInvocation, Task>? handler))
        {
            string title = _catalog.Find(invocation.CommandId)?.Title ?? invocation.CommandId;
            _status.Error($"Not available yet: {title}");
            return false;
        }

        try
        {
            await handler(invocation);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A handler that throws is a bug, but a bug in one command must not take the explorer down:
            // keys arrive through async-void event handlers, where an escaping exception would.
            _logger.LogError(exception, "The command {CommandId} failed", invocation.CommandId);
            _status.Error($"{_catalog.Find(invocation.CommandId)?.Title ?? invocation.CommandId} failed: {exception.Message}");
            return true;
        }
    }
}

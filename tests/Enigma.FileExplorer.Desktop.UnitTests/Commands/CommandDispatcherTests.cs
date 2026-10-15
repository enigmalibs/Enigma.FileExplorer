using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Desktop.Commands;
using Enigma.FileExplorer.Desktop.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests.Commands;

public sealed class CommandDispatcherTests
{
    private readonly StatusMessages _status = new();

    [Fact]
    public async Task ExecuteAsync_RunsTheRegisteredHandlerWithTheInvocation()
    {
        List<CommandInvocation> seen = [];
        CommandDispatcher dispatcher = Create(new Module(registry => registry.Register(CommandIds.CursorDown, invocation =>
        {
            seen.Add(invocation);
            return Task.CompletedTask;
        })));

        bool ran = await dispatcher.ExecuteAsync(new CommandInvocation(CommandIds.CursorDown, 3));

        Assert.True(ran);
        Assert.Equal(3, Assert.Single(seen).Count);
        Assert.True(dispatcher.IsAvailable(CommandIds.CursorDown));
    }

    [Fact]
    public async Task ExecuteAsync_AnUnwiredCommand_IsReportedNotThrown()
    {
        CommandDispatcher dispatcher = Create();

        bool ran = await dispatcher.ExecuteAsync(new CommandInvocation(CommandIds.Copy));

        Assert.False(ran);
        Assert.Equal("Not available yet: Copy", _status.Current?.Text);
        Assert.True(_status.Current?.IsError);
    }

    [Fact]
    public async Task ExecuteAsync_AFailingHandler_IsReportedNotThrown()
    {
        CommandDispatcher dispatcher = Create(new Module(registry =>
            registry.Register(CommandIds.Refresh, _ => throw new InvalidOperationException("boom"))));

        await dispatcher.ExecuteAsync(new CommandInvocation(CommandIds.Refresh));

        Assert.Equal("Refresh failed: boom", _status.Current?.Text);
    }

    [Fact]
    public void Register_Twice_Throws()
    {
        Module twice = new(registry =>
        {
            registry.Register(CommandIds.Refresh, _ => Task.CompletedTask);
            registry.Register(CommandIds.Refresh, _ => Task.CompletedTask);
        });

        Assert.Throws<InvalidOperationException>(() => Create(twice));
    }

    [Fact]
    public void CountOrOne_DefaultsToOne()
    {
        Assert.Equal(1, new CommandInvocation(CommandIds.CursorDown).CountOrOne);
        Assert.Equal(4, new CommandInvocation(CommandIds.CursorDown, 4).CountOrOne);
    }

    private CommandDispatcher Create(params ICommandModule[] modules)
        => new(CommandCatalog.Default, _status, modules, NullLogger<CommandDispatcher>.Instance);

    private sealed class Module(Action<ICommandRegistry> register) : ICommandModule
    {
        public void Register(ICommandRegistry registry) => register(registry);
    }
}

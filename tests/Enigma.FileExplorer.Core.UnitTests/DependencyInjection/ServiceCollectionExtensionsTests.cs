using Enigma.FileExplorer.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFileExplorerCore_ReturnsTheSameCollection()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddFileExplorerCore();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddFileExplorerCore_BuildsAValidContainer()
    {
        ServiceCollection services = new();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddFileExplorerCore();

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        Assert.NotNull(provider);
    }
}

using System;
using Enigma.Avalonia.Desktop.Services;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Keymaps;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests;

/// <summary>
/// Guards the composition root. A missing registration or a captive dependency is invisible until
/// the feature that needs it is used, which is exactly the kind of failure a build should catch.
/// </summary>
public sealed class CompositionRootTests
{
    /// <summary>
    /// Builds the application's container.
    /// </summary>
    /// <param name="replace">Registers test doubles over the application's own services, when given.</param>
    internal static ServiceProvider BuildProvider(Action<IServiceCollection>? replace = null)
    {
        ServiceCollection services = new();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        App.ConfigureServices(services);

        // Never the user's own settings file: a leader key chosen there would change every key a test types.
        services.AddSingleton<ISettingsStore>(new FakeSettingsStore());
        replace?.Invoke(services);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            // Proves every registration can have a constructor selected, and that no singleton
            // depends on something shorter-lived than itself.
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    [Fact]
    public void Container_ValidatesOnBuild()
    {
        using ServiceProvider provider = BuildProvider();

        Assert.NotNull(provider);
    }

    [Theory]
    [InlineData(typeof(INavigationService))]
    [InlineData(typeof(IContentDialogService))]
    [InlineData(typeof(IOverlayService))]
    [InlineData(typeof(IInfoBarService))]
    [InlineData(typeof(IFileDialogService))]
    [InlineData(typeof(IFolderDialogService))]
    [InlineData(typeof(MainWindowViewModel))]
    public void Container_ResolvesService(Type serviceType)
    {
        using ServiceProvider provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService(serviceType));
    }

    [Fact]
    public void TheCatalog_UsesTheLeaderKeyOfTheSettings()
    {
        using ServiceProvider provider = BuildProvider(services =>
            services.AddSingleton<ISettingsStore>(new FakeSettingsStore { Stored = new AppSettings { LeaderKey = "," } }));

        CommandCatalog catalog = provider.GetRequiredService<CommandCatalog>();

        Assert.Equal(KeyStroke.Char(','), catalog.Leader);
        Assert.Equal("Split side by side (,v)", provider.GetRequiredService<MainWindowViewModel>().SplitColumnsTip);
    }

    [Fact]
    public void AnUnusableLeaderKey_FallsBackToSpace_AndSaysSo()
    {
        using ServiceProvider provider = BuildProvider(services =>
            services.AddSingleton<ISettingsStore>(new FakeSettingsStore { Stored = new AppSettings { LeaderKey = "j" } }));

        CommandCatalog catalog = provider.GetRequiredService<CommandCatalog>();

        Assert.Same(CommandCatalog.Default, catalog);
        StatusMessage? message = provider.GetRequiredService<IStatusMessages>().Current;
        Assert.NotNull(message);
        Assert.True(message.IsError);
        Assert.Contains("Using Space as the leader key", message.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureServices_RejectsNull()
        => Assert.Throws<ArgumentNullException>(() => App.ConfigureServices(null!));
}

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Enigma.Avalonia.Desktop.Services;
using Enigma.FileExplorer.Core.DependencyInjection;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Desktop.DependencyInjection;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Enigma.FileExplorer.Desktop;

/// <summary>
/// The Avalonia <see cref="Application"/> for Enigma.FileExplorer, and the composition root.
/// </summary>
/// <remarks>
/// <para>
/// The host is <em>started</em>, never run: Avalonia's classic desktop lifetime runs the
/// application, and the host only supplies configuration, logging and services.
/// </para>
/// <para>
/// Every <c>using</c> directive in this project sits at file scope, above the namespace declaration:
/// inside a namespace starting with <c>Enigma.</c>, an inline <c>Avalonia.Xxx</c> reference would bind
/// to the <c>Enigma.Avalonia</c> namespace shipped by Enigma.Avalonia.Desktop.
/// </para>
/// </remarks>
public partial class App : Application
{
    private IHost? _host;

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // First: a command or a posted callback that throws from here on is caught and shown, not the end
            // of the process. The dispatcher is safe to touch now that the platform is set up; the headless
            // tests never come this way.
            UnhandledExceptionHooks.InstallDispatcher();

            IHost host = BuildHost();
            host.Start();
            _host = host;

            IServiceProvider services = host.Services;
            MainWindow window = services.GetRequiredService<MainWindow>();

            RegisterHosts(services, window);

            // The safety net, as soon as the window has handed it its dialog host: anything caught before is
            // handed over and shown.
            IUnhandledErrorReporter errors = services.GetRequiredService<IUnhandledErrorReporter>();
            UnhandledExceptionHooks.Attach(errors);
            desktop.ShutdownRequested += (_, _) => errors.ShutdownStarted();
            MainWindowViewModel viewModel = services.GetRequiredService<MainWindowViewModel>();
            window.DataContext = viewModel;

            // The settings come first: the theme and the window's place must be right before it is shown.
            Session session = services.GetRequiredService<Session>();
            AppSettings settings = session.Load();
            window.RestorePlacement(settings.Window);
            session.TrackWindow(window.CapturePlacement);
            string? path = Session.DirectoryArgument(desktop.Args ?? [], Directory.Exists);

            desktop.MainWindow = window;

            // Posted, so the window is on screen before the first directory is read.
            Dispatcher.UIThread.Post(() => viewModel.StartAsync(path, settings.Pane).Forget(UnhandledErrorSource.Startup));
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Builds the application host.
    /// </summary>
    /// <returns>The built, unstarted host.</returns>
    internal static IHost BuildHost()
    {
        // A desktop application must not depend on its working directory, so configuration is read
        // from beside the executable.
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.Logging.SetMinimumLevel(LogLevel.Information);

        ConfigureServices(builder.Services);

        return builder.Build();
    }

    /// <summary>
    /// Registers everything the application needs. Kept apart from <see cref="BuildHost"/> so a test can
    /// validate the whole container without an Avalonia platform behind it.
    /// </summary>
    /// <param name="services">The collection to register into.</param>
    internal static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFileExplorerCore();
        services.AddEnigmaDesktopServices();
        services.AddFileExplorerApp();
    }

    /// <summary>
    /// Hands the window's host controls and storage provider to the Enigma services, and its dialog host to
    /// the safety net. Must run before the window is shown: a service whose host is still unregistered
    /// throws the moment it is used.
    /// </summary>
    /// <param name="services">The application's services.</param>
    /// <param name="window">The main window.</param>
    internal static void RegisterHosts(IServiceProvider services, MainWindow window)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(window);

        services.GetRequiredService<IContentDialogService>().RegisterHost(window.HostDialog);
        services.GetRequiredService<IOverlayService>().RegisterHost(window.HostOverlay);
        services.GetRequiredService<IInfoBarService>().RegisterHost(window.HostInfoBar);
        services.GetRequiredService<IFileDialogService>().SetStorageProvider(window.StorageProvider);
        services.GetRequiredService<IFolderDialogService>().SetStorageProvider(window.StorageProvider);
        services.GetRequiredService<SystemClipboard>().Attach(window);
        services.GetRequiredService<UnhandledErrorReporter>().RegisterHost(window.HostDialog);
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        IHost? host = _host;

        if (host is null)
        {
            return;
        }

        _host = null;

        // Closing: no cancellation is worth a word now, and a late report must not log through the container
        // disposed below.
        host.Services.GetRequiredService<IUnhandledErrorReporter>().ShutdownStarted();
        UnhandledExceptionHooks.Detach();

        // Saved while the services are still alive; synchronous, as nothing runs after this.
        host.Services.GetRequiredService<Session>().Save();

        // The process is tearing down and no message pump remains, so blocking here is correct.
        host.StopAsync().GetAwaiter().GetResult();
        host.Dispose();
    }
}

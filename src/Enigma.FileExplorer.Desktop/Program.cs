using System;
using Avalonia;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop;

/// <summary>
/// Entry point of the Enigma.FileExplorer desktop application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Starts the Avalonia classic desktop lifetime.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <remarks>
    /// The process-wide unhandled-exception hooks come first, so a failure anywhere in the start is caught
    /// too.
    /// </remarks>
    [STAThread]
    public static void Main(string[] args)
    {
        UnhandledExceptionHooks.Install();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Builds the Avalonia application. Also used by the XAML previewer and by headless tests.
    /// </summary>
    /// <returns>The configured <see cref="AppBuilder"/>.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}

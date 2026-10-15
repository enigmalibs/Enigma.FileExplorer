using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Shuts the application down through Avalonia's desktop lifetime, which closes the window and lets
/// the usual exit path run.
/// </summary>
public sealed class AppShutdown : IAppShutdown
{
    /// <inheritdoc />
    public void Shutdown()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}

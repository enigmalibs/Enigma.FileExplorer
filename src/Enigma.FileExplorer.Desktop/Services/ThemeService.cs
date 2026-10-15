using System;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Enigma.FileExplorer.Core.Settings;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Applies the theme to the application. "System" is resolved to Dark or Light from the platform, and
/// followed as the platform changes, rather than left at <see cref="ThemeVariant.Default"/>: the Enigma
/// theme defines only Dark and Light, and a host whose theme cannot be resolved would paint nothing.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private IPlatformSettings? _platform;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public ThemePreference Preference { get; private set; } = ThemePreference.Dark;

    /// <inheritdoc />
    public void Apply(ThemePreference preference)
    {
        bool changed = preference != Preference;
        Preference = preference;
        Update();

        if (changed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Update()
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        if ((_platform is null) && (application.PlatformSettings is { } platform))
        {
            _platform = platform;
            platform.ColorValuesChanged += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                if (Preference == ThemePreference.System)
                {
                    Update();
                }
            });
        }

        application.RequestedThemeVariant = Preference switch
        {
            ThemePreference.Light => ThemeVariant.Light,
            ThemePreference.System when _platform?.GetColorValues().ThemeVariant == PlatformThemeVariant.Light => ThemeVariant.Light,
            _ => ThemeVariant.Dark,
        };
    }
}

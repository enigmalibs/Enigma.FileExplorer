using System;
using System.Collections.Generic;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A theme that only records what was applied.
/// </summary>
public sealed class FakeThemeService : IThemeService
{
    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public ThemePreference Preference { get; private set; } = ThemePreference.Dark;

    /// <summary>
    /// Every theme applied, in order.
    /// </summary>
    public List<ThemePreference> Applied { get; } = [];

    /// <inheritdoc />
    public void Apply(ThemePreference preference)
    {
        Applied.Add(preference);
        bool changed = preference != Preference;
        Preference = preference;

        if (changed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}

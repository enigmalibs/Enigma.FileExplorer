using System;
using Enigma.FileExplorer.Core.Settings;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// The colour theme of the running application.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// The theme chosen.
    /// </summary>
    ThemePreference Preference { get; }

    /// <summary>
    /// Raised after <see cref="Preference"/> changed.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Switches the theme, at once.
    /// </summary>
    /// <param name="preference">The theme.</param>
    void Apply(ThemePreference preference);
}

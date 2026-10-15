using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// <c>:theme dark|light|system</c>: switches the colour theme at once; the session remembers it.
/// </summary>
/// <param name="theme">The application's theme.</param>
/// <param name="status">The status line.</param>
public sealed class ThemeCommands(IThemeService theme, IStatusMessages status) : ICommandModule
{
    private static readonly Dictionary<string, ThemePreference> Themes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dark"] = ThemePreference.Dark,
        ["light"] = ThemePreference.Light,
        ["system"] = ThemePreference.System,
    };

    /// <inheritdoc />
    public void Register(ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(CommandIds.Theme, invocation =>
        {
            SetTheme(invocation.Argument);
            return Task.CompletedTask;
        });
    }

    private void SetTheme(string? argument)
    {
        string word = argument?.Trim() ?? string.Empty;

        // Unambiguous prefixes are enough, as for :view and :sort.
        List<ThemePreference> matches = [.. Themes.Where(pair => (word.Length > 0) && pair.Key.StartsWith(word, StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Value)];

        if (matches is not [ThemePreference preference])
        {
            status.Error($"Unknown theme: {word} (dark, light or system)");
            return;
        }

        theme.Apply(preference);
        status.Info($"Theme: {preference.ToString().ToLowerInvariant()}");
    }
}

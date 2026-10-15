using System;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Keymaps;
using Enigma.FileExplorer.Core.Settings;
using Microsoft.Extensions.Logging;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Builds the explorer's command catalog from the settings: the keymap starts the split-group commands with
/// the leader key the user chose.
/// </summary>
public static class CommandCatalogFactory
{
    /// <summary>
    /// Builds the catalog. A leader key that cannot be used never stops the explorer: the default one is used
    /// instead, and the user is told why on the status line.
    /// </summary>
    /// <param name="settings">The settings, for the leader key.</param>
    /// <param name="status">Where an unusable leader key is reported.</param>
    /// <param name="logger">Where an unusable leader key is recorded.</param>
    /// <returns>The catalog.</returns>
    public static CommandCatalog Create(AppSettings settings, IStatusMessages status, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(logger);

        if (CommandCatalog.TryCreate(settings.LeaderKey, out CommandCatalog catalog, out string? error))
        {
            return catalog;
        }

        string fallback = KeyNotation.Describe([CommandCatalog.DefaultLeader]);
        logger.LogWarning("{Error} Using {Fallback} as the leader key.", error, fallback);
        status.Error($"{error} Using {fallback} as the leader key.");
        return catalog;
    }
}

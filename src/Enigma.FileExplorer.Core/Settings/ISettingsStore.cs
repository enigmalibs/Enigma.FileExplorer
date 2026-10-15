namespace Enigma.FileExplorer.Core.Settings;

/// <summary>
/// Reads and writes the settings. Neither call throws: settings that cannot be read are the defaults, and
/// settings that cannot be written are reported and lost, since neither may stop the explorer.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Reads the settings.
    /// </summary>
    /// <returns>The settings; the defaults when there are none or they cannot be read.</returns>
    AppSettings Load();

    /// <summary>
    /// Writes the settings.
    /// </summary>
    /// <param name="settings">The settings.</param>
    void Save(AppSettings settings);
}

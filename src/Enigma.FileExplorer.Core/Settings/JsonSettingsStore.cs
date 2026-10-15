using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Enigma.FileExplorer.Core.Settings;

/// <summary>
/// The settings as one JSON file, written atomically. Reading is tolerant: a missing file or member gives
/// the defaults, unknown members are skipped, and a file that cannot be parsed is kept beside the original
/// as <c>.bad</c> before the defaults are used, so the next save never destroys it unseen.
/// </summary>
/// <param name="path">The settings file.</param>
/// <param name="logger">Where unreadable and unwritable settings are reported.</param>
public sealed class JsonSettingsStore(string path, ILogger<JsonSettingsStore> logger) : ISettingsStore
{
    /// <summary>
    /// The suffix of the copy kept of a file that could not be parsed.
    /// </summary>
    public const string BadSuffix = ".bad";

    /// <summary>
    /// The settings file.
    /// </summary>
    public string Path => path;

    /// <inheritdoc />
    public AppSettings Load()
    {
        string json;

        try
        {
            if (!File.Exists(path))
            {
                return new AppSettings();
            }

            json = File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not read the settings at {Path}; using the defaults", path);
            return new AppSettings();
        }

        try
        {
            return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.AppSettings) ?? new AppSettings();
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "The settings at {Path} cannot be parsed; keeping them as {Backup} and using the defaults", path, path + BadSuffix);
            KeepUnreadable();
            return new AppSettings();
        }
    }

    /// <inheritdoc />
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string json = JsonSerializer.Serialize(settings with { SchemaVersion = AppSettings.CurrentSchemaVersion }, SettingsJsonContext.Default.AppSettings);

        try
        {
            AtomicFile.WriteAllText(path, json);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Could not save the settings to {Path}", path);
        }
    }

    private void KeepUnreadable()
    {
        try
        {
            File.Copy(path, path + BadSuffix, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not keep a copy of the unreadable settings at {Path}", path);
        }
    }
}

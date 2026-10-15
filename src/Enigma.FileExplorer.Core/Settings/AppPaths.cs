using System;
using System.IO;

namespace Enigma.FileExplorer.Core.Settings;

/// <summary>
/// Where the explorer keeps its files for the current user.
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// The application's folder name.
    /// </summary>
    public const string ApplicationName = "Enigma.FileExplorer";

    /// <summary>
    /// The per-user settings directory: <c>%APPDATA%</c> on Windows, <c>$XDG_CONFIG_HOME</c> (or
    /// <c>~/.config</c>) on Linux, <c>~/Library/Application Support</c> on macOS.
    /// </summary>
    public static string SettingsDirectory
        => OperatingSystem.IsMacOS()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", ApplicationName)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ApplicationName);

    /// <summary>
    /// The settings file.
    /// </summary>
    public static string SettingsFile => Path.Combine(SettingsDirectory, "settings.json");
}

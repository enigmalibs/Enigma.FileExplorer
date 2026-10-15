namespace Enigma.FileExplorer.Core.Settings;

/// <summary>
/// The colour theme the user chose.
/// </summary>
public enum ThemePreference
{
    /// <summary>
    /// The dark theme, the default.
    /// </summary>
    Dark,

    /// <summary>
    /// The light theme.
    /// </summary>
    Light,

    /// <summary>
    /// Whichever the operating system uses, followed as it changes.
    /// </summary>
    System,
}

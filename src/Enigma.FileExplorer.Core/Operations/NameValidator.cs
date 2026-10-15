using System;
using System.Collections.Frozen;
using System.Linq;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// Checks a name typed by the user before it touches the disk, so a mistake is explained instead of
/// surfacing as an obscure I/O error — or worse, as a path that escapes the directory.
/// </summary>
public static class NameValidator
{
    /// <summary>
    /// The longest name most file systems accept.
    /// </summary>
    public const int MaximumLength = 255;

    private const string WindowsInvalidCharacters = "<>:\"|?*";

    private static readonly FrozenSet<string> WindowsReservedNames = new[]
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks a name against the current platform's rules.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>Why the name is invalid, or <see langword="null"/> when it is valid.</returns>
    public static string? Validate(string? name) => Validate(name, NamePlatforms.Current);

    /// <summary>
    /// Checks a name against a platform's rules.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="platform">Whose rules.</param>
    /// <returns>Why the name is invalid, or <see langword="null"/> when it is valid.</returns>
    public static string? Validate(string? name, NamePlatform platform)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "A name cannot be empty";
        }

        if (name is "." or "..")
        {
            return "'.' and '..' are not valid names";
        }

        if (name.Length > MaximumLength)
        {
            return $"A name cannot be longer than {MaximumLength} characters";
        }

        if (name.Contains('/') || ((platform == NamePlatform.Windows) && name.Contains('\\')))
        {
            return platform == NamePlatform.Windows
                ? "A name cannot contain '/' or '\\'"
                : "A name cannot contain '/'";
        }

        if (name.Contains('\0'))
        {
            return "A name cannot contain a NUL character";
        }

        return platform == NamePlatform.Windows ? ValidateWindows(name) : null;
    }

    private static string? ValidateWindows(string name)
    {
        if (name.FirstOrDefault(c => WindowsInvalidCharacters.Contains(c) || (c < 32)) is var invalid and not '\0')
        {
            return char.IsControl(invalid)
                ? "A name cannot contain control characters"
                : $"A name cannot contain any of {WindowsInvalidCharacters}";
        }

        if (name.EndsWith('.') || name.EndsWith(' '))
        {
            return "A name cannot end with a dot or a space";
        }

        // "CON" is reserved with any extension too: "con.txt" names the console.
        int dot = name.IndexOf('.', StringComparison.Ordinal);
        string stem = (dot < 0 ? name : name[..dot]).TrimEnd();

        return WindowsReservedNames.Contains(stem) ? $"'{stem}' is reserved by Windows" : null;
    }
}

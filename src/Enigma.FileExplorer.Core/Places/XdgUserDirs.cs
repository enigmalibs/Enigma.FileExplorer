using System;
using System.Collections.Generic;
using System.IO;

namespace Enigma.FileExplorer.Core.Places;

/// <summary>
/// Reads the freedesktop.org user directories file (<c>~/.config/user-dirs.dirs</c>).
/// </summary>
public static class XdgUserDirs
{
    /// <summary>
    /// The key of the downloads folder.
    /// </summary>
    public const string Download = "XDG_DOWNLOAD_DIR";

    /// <summary>
    /// Reads the user's file; an absent or unreadable file yields no entries.
    /// </summary>
    /// <param name="home">The home directory, which <c>$HOME</c> stands for.</param>
    /// <returns>The directories by key.</returns>
    public static IReadOnlyDictionary<string, string> Read(string home)
    {
        string? configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        string file = Path.Combine(string.IsNullOrEmpty(configHome) ? Path.Combine(home, ".config") : configHome, "user-dirs.dirs");

        try
        {
            return File.Exists(file) ? Parse(File.ReadAllText(file), home) : new Dictionary<string, string>();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Parses the file's content: <c>KEY="$HOME/Name"</c> or <c>KEY="/absolute"</c> lines, <c>#</c> comments.
    /// </summary>
    /// <param name="content">The file's content.</param>
    /// <param name="home">The home directory, which <c>$HOME</c> stands for.</param>
    /// <returns>The directories by key.</returns>
    public static IReadOnlyDictionary<string, string> Parse(string content, string home)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(home);

        Dictionary<string, string> directories = new(StringComparer.Ordinal);

        foreach (string raw in content.Split('\n'))
        {
            string line = raw.Trim();
            int equals = line.IndexOf('=', StringComparison.Ordinal);

            if (line.StartsWith('#') || (equals <= 0))
            {
                continue;
            }

            string key = line[..equals].Trim();
            string value = line[(equals + 1)..].Trim().Trim('"');

            if (value.StartsWith("$HOME", StringComparison.Ordinal))
            {
                value = home + value[5..];
            }

            // A relative value is not allowed by the specification; ignoring it beats guessing.
            if (Path.IsPathRooted(value))
            {
                directories[key] = value;
            }
        }

        return directories;
    }
}

using System;
using System.IO;

namespace Enigma.FileExplorer.Core.FileSystem;

/// <summary>
/// Path arithmetic shared by navigation, the command line and file operations.
/// </summary>
public static class PathUtilities
{
    /// <summary>
    /// How paths compare on this platform: case-insensitively on Windows and macOS, whose default file
    /// systems are, and ordinally elsewhere.
    /// </summary>
    public static StringComparison Comparison { get; } =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// Makes a path absolute and removes a trailing separator, keeping a root such as <c>C:\</c> or
    /// <c>/</c> intact.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The normalized path.</returns>
    public static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    /// <summary>
    /// Resolves what a user typed into an absolute, normalized path: <c>~</c> is the home directory,
    /// a relative path is relative to <paramref name="baseDirectory"/>, and on Windows a bare drive such
    /// as <c>D:</c> is that drive's root.
    /// </summary>
    /// <param name="input">What the user typed.</param>
    /// <param name="baseDirectory">The directory a relative path is relative to; must be absolute.</param>
    /// <param name="homeDirectory">The user's home directory.</param>
    /// <returns>The resolved path; <paramref name="baseDirectory"/> itself for blank input.</returns>
    public static string Resolve(string input, string baseDirectory, string homeDirectory)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(homeDirectory);

        string text = input.Trim();

        if (text.Length == 0)
        {
            return Normalize(baseDirectory);
        }

        if (text == "~")
        {
            text = homeDirectory;
        }
        else if (text.StartsWith("~/", StringComparison.Ordinal) || text.StartsWith("~\\", StringComparison.Ordinal))
        {
            text = Path.Combine(homeDirectory, text[2..]);
        }

        // "D:" alone means "the current directory of drive D" to Windows, which nobody typing it means.
        if (OperatingSystem.IsWindows() && (text.Length == 2) && (text[1] == ':') && char.IsAsciiLetter(text[0]))
        {
            text += Path.DirectorySeparatorChar;
        }

        // GetFullPath with a base resolves "..", "sub\dir" and the drive-relative "\dir" against the base,
        // never against the process's working directory.
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(text, Normalize(baseDirectory)));
    }

    /// <summary>
    /// The parent directory of a path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The parent, or <see langword="null"/> for a root.</returns>
    public static string? GetParent(string path) => Path.GetDirectoryName(Normalize(path));

    /// <summary>
    /// The root of the volume a path lives on: <c>C:\</c>, <c>\\server\share</c> or <c>/</c>.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The root.</returns>
    public static string GetVolumeRoot(string path)
    {
        string normalized = Normalize(path);
        string? root = Path.GetPathRoot(normalized);
        return string.IsNullOrEmpty(root) ? normalized : root;
    }

    /// <summary>
    /// Walks up from a path to the first directory that exists.
    /// </summary>
    /// <param name="path">Where to start.</param>
    /// <param name="directoryExists">Tells whether a directory exists; <see cref="Directory.Exists"/> in production.</param>
    /// <returns>The nearest existing directory, possibly <paramref name="path"/> itself; <see langword="null"/> when none exists.</returns>
    public static string? FindNearestExisting(string path, Func<string, bool> directoryExists)
    {
        ArgumentNullException.ThrowIfNull(directoryExists);

        string? current = Normalize(path);

        while (current is not null)
        {
            if (directoryExists(current))
            {
                return current;
            }

            current = Path.GetDirectoryName(current);
        }

        return null;
    }

    /// <summary>
    /// Whether two paths name the same location, comparing as the platform does.
    /// </summary>
    /// <param name="first">The first path.</param>
    /// <param name="second">The second path.</param>
    /// <returns><see langword="true"/> when they are the same.</returns>
    public static bool AreSame(string first, string second)
        => string.Equals(Normalize(first), Normalize(second), Comparison);

    /// <summary>
    /// Whether a path is <paramref name="ancestor"/> itself or lies anywhere beneath it.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <param name="ancestor">The candidate ancestor.</param>
    /// <returns><see langword="true"/> when <paramref name="path"/> is inside <paramref name="ancestor"/> or equal to it.</returns>
    public static bool IsSameOrDescendant(string path, string ancestor)
    {
        string normalizedPath = Normalize(path);
        string normalizedAncestor = Normalize(ancestor);

        if (string.Equals(normalizedPath, normalizedAncestor, Comparison))
        {
            return true;
        }

        string prefix = Path.EndsInDirectorySeparator(normalizedAncestor)
            ? normalizedAncestor
            : normalizedAncestor + Path.DirectorySeparatorChar;

        return normalizedPath.StartsWith(prefix, Comparison);
    }
}

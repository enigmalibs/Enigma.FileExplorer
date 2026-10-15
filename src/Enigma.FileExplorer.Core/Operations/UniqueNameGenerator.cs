using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// Finds a free name next to a taken one, the way Explorer and Dolphin do: <c>report (2).pdf</c>,
/// <c>report (3).pdf</c>, <c>photos (2)</c>.
/// </summary>
public static partial class UniqueNameGenerator
{
    /// <summary>
    /// The first free name in a directory, starting from <paramref name="name"/> itself.
    /// </summary>
    /// <param name="directory">The directory the name must be free in.</param>
    /// <param name="name">The wanted name.</param>
    /// <param name="isDirectory">Whether the name is for a directory, whose dots are not an extension.</param>
    /// <param name="isTaken">Tells whether a full path is taken; file or directory existence in production.</param>
    /// <returns>The free name (just the name, not the path).</returns>
    public static string FirstFree(string directory, string name, bool isDirectory, Func<string, bool> isTaken)
    {
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(isTaken);

        if (!isTaken(Path.Combine(directory, name)))
        {
            return name;
        }

        (string stem, string extension) = Split(name, isDirectory);
        int number = 2;

        // "report (2)" copied again becomes "report (3)", not "report (2) (2)".
        if (Numbered().Match(stem) is { Success: true } match)
        {
            stem = match.Groups["stem"].Value;
            number = int.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture) + 1;
        }

        while (true)
        {
            string candidate = $"{stem} ({number}){extension}";

            if (!isTaken(Path.Combine(directory, candidate)))
            {
                return candidate;
            }

            number++;
        }
    }

    /// <summary>
    /// Whether a path is taken by a file, a directory or a link (even a broken one).
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns><see langword="true"/> when something exists there.</returns>
    public static bool Exists(string path)
        => File.Exists(path) || Directory.Exists(path) || (new FileInfo(path).LinkTarget is not null);

    /// <summary>
    /// Splits a name into its stem and extension; a directory's name, or a dot-file's, has no extension.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="isDirectory">Whether it names a directory.</param>
    /// <returns>The stem and the extension (with its dot, or empty).</returns>
    public static (string Stem, string Extension) Split(string name, bool isDirectory)
    {
        ArgumentNullException.ThrowIfNull(name);

        int dot = isDirectory ? -1 : name.LastIndexOf('.');
        return dot > 0 ? (name[..dot], name[dot..]) : (name, string.Empty);
    }

    [GeneratedRegex(@"^(?<stem>.*) \((?<number>[0-9]{1,9})\)$", RegexOptions.CultureInvariant)]
    private static partial Regex Numbered();
}

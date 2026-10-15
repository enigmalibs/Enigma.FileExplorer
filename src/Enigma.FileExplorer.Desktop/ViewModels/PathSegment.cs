using System.Collections.Generic;
using System.IO;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// One clickable segment of the path bar's breadcrumb.
/// </summary>
/// <param name="Name">What the segment shows: a directory name, or the root (<c>C:</c>, <c>/</c>).</param>
/// <param name="FullPath">Where clicking it navigates.</param>
public sealed record PathSegment(string Name, string FullPath)
{
    /// <summary>
    /// Splits a normalized absolute path into its segments, root first.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The segments; empty for an empty path.</returns>
    public static IReadOnlyList<PathSegment> Split(string path)
    {
        List<PathSegment> segments = [];
        string? current = string.IsNullOrEmpty(path) ? null : path;

        while (current is not null)
        {
            string? parent = Path.GetDirectoryName(current);
            string name = parent is null ? RootName(current) : Path.GetFileName(current);
            segments.Add(new PathSegment(name, current));
            current = parent;
        }

        segments.Reverse();
        return segments;
    }

    private static string RootName(string root)
    {
        // "C:\" reads better as "C:", but "/" must stay "/" or the root segment would be blank.
        string trimmed = Path.TrimEndingDirectorySeparator(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return trimmed.Length == 0 ? root : trimmed;
    }
}

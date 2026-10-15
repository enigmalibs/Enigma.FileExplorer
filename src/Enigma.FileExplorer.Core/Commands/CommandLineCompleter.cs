using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enigma.FileExplorer.Core.FileSystem;

namespace Enigma.FileExplorer.Core.Commands;

/// <summary>
/// Completes command lines: command names, the fixed values of an argument (<c>:view d</c> →
/// <c>details</c>) and directory paths (<c>:cd ~/Doc</c> → <c>~/Documents/</c>).
/// </summary>
/// <param name="parser">The parser whose commands are completed.</param>
public sealed class CommandLineCompleter(ExCommandParser parser)
{
    /// <summary>
    /// The most candidates offered; a directory with thousands of children is not something to Tab through.
    /// </summary>
    public const int MaximumCandidates = 200;

    private readonly ExCommandParser _parser = parser ?? throw new ArgumentNullException(nameof(parser));

    /// <summary>
    /// Lists the completions of a line.
    /// </summary>
    /// <param name="line">The line typed, without its leading <c>:</c>.</param>
    /// <param name="currentDirectory">The directory a relative path starts from.</param>
    /// <param name="homeDirectory">What <c>~</c> means.</param>
    /// <param name="listDirectories">Lists the names of a directory's subdirectories; empty when it cannot be read.</param>
    /// <returns>Whole replacement lines, in order; empty when nothing completes.</returns>
    public IReadOnlyList<string> Complete(string line, string currentDirectory, string homeDirectory, Func<string, IEnumerable<string>> listDirectories)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(listDirectories);

        int space = line.IndexOf(' ', StringComparison.Ordinal);

        if (space < 0)
        {
            return _parser.CompleteNames(line);
        }

        string head = line[..space];
        string argument = line[(space + 1)..].TrimStart();

        if (_parser.Find(head.TrimEnd('!')) is not { } definition)
        {
            return [];
        }

        if (definition.Values is { Length: > 0 } values)
        {
            return [.. values
                .Where(value => value.StartsWith(argument, StringComparison.OrdinalIgnoreCase))
                .Select(value => $"{head} {value}")];
        }

        return definition.ArgumentKind == ExArgumentKind.Path
            ? [.. CompletePath(argument, currentDirectory, homeDirectory, listDirectories).Select(path => $"{head} {path}")]
            : [];
    }

    private static IEnumerable<string> CompletePath(string argument, string currentDirectory, string homeDirectory, Func<string, IEnumerable<string>> listDirectories)
    {
        if (argument == "~")
        {
            return ["~/"];
        }

        int separator = argument.LastIndexOfAny(['/', '\\']);
        string directoryPart = separator < 0 ? string.Empty : argument[..(separator + 1)];
        string partial = argument[(separator + 1)..];
        char separatorChar = separator < 0 ? Path.DirectorySeparatorChar : argument[separator];

        string directory;

        try
        {
            directory = directoryPart.Length == 0
                ? currentDirectory
                : PathUtilities.Resolve(directoryPart, currentDirectory, homeDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return [];
        }

        // Dot-directories only when asked for, as shells do: ".git" is rarely where one is going.
        bool includeHidden = partial.StartsWith('.');

        return listDirectories(directory)
            .Where(name => name.StartsWith(partial, StringComparison.OrdinalIgnoreCase) && (includeHidden || !name.StartsWith('.')))
            .Order(NaturalStringComparer.Instance)
            .Take(MaximumCandidates)
            .Select(name => directoryPart + name + separatorChar);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Enigma.FileExplorer.Core.Commands;

/// <summary>
/// Parses ex command lines (<c>:vsplit ~/src</c>, <c>:delete!</c>, <c>:mkdir "New folder"</c>) against a set
/// of definitions, accepting vim-style abbreviations.
/// </summary>
/// <param name="definitions">The commands that exist.</param>
public sealed class ExCommandParser(IReadOnlyList<ExCommandDefinition> definitions)
{
    private readonly IReadOnlyList<ExCommandDefinition> _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));

    /// <summary>
    /// Parses a command line.
    /// </summary>
    /// <param name="line">The line, with or without its leading <c>:</c>.</param>
    /// <returns>The invocation, or why there is none.</returns>
    public ExParseResult Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string text = line.Trim();

        if (text.StartsWith(':'))
        {
            text = text[1..].TrimStart();
        }

        if (text.Length == 0)
        {
            return ExParseResult.Failure("No command");
        }

        int end = 0;

        while ((end < text.Length) && char.IsAsciiLetter(text[end]))
        {
            end++;
        }

        if (end == 0)
        {
            return ExParseResult.Failure($"Not a command: {text}");
        }

        string name = text[..end];
        bool bang = (end < text.Length) && (text[end] == '!');
        string rest = text[(end + (bang ? 1 : 0))..].Trim();

        if (Find(name) is not { } definition)
        {
            return ExParseResult.Failure(Matching(name).Count > 1 ? $"Ambiguous command: {name}" : $"Not a command: {name}");
        }

        if (bang && !definition.AllowsBang)
        {
            return ExParseResult.Failure($"No ! allowed: {definition.Name}");
        }

        string? argument = rest.Length == 0 ? null : Unquote(rest);

        if ((definition.ArgumentKind == ExArgumentKind.None) && (argument is not null))
        {
            return ExParseResult.Failure($"Trailing characters: {rest}");
        }

        if ((definition.ArgumentKind == ExArgumentKind.Required) && (argument is null))
        {
            return ExParseResult.Failure($"Argument required: {definition.Name}");
        }

        string commandId = bang && (definition.BangCommandId is { } bangCommand) ? bangCommand : definition.CommandId;
        return ExParseResult.Success(new ExInvocation(definition, commandId, argument, bang));
    }

    /// <summary>
    /// Finds the command a typed name means: the exact name first, else the one command it abbreviates.
    /// </summary>
    /// <param name="name">The name typed.</param>
    /// <returns>The command, or <see langword="null"/> when the name is unknown or ambiguous.</returns>
    public ExCommandDefinition? Find(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        IReadOnlyList<ExCommandDefinition> matches = Matching(name);
        return matches.FirstOrDefault(definition => definition.Name == name) ?? (matches.Count == 1 ? matches[0] : null);
    }

    /// <summary>
    /// The full names of the commands that start with a prefix, for completion.
    /// </summary>
    /// <param name="prefix">What has been typed of the name.</param>
    /// <returns>The names, sorted.</returns>
    public IReadOnlyList<string> CompleteNames(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        return [.. _definitions
            .Where(definition => definition.Name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(definition => definition.Name)
            .Order(StringComparer.Ordinal)];
    }

    private List<ExCommandDefinition> Matching(string name) => [.. _definitions.Where(definition => definition.Matches(name))];

    private static string Unquote(string text)
        => (text.Length >= 2) && (text[0] == '"') && (text[^1] == '"')
            ? text[1..^1].Replace("\\\"", "\"", StringComparison.Ordinal)
            : text;
}

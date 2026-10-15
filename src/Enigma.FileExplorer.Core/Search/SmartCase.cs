using System;
using System.Linq;

namespace Enigma.FileExplorer.Core.Search;

/// <summary>
/// vim's <c>smartcase</c>: a pattern in lowercase matches any case; a pattern with an uppercase letter
/// matches case exactly.
/// </summary>
public static class SmartCase
{
    /// <summary>
    /// How a pattern compares.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <returns>Ordinal when the pattern has an uppercase letter, ordinal-ignore-case otherwise.</returns>
    public static StringComparison ComparisonFor(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        return pattern.Any(char.IsUpper) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    }

    /// <summary>
    /// Whether a text contains a pattern, smart-case.
    /// </summary>
    /// <param name="text">The text searched.</param>
    /// <param name="pattern">The pattern.</param>
    /// <returns><see langword="true"/> when the pattern occurs in the text.</returns>
    public static bool Contains(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(pattern);

        return text.Contains(pattern, ComparisonFor(pattern));
    }
}

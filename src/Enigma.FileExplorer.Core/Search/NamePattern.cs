using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Enigma.FileExplorer.Core.Search;

/// <summary>
/// A name pattern: a glob when it contains <c>*</c> or <c>?</c> (matching the whole name, as shells do), a
/// substring otherwise; smart-case either way.
/// </summary>
public sealed class NamePattern
{
    private readonly string _text;
    private readonly Regex? _glob;
    private readonly StringComparison _comparison;

    private NamePattern(string text, Regex? glob, StringComparison comparison)
    {
        _text = text;
        _glob = glob;
        _comparison = comparison;
    }

    /// <summary>
    /// Whether the pattern is a glob.
    /// </summary>
    public bool IsGlob => _glob is not null;

    /// <summary>
    /// Reads a pattern.
    /// </summary>
    /// <param name="pattern">What the user typed.</param>
    /// <returns>The pattern.</returns>
    public static NamePattern Parse(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        StringComparison comparison = SmartCase.ComparisonFor(pattern);

        if (pattern.IndexOfAny(['*', '?']) < 0)
        {
            return new NamePattern(pattern, null, comparison);
        }

        StringBuilder expression = new("^");

        foreach (char c in pattern)
        {
            expression.Append(c switch
            {
                '*' => ".*",
                '?' => ".",
                _ => Regex.Escape(c.ToString()),
            });
        }

        expression.Append('$');

        RegexOptions options = RegexOptions.CultureInvariant | RegexOptions.Singleline
            | (comparison == StringComparison.OrdinalIgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

        return new NamePattern(pattern, new Regex(expression.ToString(), options, TimeSpan.FromMilliseconds(100)), comparison);
    }

    /// <summary>
    /// Whether a name matches.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns><see langword="true"/> when it matches.</returns>
    public bool IsMatch(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (_glob is null)
        {
            return name.Contains(_text, _comparison);
        }

        try
        {
            return _glob.IsMatch(name);
        }
        catch (RegexMatchTimeoutException)
        {
            // A pathological glob against a pathological name: no match beats a frozen search.
            return false;
        }
    }
}

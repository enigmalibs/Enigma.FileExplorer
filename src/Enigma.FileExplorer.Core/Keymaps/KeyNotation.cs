using System;
using System.Collections.Generic;
using System.Text;

namespace Enigma.FileExplorer.Core.Keymaps;

/// <summary>
/// Reads and writes key sequences in vim's notation: <c>gg</c>, <c>&lt;C-w&gt;v</c>, <c>&lt;S-Del&gt;</c>,
/// <c>&lt;CR&gt;</c>, <c>&lt;lt&gt;</c> for a literal <c>&lt;</c>.
/// </summary>
public static class KeyNotation
{
    /// <summary>
    /// Parses a key sequence.
    /// </summary>
    /// <param name="notation">The sequence, e.g. <c>&lt;C-w&gt;v</c>.</param>
    /// <returns>The strokes, in order.</returns>
    /// <exception cref="FormatException">The notation is empty.</exception>
    public static IReadOnlyList<KeyStroke> Parse(string notation)
    {
        ArgumentNullException.ThrowIfNull(notation);

        if (notation.Length == 0)
        {
            throw new FormatException("A key sequence cannot be empty.");
        }

        List<KeyStroke> strokes = [];
        int index = 0;

        while (index < notation.Length)
        {
            if ((notation[index] == '<') && TryParseBracket(notation, index, out KeyStroke bracketed, out int consumed))
            {
                strokes.Add(bracketed);
                index += consumed;
            }
            else
            {
                strokes.Add(KeyStroke.Char(notation[index]));
                index++;
            }
        }

        return strokes;
    }

    /// <summary>
    /// Writes one stroke in vim notation.
    /// </summary>
    /// <param name="stroke">The stroke.</param>
    /// <returns>The notation.</returns>
    public static string Format(KeyStroke stroke)
    {
        if (stroke.IsPlainCharacter)
        {
            return stroke.Key == "<" ? "<lt>" : stroke.Key;
        }

        StringBuilder builder = new("<");
        AppendModifiers(builder, stroke.Modifiers, "C-", "A-", "S-");
        builder.Append(stroke.Key == "<" ? "lt" : stroke.Key).Append('>');
        return builder.ToString();
    }

    /// <summary>
    /// Writes a sequence in vim notation.
    /// </summary>
    /// <param name="strokes">The strokes.</param>
    /// <returns>The notation.</returns>
    public static string Format(IEnumerable<KeyStroke> strokes)
    {
        ArgumentNullException.ThrowIfNull(strokes);

        StringBuilder builder = new();

        foreach (KeyStroke stroke in strokes)
        {
            builder.Append(Format(stroke));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Describes a sequence for people, as help and menus show it: <c>gg</c>, <c>Ctrl+W v</c>,
    /// <c>Shift+Del</c>, <c>Ctrl+Shift+F</c>.
    /// </summary>
    /// <param name="strokes">The strokes.</param>
    /// <returns>The description.</returns>
    public static string Describe(IReadOnlyList<KeyStroke> strokes)
    {
        ArgumentNullException.ThrowIfNull(strokes);

        StringBuilder builder = new();
        bool previousWasPlain = false;

        foreach (KeyStroke stroke in strokes)
        {
            bool plain = stroke.IsPlainCharacter;

            // Runs of characters read as typed ("gg", "dD"); anything else is set apart by a space.
            if ((builder.Length > 0) && !(plain && previousWasPlain))
            {
                builder.Append(' ');
            }

            if (plain)
            {
                builder.Append(stroke.Key);
            }
            else
            {
                AppendModifiers(builder, stroke.Modifiers, "Ctrl+", "Alt+", "Shift+");
                builder.Append(stroke.Key.Length == 1 ? stroke.Key.ToUpperInvariant() : SpecialKeys.Label(stroke.Key));
            }

            previousWasPlain = plain;
        }

        return builder.ToString();
    }

    private static bool TryParseBracket(string notation, int start, out KeyStroke stroke, out int consumed)
    {
        // The closing '>' may itself be the key, as in "<C->>": try each '>' in turn, nearest first.
        for (int close = notation.IndexOf('>', start + 1); close > start; close = notation.IndexOf('>', close + 1))
        {
            if (TryParseInner(notation.AsSpan(start + 1, close - start - 1), out stroke))
            {
                consumed = close - start + 1;
                return true;
            }

            if ((close + 1 >= notation.Length) || (notation[close + 1] != '>'))
            {
                break;
            }
        }

        stroke = default;
        consumed = 0;
        return false;
    }

    private static bool TryParseInner(ReadOnlySpan<char> inner, out KeyStroke stroke)
    {
        stroke = default;
        StrokeModifiers modifiers = StrokeModifiers.None;

        while ((inner.Length > 2) && (inner[1] == '-'))
        {
            switch (char.ToUpperInvariant(inner[0]))
            {
                case 'C':
                    modifiers |= StrokeModifiers.Control;
                    break;

                case 'A':
                case 'M':
                    modifiers |= StrokeModifiers.Alt;
                    break;

                case 'S':
                    modifiers |= StrokeModifiers.Shift;
                    break;

                default:
                    return false;
            }

            inner = inner[2..];
        }

        if (inner.IsEmpty)
        {
            return false;
        }

        string? key = inner.Length == 1 ? inner.ToString() : NamedCharacter(inner) ?? SpecialKeys.Canonical(inner.ToString());

        if (key is null)
        {
            return false;
        }

        stroke = new KeyStroke(key, modifiers);
        return true;
    }

    private static string? NamedCharacter(ReadOnlySpan<char> name)
    {
        if (name.Equals("lt", StringComparison.OrdinalIgnoreCase))
        {
            return "<";
        }

        if (name.Equals("gt", StringComparison.OrdinalIgnoreCase))
        {
            return ">";
        }

        if (name.Equals("bar", StringComparison.OrdinalIgnoreCase))
        {
            return "|";
        }

        return name.Equals("bslash", StringComparison.OrdinalIgnoreCase) ? "\\" : null;
    }

    private static void AppendModifiers(StringBuilder builder, StrokeModifiers modifiers, string control, string alt, string shift)
    {
        if ((modifiers & StrokeModifiers.Control) != 0)
        {
            builder.Append(control);
        }

        if ((modifiers & StrokeModifiers.Alt) != 0)
        {
            builder.Append(alt);
        }

        if ((modifiers & StrokeModifiers.Shift) != 0)
        {
            builder.Append(shift);
        }
    }
}

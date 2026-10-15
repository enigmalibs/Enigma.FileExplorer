using System.Collections.Frozen;
using System.Collections.Generic;
using Avalonia.Input;
using Enigma.FileExplorer.Core.Keymaps;

namespace Enigma.FileExplorer.Desktop.Input;

/// <summary>
/// Translates Avalonia key events into keymap strokes, layout-aware.
/// </summary>
/// <remarks>
/// <para>
/// Printable keys are matched on the character the keyboard layout produced (<c>KeySymbol</c>), not on
/// the key's position: on a Swiss QWERTZ keyboard <c>:</c> is Shift+<c>.</c> and <c>/</c> is Shift+7, and
/// the keymap must still see <c>:</c> and <c>/</c>.
/// </para>
/// <para>
/// Ctrl and Alt chords are matched on the virtual key (<c>Key</c>) instead: with Ctrl held, layouts report
/// control characters or nothing as the symbol. AltGr, which Windows reports as Ctrl+Alt, is the
/// exception: it produces a character (<c>@</c>, <c>\</c>), and that character is the stroke.
/// </para>
/// </remarks>
public static class KeyStrokeMapper
{
    private static readonly FrozenDictionary<Key, string> Specials = new Dictionary<Key, string>
    {
        [Key.Escape] = SpecialKeys.Escape,
        [Key.Enter] = SpecialKeys.Enter,
        [Key.Back] = SpecialKeys.Backspace,
        [Key.Space] = SpecialKeys.Space,
        [Key.Tab] = SpecialKeys.Tab,
        [Key.Delete] = SpecialKeys.Delete,
        [Key.Insert] = SpecialKeys.Insert,
        [Key.Up] = SpecialKeys.Up,
        [Key.Down] = SpecialKeys.Down,
        [Key.Left] = SpecialKeys.Left,
        [Key.Right] = SpecialKeys.Right,
        [Key.Home] = SpecialKeys.Home,
        [Key.End] = SpecialKeys.End,
        [Key.PageUp] = SpecialKeys.PageUp,
        [Key.PageDown] = SpecialKeys.PageDown,
        [Key.Apps] = SpecialKeys.Apps,
    }.ToFrozenDictionary();

    /// <summary>
    /// Maps a key event.
    /// </summary>
    /// <param name="key">The virtual key.</param>
    /// <param name="modifiers">The modifiers held.</param>
    /// <param name="keySymbol">The character the layout produced, if any.</param>
    /// <returns>The stroke, or <see langword="null"/> for a key the keymap cannot express (a lone modifier, a dead key).</returns>
    public static KeyStroke? TryMap(Key key, KeyModifiers modifiers, string? keySymbol)
    {
        StrokeModifiers strokeModifiers = ToStrokeModifiers(modifiers);
        bool chord = (strokeModifiers & (StrokeModifiers.Control | StrokeModifiers.Alt)) != 0;

        if (Specials.TryGetValue(key, out string? special))
        {
            return new KeyStroke(special, strokeModifiers);
        }

        if (key is >= Key.F1 and <= Key.F24)
        {
            return new KeyStroke($"F{key - Key.F1 + 1}", strokeModifiers);
        }

        char? symbol = PrintableSymbol(keySymbol);

        if (chord)
        {
            // AltGr: Ctrl+Alt together producing a character is typing, not a chord.
            if ((strokeModifiers & (StrokeModifiers.Control | StrokeModifiers.Alt)) == (StrokeModifiers.Control | StrokeModifiers.Alt)
                && symbol is { } altGr && !char.IsAsciiLetterOrDigit(altGr))
            {
                return KeyStroke.Char(altGr);
            }

            return ChordCharacter(key) is { } character
                ? new KeyStroke(character.ToString(), strokeModifiers)
                : symbol is { } fallback ? new KeyStroke(fallback.ToString(), strokeModifiers) : null;
        }

        if (symbol is { } printable)
        {
            return KeyStroke.Char(printable);
        }

        // No symbol reported (some platforms, synthetic events): fall back on the virtual key for letters and digits.
        if (ChordCharacter(key) is { } fromKey)
        {
            bool shift = (strokeModifiers & StrokeModifiers.Shift) != 0;
            return KeyStroke.Char(shift && char.IsAsciiLetter(fromKey) ? char.ToUpperInvariant(fromKey) : fromKey);
        }

        return null;
    }

    private static StrokeModifiers ToStrokeModifiers(KeyModifiers modifiers)
    {
        StrokeModifiers result = StrokeModifiers.None;

        if ((modifiers & KeyModifiers.Control) != 0)
        {
            result |= StrokeModifiers.Control;
        }

        if ((modifiers & KeyModifiers.Alt) != 0)
        {
            result |= StrokeModifiers.Alt;
        }

        if ((modifiers & KeyModifiers.Shift) != 0)
        {
            result |= StrokeModifiers.Shift;
        }

        return result;
    }

    private static char? PrintableSymbol(string? keySymbol)
        => (keySymbol is { Length: 1 } && !char.IsControl(keySymbol[0]) && (keySymbol[0] != ' ')) ? keySymbol[0] : null;

    private static char? ChordCharacter(Key key) => key switch
    {
        >= Key.A and <= Key.Z => (char)('a' + (key - Key.A)),
        >= Key.D0 and <= Key.D9 => (char)('0' + (key - Key.D0)),
        >= Key.NumPad0 and <= Key.NumPad9 => (char)('0' + (key - Key.NumPad0)),
        _ => null,
    };
}

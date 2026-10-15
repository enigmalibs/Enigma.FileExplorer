using System;

namespace Enigma.FileExplorer.Core.Keymaps;

/// <summary>
/// One keystroke, in canonical form so that two spellings of the same stroke compare equal.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Key"/> is either a single printable character (<c>g</c>, <c>G</c>, <c>:</c>, <c>/</c>) or the
/// canonical name of a special key (<see cref="SpecialKeys"/>). Characters are what the keyboard layout
/// produced, so the same binding works on a QWERTY and a QWERTZ keyboard.
/// </para>
/// <para>
/// Under Ctrl or Alt a letter is stored lowercase and Shift is explicit, as in vim: <c>&lt;C-F&gt;</c>
/// is <c>&lt;C-f&gt;</c>, and Ctrl+Shift+F is <c>&lt;C-S-f&gt;</c>. A bare printable character never
/// carries Shift.
/// </para>
/// </remarks>
public readonly record struct KeyStroke
{
    /// <summary>
    /// Creates a stroke, canonicalizing it.
    /// </summary>
    /// <param name="key">A printable character, or a special key's name.</param>
    /// <param name="modifiers">The modifiers held.</param>
    public KeyStroke(string key, StrokeModifiers modifiers = StrokeModifiers.None)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (key.Length == 1)
        {
            if ((modifiers & (StrokeModifiers.Control | StrokeModifiers.Alt)) != 0)
            {
                key = key.ToLowerInvariant();
            }
            else
            {
                // A printable character carries its own case; Shift adds nothing to it.
                modifiers &= ~StrokeModifiers.Shift;
            }
        }
        else
        {
            key = SpecialKeys.Canonical(key) ?? throw new ArgumentException($"Unknown key '{key}'.", nameof(key));
        }

        Key = key;
        Modifiers = modifiers;
    }

    /// <summary>
    /// The printable character or the special key's canonical name.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// The modifiers held.
    /// </summary>
    public StrokeModifiers Modifiers { get; }

    /// <summary>
    /// Whether the stroke is a printable character typed without Ctrl or Alt.
    /// </summary>
    public bool IsPlainCharacter => (Key.Length == 1) && (Modifiers == StrokeModifiers.None);

    /// <summary>
    /// Whether the stroke is a plain decimal digit, which may start or extend a count.
    /// </summary>
    public bool IsDigit => IsPlainCharacter && char.IsAsciiDigit(Key[0]);

    /// <summary>
    /// A plain printable character.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <returns>The stroke.</returns>
    public static KeyStroke Char(char character) => new(character.ToString());

    /// <inheritdoc />
    public override string ToString() => KeyNotation.Format(this);
}

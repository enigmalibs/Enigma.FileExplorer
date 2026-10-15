using System;

namespace Enigma.FileExplorer.Core.Keymaps;

/// <summary>
/// The modifier keys held during a keystroke. Shift is only recorded for chords and special keys: a
/// printable character already says whether Shift was held (<c>G</c> versus <c>g</c>).
/// </summary>
[Flags]
public enum StrokeModifiers
{
    /// <summary>
    /// No modifier.
    /// </summary>
    None = 0,

    /// <summary>
    /// Ctrl (vim's <c>C-</c>).
    /// </summary>
    Control = 1,

    /// <summary>
    /// Alt (vim's <c>A-</c> or <c>M-</c>).
    /// </summary>
    Alt = 2,

    /// <summary>
    /// Shift (vim's <c>S-</c>).
    /// </summary>
    Shift = 4,
}

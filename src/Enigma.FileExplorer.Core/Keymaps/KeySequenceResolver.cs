using System;
using System.Collections.Generic;
using System.Globalization;

namespace Enigma.FileExplorer.Core.Keymaps;

/// <summary>
/// Turns keystrokes into commands, one stroke at a time: counts (<c>5j</c>), multi-key sequences
/// (<c>gg</c>, <c>&lt;C-w&gt;v</c>) and <c>Esc</c> to abandon what is pending.
/// </summary>
/// <param name="keymap">The bindings to resolve against.</param>
public sealed class KeySequenceResolver(Keymap keymap)
{
    /// <summary>
    /// The largest count accepted; more digits are ignored.
    /// </summary>
    public const int MaximumCount = 99_999;

    private static readonly KeyStroke Escape = new(SpecialKeys.Escape);

    private readonly Keymap _keymap = keymap ?? throw new ArgumentNullException(nameof(keymap));
    private readonly List<KeyStroke> _pending = [];
    private Keymap.Node _node = keymap.Root;
    private int? _count;

    /// <summary>
    /// Raised whenever <see cref="PendingText"/> changes, for the status bar.
    /// </summary>
    public event EventHandler? PendingChanged;

    /// <summary>
    /// Whether a count or a partial sequence is waiting for more keys.
    /// </summary>
    public bool IsPending => (_pending.Count > 0) || (_count is not null);

    /// <summary>
    /// What is pending, as vim's <c>showcmd</c> shows it: <c>5</c>, <c>g</c>, <c>&lt;C-w&gt;</c>.
    /// </summary>
    public string PendingText
        => (_count?.ToString(CultureInfo.InvariantCulture) ?? string.Empty) + KeyNotation.Format(_pending);

    /// <summary>
    /// Feeds one stroke.
    /// </summary>
    /// <param name="stroke">The stroke.</param>
    /// <returns>What it produced.</returns>
    public KeyResolution Feed(KeyStroke stroke)
    {
        if ((stroke == Escape) && IsPending)
        {
            Reset();
            return new KeyResolution(KeyResolutionKind.Cancelled);
        }

        // Once a count has started every digit extends it (10j); a count starts with 1-9 only, and only when
        // that digit is not a key of its own.
        bool extendsCount = _count is not null;
        bool startsCount = (stroke.Key != "0") && !_keymap.Root.Children.ContainsKey(stroke);

        if ((_pending.Count == 0) && stroke.IsDigit && (extendsCount || startsCount))
        {
            int digit = stroke.Key[0] - '0';
            _count = (int)Math.Min(MaximumCount, ((long)(_count ?? 0) * 10) + digit);
            PendingChanged?.Invoke(this, EventArgs.Empty);
            return new KeyResolution(KeyResolutionKind.Pending);
        }

        if (!_node.Children.TryGetValue(stroke, out Keymap.Node? next))
        {
            bool wasPending = IsPending;
            Reset();
            return new KeyResolution(wasPending ? KeyResolutionKind.Discarded : KeyResolutionKind.Unmatched);
        }

        if (next.Binding is { } binding)
        {
            int? count = _count;
            Reset();
            return new KeyResolution(KeyResolutionKind.Matched, binding, count);
        }

        _pending.Add(stroke);
        _node = next;
        PendingChanged?.Invoke(this, EventArgs.Empty);
        return new KeyResolution(KeyResolutionKind.Pending);
    }

    /// <summary>
    /// Abandons whatever is pending.
    /// </summary>
    public void Reset()
    {
        bool changed = IsPending;
        _pending.Clear();
        _node = _keymap.Root;
        _count = null;

        if (changed)
        {
            PendingChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

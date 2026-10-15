namespace Enigma.FileExplorer.Core.Layout;

/// <summary>
/// Where to look for a neighbouring pane (<c>gh</c>/<c>gj</c>/<c>gk</c>/<c>gl</c>, vim's <c>&lt;C-w&gt;h/j/k/l</c>).
/// </summary>
public enum FocusDirection
{
    /// <summary>To the left.</summary>
    Left,

    /// <summary>Below.</summary>
    Down,

    /// <summary>Above.</summary>
    Up,

    /// <summary>To the right.</summary>
    Right,
}

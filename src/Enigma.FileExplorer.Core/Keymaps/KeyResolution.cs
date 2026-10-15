namespace Enigma.FileExplorer.Core.Keymaps;

/// <summary>
/// What feeding a stroke to a <see cref="KeySequenceResolver"/> produced.
/// </summary>
public enum KeyResolutionKind
{
    /// <summary>
    /// The stroke started or extended a sequence (or a count); more keys are expected. The stroke is consumed.
    /// </summary>
    Pending,

    /// <summary>
    /// The sequence is complete and names a command. The stroke is consumed.
    /// </summary>
    Matched,

    /// <summary>
    /// <c>Esc</c> abandoned a pending sequence or count. The stroke is consumed.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The stroke broke a pending sequence, which is discarded with it, as vim does. The stroke is consumed.
    /// </summary>
    Discarded,

    /// <summary>
    /// Nothing was pending and the stroke is bound to nothing. The stroke is not consumed, so the UI may
    /// give it its ordinary meaning.
    /// </summary>
    Unmatched,
}

/// <summary>
/// The result of feeding one stroke.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="Binding">The matched binding, for <see cref="KeyResolutionKind.Matched"/>.</param>
/// <param name="Count">The count typed before the sequence (<c>5</c> in <c>5j</c>); <see langword="null"/> when none was.</param>
public readonly record struct KeyResolution(KeyResolutionKind Kind, KeyBinding? Binding = null, int? Count = null)
{
    /// <summary>
    /// Whether the stroke was used by the keymap and must not reach the UI's own key handling.
    /// </summary>
    public bool IsConsumed => Kind != KeyResolutionKind.Unmatched;
}

namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// One request to run a command, from a key, the command line or a menu.
/// </summary>
/// <param name="CommandId">The command (see <c>CommandIds</c>).</param>
/// <param name="Count">The count typed before the keys (<c>5</c> in <c>5j</c>), when one was.</param>
/// <param name="Argument">The argument: a key binding's fixed one, or what followed an ex command.</param>
/// <param name="Bang">Whether an ex command was typed with <c>!</c>.</param>
public sealed record CommandInvocation(string CommandId, int? Count = null, string? Argument = null, bool Bang = false)
{
    /// <summary>
    /// The count, or 1 when none was typed.
    /// </summary>
    public int CountOrOne => Count ?? 1;
}

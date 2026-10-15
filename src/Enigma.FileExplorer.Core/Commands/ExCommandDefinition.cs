using System;

namespace Enigma.FileExplorer.Core.Commands;

/// <summary>
/// An ex command: its name, how far it may be abbreviated, what it runs and what it accepts.
/// </summary>
/// <param name="Name">The full name, e.g. <c>vsplit</c>.</param>
/// <param name="MinimumLength">The shortest accepted abbreviation, e.g. 2 for <c>vs</c>.</param>
/// <param name="CommandId">The command it runs.</param>
/// <param name="ArgumentKind">What it accepts after its name.</param>
/// <param name="AllowsBang">Whether <c>!</c> may follow the name.</param>
/// <param name="BangCommandId">The command <c>name!</c> runs instead, when it is a different one (<c>:delete!</c>).</param>
/// <param name="Values">The accepted values of the argument, for completion; empty for free text.</param>
public sealed record ExCommandDefinition(
    string Name,
    int MinimumLength,
    string CommandId,
    ExArgumentKind ArgumentKind = ExArgumentKind.None,
    bool AllowsBang = false,
    string? BangCommandId = null,
    string[]? Values = null)
{
    /// <summary>
    /// The shortest accepted abbreviation.
    /// </summary>
    public string ShortName => Name[..MinimumLength];

    /// <summary>
    /// Whether a typed name abbreviates this command.
    /// </summary>
    /// <param name="typed">The name typed.</param>
    /// <returns><see langword="true"/> when it is a long enough prefix of <see cref="Name"/>.</returns>
    public bool Matches(string typed)
    {
        ArgumentNullException.ThrowIfNull(typed);

        return (typed.Length >= MinimumLength) && Name.StartsWith(typed, StringComparison.Ordinal);
    }
}

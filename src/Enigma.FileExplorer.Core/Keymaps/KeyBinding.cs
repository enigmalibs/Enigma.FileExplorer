using System;
using System.Collections.Generic;

namespace Enigma.FileExplorer.Core.Keymaps;

/// <summary>
/// A key sequence bound to a command.
/// </summary>
/// <param name="Keys">The sequence.</param>
/// <param name="CommandId">The command it runs (see <c>CommandIds</c>).</param>
/// <param name="Argument">A fixed argument for the command, such as the place number of <c>g3</c>.</param>
public sealed record KeyBinding(IReadOnlyList<KeyStroke> Keys, string CommandId, string? Argument = null)
{
    /// <summary>
    /// The sequence in vim notation.
    /// </summary>
    public string Notation => KeyNotation.Format(Keys);

    /// <summary>
    /// The sequence as people read it.
    /// </summary>
    public string Description => KeyNotation.Describe(Keys);

    /// <summary>
    /// Binds a sequence written in vim notation.
    /// </summary>
    /// <param name="notation">The sequence.</param>
    /// <param name="commandId">The command.</param>
    /// <param name="argument">The command's fixed argument.</param>
    /// <returns>The binding.</returns>
    public static KeyBinding Parse(string notation, string commandId, string? argument = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(commandId);

        return new KeyBinding(KeyNotation.Parse(notation), commandId, argument);
    }
}

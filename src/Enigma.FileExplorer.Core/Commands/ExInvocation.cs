namespace Enigma.FileExplorer.Core.Commands;

/// <summary>
/// A parsed ex command line, ready to run.
/// </summary>
/// <param name="Definition">The command typed.</param>
/// <param name="CommandId">The command to run (the bang variant's when <c>!</c> selected one).</param>
/// <param name="Argument">The argument, unquoted; <see langword="null"/> when none was given.</param>
/// <param name="Bang">Whether <c>!</c> followed the name.</param>
public sealed record ExInvocation(ExCommandDefinition Definition, string CommandId, string? Argument, bool Bang);

using System.Collections.Generic;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// What to transfer, where, and how.
/// </summary>
/// <param name="Sources">The files and directories, by absolute path.</param>
/// <param name="TargetDirectory">The directory they go into.</param>
/// <param name="Mode">Copy or move.</param>
public sealed record TransferRequest(IReadOnlyList<string> Sources, string TargetDirectory, TransferMode Mode);

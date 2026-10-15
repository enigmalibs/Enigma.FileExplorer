using System;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// A destination that already exists, described for the user to decide.
/// </summary>
/// <param name="SourcePath">What is being transferred.</param>
/// <param name="TargetPath">What is in the way.</param>
/// <param name="SourceSize">The source's size (0 for a directory).</param>
/// <param name="SourceModified">When the source was last written.</param>
/// <param name="TargetSize">The existing entry's size (0 for a directory).</param>
/// <param name="TargetModified">When the existing entry was last written.</param>
/// <param name="TargetIsDirectory">Whether the existing entry is a directory.</param>
public sealed record ConflictInfo(
    string SourcePath,
    string TargetPath,
    long SourceSize,
    DateTime SourceModified,
    long TargetSize,
    DateTime TargetModified,
    bool TargetIsDirectory);

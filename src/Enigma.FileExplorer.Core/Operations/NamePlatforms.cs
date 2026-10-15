using System;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// The platform the application runs on.
/// </summary>
public static class NamePlatforms
{
    /// <summary>
    /// The current platform's rules.
    /// </summary>
    public static NamePlatform Current { get; } = OperatingSystem.IsWindows() ? NamePlatform.Windows : NamePlatform.Unix;
}

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// Whose file-name rules apply.
/// </summary>
public enum NamePlatform
{
    /// <summary>
    /// Windows: reserved characters and device names, no trailing dot or space.
    /// </summary>
    Windows,

    /// <summary>
    /// Linux and macOS: only <c>/</c> and NUL are forbidden.
    /// </summary>
    Unix,
}

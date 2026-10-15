namespace Enigma.FileExplorer.Core.Settings;

/// <summary>
/// One saved bookmark: a folder and the name it is listed under.
/// </summary>
public sealed record BookmarkSettings
{
    /// <summary>
    /// The name it is listed under.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The folder.
    /// </summary>
    public string Path { get; set; } = string.Empty;
}

namespace Enigma.FileExplorer.Core.Places;

/// <summary>
/// The mount conventions a place list follows.
/// </summary>
internal enum PlacesPlatform
{
    /// <summary>
    /// Drive letters.
    /// </summary>
    Windows,

    /// <summary>
    /// <c>/</c> plus <c>/media</c>, <c>/mnt</c> and <c>/run/media</c>.
    /// </summary>
    Linux,

    /// <summary>
    /// <c>/</c> plus <c>/Volumes</c>.
    /// </summary>
    MacOS,
}

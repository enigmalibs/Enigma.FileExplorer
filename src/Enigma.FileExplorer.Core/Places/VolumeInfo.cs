using System.IO;

namespace Enigma.FileExplorer.Core.Places;

/// <summary>
/// A mounted volume as the operating system reports it.
/// </summary>
/// <param name="RootPath">Where it is mounted: <c>C:\</c>, <c>/media/me/USB</c>.</param>
/// <param name="Label">Its volume label, when it has one.</param>
/// <param name="Format">Its file system (<c>NTFS</c>, <c>ext4</c>, <c>proc</c>), when known.</param>
/// <param name="DriveType">What kind of drive it is.</param>
/// <param name="IsReady">Whether it can be read (a card reader without a card cannot).</param>
public sealed record VolumeInfo(string RootPath, string? Label, string? Format, DriveType DriveType, bool IsReady);

using System;
using System.Collections.Generic;
using System.IO;

namespace Enigma.FileExplorer.Core.Places;

/// <summary>
/// Lists volumes through <see cref="DriveInfo"/>.
/// </summary>
public sealed class SystemVolumeSource : IVolumeSource
{
    /// <inheritdoc />
    public IReadOnlyList<VolumeInfo> GetVolumes()
    {
        List<VolumeInfo> volumes = [];

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            bool ready = Safe(() => drive.IsReady, false);

            // Label and format are only readable on a ready drive; asking anyway throws.
            volumes.Add(new VolumeInfo(
                drive.RootDirectory.FullName,
                ready ? Safe(() => drive.VolumeLabel, null) : null,
                ready ? Safe(() => drive.DriveFormat, null) : null,
                Safe(() => drive.DriveType, DriveType.Unknown),
                ready));
        }

        return volumes;
    }

    private static T Safe<T>(Func<T> read, T fallback)
    {
        try
        {
            return read();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // One unreadable volume (a vanished network share, a locked device) must not hide the others.
            return fallback;
        }
    }
}

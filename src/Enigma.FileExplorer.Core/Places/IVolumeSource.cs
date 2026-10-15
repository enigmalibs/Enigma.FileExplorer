using System.Collections.Generic;

namespace Enigma.FileExplorer.Core.Places;

/// <summary>
/// Lists the mounted volumes.
/// </summary>
public interface IVolumeSource
{
    /// <summary>
    /// Lists every mounted volume, pseudo file systems included; filtering is the caller's job.
    /// </summary>
    /// <returns>The volumes.</returns>
    IReadOnlyList<VolumeInfo> GetVolumes();
}

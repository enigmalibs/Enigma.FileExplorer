using System;
using System.Collections.Generic;
using System.IO;
using Enigma.FileExplorer.Core.FileSystem;

namespace Enigma.FileExplorer.Core.Places;

/// <summary>
/// Builds the sidebar from the user's known folders and the mounted volumes.
/// </summary>
/// <param name="folders">The user's well-known folders.</param>
/// <param name="volumes">The mounted volumes.</param>
public sealed class PlacesProvider(IKnownFolders folders, IVolumeSource volumes) : IPlacesProvider
{
    /// <summary>
    /// File systems that are never storage a user browses to: kernel interfaces and container layers.
    /// </summary>
    private static readonly HashSet<string> PseudoFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "autofs", "binfmt_misc", "bpf", "cgroup", "cgroup2", "configfs", "debugfs", "devpts", "devtmpfs",
        "efivarfs", "fusectl", "hugetlbfs", "mqueue", "nsfs", "overlay", "proc", "pstore", "ramfs",
        "rpc_pipefs", "securityfs", "squashfs", "sysfs", "tmpfs", "tracefs", "fuse.portal", "fuse.gvfsd-fuse",
    };

    /// <inheritdoc />
    public IReadOnlyList<Place> GetPlaces() => Build(folders, volumes.GetVolumes(), CurrentPlatform(), Directory.Exists);

    /// <summary>
    /// Builds the places for a platform.
    /// </summary>
    /// <param name="folders">The user's well-known folders.</param>
    /// <param name="volumes">The mounted volumes.</param>
    /// <param name="platform">The platform whose mount conventions apply.</param>
    /// <param name="directoryExists">Tells whether a folder exists.</param>
    /// <returns>The places, folders first, then devices.</returns>
    internal static IReadOnlyList<Place> Build(IKnownFolders folders, IReadOnlyList<VolumeInfo> volumes, PlacesPlatform platform, Func<string, bool> directoryExists)
    {
        ArgumentNullException.ThrowIfNull(folders);
        ArgumentNullException.ThrowIfNull(volumes);
        ArgumentNullException.ThrowIfNull(directoryExists);

        List<Place> places = [];
        HashSet<string> seen = new(StringComparer.FromComparison(PathUtilities.Comparison));

        void AddFolder(string path, string name, PlaceKind kind)
        {
            // A folder the platform does not define, or that resolves to one already listed (Linux without
            // XDG directories maps Desktop to home), is left out rather than shown twice.
            if (!string.IsNullOrEmpty(path) && directoryExists(path) && seen.Add(PathUtilities.Normalize(path)))
            {
                places.Add(new Place(name, PathUtilities.Normalize(path), kind));
            }
        }

        AddFolder(folders.Home, "Home", PlaceKind.Home);
        AddFolder(folders.Desktop, "Desktop", PlaceKind.Desktop);
        AddFolder(folders.Documents, "Documents", PlaceKind.Documents);
        AddFolder(folders.Downloads, "Downloads", PlaceKind.Downloads);
        AddFolder(folders.Pictures, "Pictures", PlaceKind.Pictures);
        AddFolder(folders.Music, "Music", PlaceKind.Music);
        AddFolder(folders.Videos, "Videos", PlaceKind.Videos);

        foreach (VolumeInfo volume in volumes)
        {
            if (ToDevice(volume, platform) is { } device && seen.Add(device.Path))
            {
                places.Add(device);
            }
        }

        return places;
    }

    private static Place? ToDevice(VolumeInfo volume, PlacesPlatform platform)
    {
        if (!volume.IsReady || (volume.Format is { } format && PseudoFormats.Contains(format)))
        {
            return null;
        }

        string root = Path.TrimEndingDirectorySeparator(volume.RootPath);

        if (platform == PlacesPlatform.Windows)
        {
            string letter = root.TrimEnd('\\');
            string name = string.IsNullOrWhiteSpace(volume.Label) ? letter : $"{volume.Label} ({letter})";
            return new Place(name, volume.RootPath, KindOf(volume.DriveType));
        }

        if (root is "/" or "")
        {
            return new Place("Root", "/", PlaceKind.Root);
        }

        string[] userMounts = platform == PlacesPlatform.MacOS
            ? ["/Volumes/"]
            : ["/media/", "/mnt/", "/run/media/"];

        foreach (string prefix in userMounts)
        {
            if (root.StartsWith(prefix, StringComparison.Ordinal))
            {
                string name = string.IsNullOrWhiteSpace(volume.Label) ? Path.GetFileName(root) : volume.Label;
                return new Place(name, root, KindOf(volume.DriveType));
            }
        }

        // Everything else (/boot, /var/lib/docker/…, /snap/…) is system plumbing, not a place.
        return null;
    }

    private static PlaceKind KindOf(DriveType type) => type switch
    {
        DriveType.Removable => PlaceKind.RemovableDrive,
        DriveType.Network => PlaceKind.NetworkDrive,
        DriveType.CDRom => PlaceKind.OpticalDrive,
        _ => PlaceKind.FixedDrive,
    };

    private static PlacesPlatform CurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return PlacesPlatform.Windows;
        }

        return OperatingSystem.IsMacOS() ? PlacesPlatform.MacOS : PlacesPlatform.Linux;
    }
}

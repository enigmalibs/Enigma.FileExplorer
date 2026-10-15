using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Places;

public sealed class PlacesProviderTests
{
    private static readonly Func<string, bool> Exists = Directory.Exists;

    [Fact]
    public void Build_ListsExistingFoldersInOrder_SkippingMissingAndDuplicates()
    {
        using TempDirectory temp = new();
        Folders folders = new()
        {
            Home = temp.Path,
            Desktop = temp.Path,
            Documents = temp.CreateDirectory("Documents"),
            Downloads = temp.Combine("Downloads"),
            Pictures = temp.CreateDirectory("Pictures"),
            Music = string.Empty,
            Videos = temp.CreateDirectory("Videos"),
        };

        IReadOnlyList<Place> places = PlacesProvider.Build(folders, [], PlacesPlatform.Linux, Exists);

        Assert.Equal(["Home", "Documents", "Pictures", "Videos"], places.Select(place => place.Name));
        Assert.Equal([PlaceKind.Home, PlaceKind.Documents, PlaceKind.Pictures, PlaceKind.Videos], places.Select(place => place.Kind));
        Assert.All(places, place => Assert.False(place.IsDevice));
    }

    [Fact]
    public void Build_OnWindows_NamesDrivesByLabelAndLetter_AndSkipsUnreadyOnes()
    {
        VolumeInfo[] volumes =
        [
            new(@"C:\", "Windows", "NTFS", DriveType.Fixed, IsReady: true),
            new(@"D:\", null, null, DriveType.CDRom, IsReady: false),
            new(@"E:\", "", "FAT32", DriveType.Removable, IsReady: true),
            new(@"S:\", "Share", "NTFS", DriveType.Network, IsReady: true),
        ];

        IReadOnlyList<Place> places = PlacesProvider.Build(new Folders(), volumes, PlacesPlatform.Windows, _ => false);

        Assert.Equal(["Windows (C:)", "E:", "Share (S:)"], places.Select(place => place.Name));
        Assert.Equal([@"C:\", @"E:\", @"S:\"], places.Select(place => place.Path));
        Assert.Equal([PlaceKind.FixedDrive, PlaceKind.RemovableDrive, PlaceKind.NetworkDrive], places.Select(place => place.Kind));
        Assert.All(places, place => Assert.True(place.IsDevice));
    }

    [Fact]
    public void Build_OnLinux_KeepsRootAndUserMounts_DropsPlumbing()
    {
        VolumeInfo[] volumes =
        [
            new("/", null, "ext4", DriveType.Fixed, IsReady: true),
            new("/proc", null, "proc", DriveType.Ram, IsReady: true),
            new("/sys/fs/cgroup", null, "cgroup2", DriveType.Ram, IsReady: true),
            new("/boot/efi", null, "vfat", DriveType.Fixed, IsReady: true),
            new("/snap/core/123", null, "squashfs", DriveType.Fixed, IsReady: true),
            new("/run/user/1000", null, "tmpfs", DriveType.Ram, IsReady: true),
            new("/media/me/USBSTICK", "USBSTICK", "vfat", DriveType.Removable, IsReady: true),
            new("/mnt/nas", null, "nfs4", DriveType.Network, IsReady: true),
            new("/run/media/me/Backup", null, "ext4", DriveType.Fixed, IsReady: true),
        ];

        IReadOnlyList<Place> places = PlacesProvider.Build(new Folders(), volumes, PlacesPlatform.Linux, _ => false);

        Assert.Equal(["Root", "USBSTICK", "nas", "Backup"], places.Select(place => place.Name));
        Assert.Equal(["/", "/media/me/USBSTICK", "/mnt/nas", "/run/media/me/Backup"], places.Select(place => place.Path));
        Assert.Equal(
            [PlaceKind.Root, PlaceKind.RemovableDrive, PlaceKind.NetworkDrive, PlaceKind.FixedDrive],
            places.Select(place => place.Kind));
    }

    [Fact]
    public void Build_OnMacOS_KeepsRootAndVolumes()
    {
        VolumeInfo[] volumes =
        [
            new("/", null, "apfs", DriveType.Fixed, IsReady: true),
            new("/System/Volumes/Data", null, "apfs", DriveType.Fixed, IsReady: true),
            new("/Volumes/Photos", "Photos", "exfat", DriveType.Removable, IsReady: true),
        ];

        IReadOnlyList<Place> places = PlacesProvider.Build(new Folders(), volumes, PlacesPlatform.MacOS, _ => false);

        Assert.Equal(["Root", "Photos"], places.Select(place => place.Name));
    }

    [Fact]
    public void Build_PutsFoldersBeforeDevices()
    {
        using TempDirectory temp = new();
        VolumeInfo[] volumes = [new("/", null, "ext4", DriveType.Fixed, IsReady: true)];

        IReadOnlyList<Place> places = PlacesProvider.Build(new Folders { Home = temp.Path }, volumes, PlacesPlatform.Linux, Exists);

        Assert.Equal([PlaceKind.Home, PlaceKind.Root], places.Select(place => place.Kind));
    }

    [Fact]
    public void XdgUserDirs_Parse_ExpandsHomeAndIgnoresCommentsAndRelativeValues()
    {
        string content = """
            # This file is written by xdg-user-dirs-update
            XDG_DESKTOP_DIR="$HOME/Bureau"
            XDG_DOWNLOAD_DIR="$HOME/Téléchargements"
            XDG_MUSIC_DIR="/data/music"
            XDG_VIDEOS_DIR="relative/videos"
            """;

        IReadOnlyDictionary<string, string> directories = XdgUserDirs.Parse(content, "/home/me");

        Assert.Equal("/home/me/Téléchargements", directories[XdgUserDirs.Download]);
        Assert.Equal("/home/me/Bureau", directories["XDG_DESKTOP_DIR"]);
        Assert.Equal("/data/music", directories["XDG_MUSIC_DIR"]);
        Assert.False(directories.ContainsKey("XDG_VIDEOS_DIR"));
    }

    [Fact]
    public void KnownFolders_HomeExists() => Assert.True(Directory.Exists(new KnownFolders().Home));

    private sealed class Folders : IKnownFolders
    {
        public string Home { get; init; } = string.Empty;

        public string Desktop { get; init; } = string.Empty;

        public string Documents { get; init; } = string.Empty;

        public string Downloads { get; init; } = string.Empty;

        public string Pictures { get; init; } = string.Empty;

        public string Music { get; init; } = string.Empty;

        public string Videos { get; init; } = string.Empty;
    }
}

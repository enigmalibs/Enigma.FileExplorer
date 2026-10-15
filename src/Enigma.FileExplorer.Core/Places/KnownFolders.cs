using System;
using System.IO;

namespace Enigma.FileExplorer.Core.Places;

/// <summary>
/// The current user's well-known folders, as the operating system reports them.
/// </summary>
/// <remarks>
/// .NET resolves Desktop, Documents, Pictures, Music and Videos itself, reading the XDG user directories
/// on Linux, so a French desktop's "Bureau" is found. It has no Downloads folder, so that one is read
/// from the XDG file on Linux and is <c>~/Downloads</c> elsewhere: Windows keeps that folder's on-disk name
/// in every language.
/// </remarks>
public sealed class KnownFolders : IKnownFolders
{
    /// <summary>
    /// Resolves the folders once; they do not move while the application runs.
    /// </summary>
    public KnownFolders()
    {
        Home = ResolveHome();
        Desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        Documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        Pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        Music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        Videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        Downloads = ResolveDownloads(Home);
    }

    /// <inheritdoc />
    public string Home { get; }

    /// <inheritdoc />
    public string Desktop { get; }

    /// <inheritdoc />
    public string Documents { get; }

    /// <inheritdoc />
    public string Downloads { get; }

    /// <inheritdoc />
    public string Pictures { get; }

    /// <inheritdoc />
    public string Music { get; }

    /// <inheritdoc />
    public string Videos { get; }

    private static string ResolveHome()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // An account without a profile directory still needs somewhere to start; the root of the
        // drive the application runs from always exists.
        return string.IsNullOrEmpty(home) ? Path.GetPathRoot(AppContext.BaseDirectory) ?? "/" : home;
    }

    private static string ResolveDownloads(string home)
    {
        if (OperatingSystem.IsLinux() && XdgUserDirs.Read(home).TryGetValue(XdgUserDirs.Download, out string? xdg))
        {
            return xdg;
        }

        return Path.Combine(home, "Downloads");
    }
}
